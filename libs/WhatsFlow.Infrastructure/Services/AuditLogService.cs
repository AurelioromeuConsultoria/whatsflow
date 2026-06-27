using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Auditoria;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private static readonly string[] CriticalActions = ["Login", "AlterarSenha", "Publicar", "Confirmar", "Recusar", "Aprovar", "Rejeitar", "ErroEnvio", "ProcessarDia"];
    private static readonly string[] FailureActions = ["Delete", "ErroEnvio", "Recusar", "Rejeitar"];
    private readonly WhatsFlowDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantContext _tenantContext;

    public AuditLogService(WhatsFlowDbContext db, ICurrentUserContext currentUser)
        : this(db, currentUser, new DefaultTenantContext())
    {
    }

    public AuditLogService(WhatsFlowDbContext db, ICurrentUserContext currentUser, ITenantContext tenantContext)
    {
        _db = db;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogPagedQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);

        var q = BuildFilteredQuery(query).OrderByDescending(a => a.CreatedAt);

        var total = await q.CountAsync();
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                UserId = a.UserId,
                UserName = a.UserName,
                UserEmail = a.UserEmail,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt,
                ChangesJson = a.ChangesJson
            })
            .ToListAsync();

        return new PagedResultDto<AuditLogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AuditLogMetricsDto> GetMetricsAsync(AuditLogPagedQueryDto query)
    {
        var q = BuildFilteredQuery(query);

        var totalLogs = await q.CountAsync();
        var criticalActions = await q.CountAsync(a => CriticalActions.Contains(a.Action));
        var failureActions = await q.CountAsync(a => FailureActions.Contains(a.Action));
        var distinctUsers = await q
            .Select(a => a.UserEmail ?? a.UserName ?? (a.UserId.HasValue ? a.UserId.Value.ToString() : null))
            .Where(x => x != null)
            .Distinct()
            .CountAsync();

        var topUser = await q
            .GroupBy(a => a.UserEmail ?? a.UserName ?? (a.UserId.HasValue ? a.UserId.Value.ToString() : "Sistema"))
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .FirstOrDefaultAsync();

        var topEntity = await q
            .GroupBy(a => a.EntityName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .FirstOrDefaultAsync();

        var topAction = await q
            .GroupBy(a => a.Action)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .FirstOrDefaultAsync();

        return new AuditLogMetricsDto
        {
            TotalLogs = totalLogs,
            CriticalActions = criticalActions,
            FailureActions = failureActions,
            DistinctUsers = distinctUsers,
            TopUserLabel = topUser?.Label,
            TopUserCount = topUser?.Count ?? 0,
            TopEntityName = topEntity?.Name,
            TopEntityCount = topEntity?.Count ?? 0,
            TopActionName = topAction?.Name,
            TopActionCount = topAction?.Count ?? 0
        };
    }

    public async Task RecordAsync(string entityName, string entityId, string action, object? changes = null)
    {
        var log = new AuditLog
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            UserEmail = _currentUser.UserEmail,
            IpAddress = _currentUser.IpAddress,
            CreatedAt = DateTime.UtcNow,
            ChangesJson = changes == null ? null : JsonSerializer.Serialize(changes)
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    private IQueryable<AuditLog> BuildFilteredQuery(AuditLogPagedQueryDto query)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (_tenantContext.TenantId.HasValue)
        {
            q = q.Where(a => a.TenantId == _tenantContext.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            q = q.Where(a =>
                a.EntityName.ToLower().Contains(search) ||
                a.Action.ToLower().Contains(search) ||
                a.EntityId.ToLower().Contains(search) ||
                (a.UserName != null && a.UserName.ToLower().Contains(search)) ||
                (a.UserEmail != null && a.UserEmail.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            var entity = query.EntityName.Trim();
            q = q.Where(a => a.EntityName == entity);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var entityId = query.EntityId.Trim();
            q = q.Where(a => a.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            q = q.Where(a => a.Action == action);
        }

        if (query.UserId.HasValue)
        {
            var userId = query.UserId.Value;
            q = q.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.UserName))
        {
            var userName = query.UserName.Trim().ToLowerInvariant();
            q = q.Where(a => a.UserName != null && a.UserName.ToLower().Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(query.UserEmail))
        {
            var email = query.UserEmail.Trim().ToLowerInvariant();
            q = q.Where(a => a.UserEmail != null && a.UserEmail.ToLower().Contains(email));
        }

        if (query.From.HasValue)
        {
            var from = DateTime.SpecifyKind(query.From.Value, DateTimeKind.Utc);
            q = q.Where(a => a.CreatedAt >= from);
        }

        if (query.To.HasValue)
        {
            var to = DateTime.SpecifyKind(query.To.Value, DateTimeKind.Utc);
            q = q.Where(a => a.CreatedAt <= to);
        }

        return q;
    }
}
