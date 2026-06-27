using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Infrastructure.Data;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserContext _currentUser;
    private static readonly ConcurrentDictionary<DbContext, List<PendingAudit>> PendingByContext = new();
    private static readonly ConcurrentDictionary<DbContext, byte> WritingAudit = new();

    public AuditSaveChangesInterceptor(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CollectPending(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CollectPending(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        WriteAuditLogs(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        WriteAuditLogs(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CollectPending(DbContext? context)
    {
        if (context == null) return;
        if (WritingAudit.ContainsKey(context)) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                e.Entity is not AuditLog);

        var list = new List<PendingAudit>();

        foreach (var entry in entries)
        {
            var entityName = entry.Metadata.ClrType.Name;
            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "Unknown"
            };

            var changes = BuildChanges(entry);
            list.Add(new PendingAudit
            {
                Entry = entry,
                EntityName = entityName,
                Action = action,
                ChangesJson = changes == null ? null : JsonSerializer.Serialize(changes)
            });
        }

        if (list.Count == 0) return;
        PendingByContext[context] = list;
    }

    private void WriteAuditLogs(DbContext? context)
    {
        if (context == null) return;
        if (!PendingByContext.TryRemove(context, out var pending) || pending.Count == 0) return;
        if (!WritingAudit.TryAdd(context, 1)) return;

        try
        {
            var logs = pending
                .Select(p =>
                {
                    var entityId = GetPrimaryKeyString(p.Entry);
                    return new AuditLog
                    {
                        EntityName = p.EntityName,
                        EntityId = entityId,
                        Action = p.Action,
                        UserId = _currentUser.UserId,
                        UserName = _currentUser.UserName,
                        UserEmail = _currentUser.UserEmail,
                        IpAddress = _currentUser.IpAddress,
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                        ChangesJson = p.ChangesJson
                    };
                })
                .ToList();

            if (logs.Count == 0) return;

            context.Set<AuditLog>().AddRange(logs);
            context.SaveChanges();
        }
        finally
        {
            WritingAudit.TryRemove(context, out _);
        }
    }

    private static string GetPrimaryKeyString(EntityEntry entry)
    {
        var keys = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keys == null || keys.Count == 0) return string.Empty;
        if (keys.Count == 1)
        {
            var val = entry.Property(keys[0].Name).CurrentValue;
            return val?.ToString() ?? string.Empty;
        }

        var dict = new Dictionary<string, object?>();
        foreach (var k in keys)
        {
            dict[k.Name] = entry.Property(k.Name).CurrentValue;
        }
        return JsonSerializer.Serialize(dict);
    }

    private static object? BuildChanges(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            var newValues = entry.Properties
                .Where(p => !p.Metadata.IsPrimaryKey())
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            return new { newValues };
        }

        if (entry.State == EntityState.Deleted)
        {
            var oldValues = entry.Properties
                .Where(p => !p.Metadata.IsPrimaryKey())
                .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
            return new { oldValues };
        }

        if (entry.State == EntityState.Modified)
        {
            var changes = new Dictionary<string, object?>();
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsPrimaryKey()) continue;
                if (!prop.IsModified) continue;
                changes[prop.Metadata.Name] = new { oldValue = prop.OriginalValue, newValue = prop.CurrentValue };
            }

            return changes.Count == 0 ? null : new { changes };
        }

        return null;
    }

    private sealed class PendingAudit
    {
        public required EntityEntry Entry { get; init; }
        public required string EntityName { get; init; }
        public required string Action { get; init; }
        public string? ChangesJson { get; init; }
    }
}
