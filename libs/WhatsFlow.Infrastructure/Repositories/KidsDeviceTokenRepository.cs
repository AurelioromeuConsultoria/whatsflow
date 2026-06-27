using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class KidsDeviceTokenRepository : IKidsDeviceTokenRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public KidsDeviceTokenRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public KidsDeviceTokenRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task UpsertAsync(int pessoaId, string fcmToken, string platform)
    {
        var normalized = platform?.Trim().ToUpperInvariant() ?? "ANDROID";
        if (normalized != "ANDROID" && normalized != "IOS") normalized = "ANDROID";

        var existing = await _context.Set<KidsDeviceToken>()
            .FirstOrDefaultAsync(t => t.PessoaId == pessoaId && t.FcmToken == fcmToken);

        if (existing != null)
        {
            existing.Platform = normalized;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return;
        }

        _context.Set<KidsDeviceToken>().Add(new KidsDeviceToken
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            PessoaId = pessoaId,
            FcmToken = fcmToken,
            Platform = normalized,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> GetTokensByPessoaIdsAsync(IEnumerable<int> pessoaIds)
    {
        var ids = pessoaIds.Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<string>();

        return await _context.Set<KidsDeviceToken>()
            .Where(t => ids.Contains(t.PessoaId))
            .Select(t => t.FcmToken)
            .ToListAsync();
    }

    public async Task DeleteByTokenAsync(string fcmToken)
    {
        var rows = await _context.Set<KidsDeviceToken>()
            .Where(t => t.FcmToken == fcmToken)
            .ToListAsync();
        if (rows.Count == 0) return;
        _context.Set<KidsDeviceToken>().RemoveRange(rows);
        await _context.SaveChangesAsync();
    }
}
