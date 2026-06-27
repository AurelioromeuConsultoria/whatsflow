using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class GaleriaFotoItemRepository : IGaleriaFotoItemRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GaleriaFotoItemRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public GaleriaFotoItemRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<List<GaleriaFotoItem>> GetByGaleriaIdAsync(int galeriaId)
    {
        return await _context.Set<GaleriaFotoItem>()
            .Where(i => i.GaleriaFotoId == galeriaId)
            .OrderBy(i => i.Ordem)
            .ThenBy(i => i.NomeArquivo)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<GaleriaFotoItem> items)
    {
        var tenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        foreach (var item in items)
        {
            item.TenantId = tenantId;
        }
        await _context.Set<GaleriaFotoItem>().AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    public async Task SetDestaqueAsync(int galeriaId, string nomeArquivoDestaque)
    {
        var itens = await _context.Set<GaleriaFotoItem>()
            .Where(i => i.GaleriaFotoId == galeriaId)
            .ToListAsync();
        foreach (var item in itens)
        {
            item.Destaque = string.Equals(item.NomeArquivo, nomeArquivoDestaque, StringComparison.OrdinalIgnoreCase);
        }
        await _context.SaveChangesAsync();
    }
}
