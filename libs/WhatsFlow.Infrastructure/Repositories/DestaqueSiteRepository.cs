using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class DestaqueSiteRepository : IDestaqueSiteRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public DestaqueSiteRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public DestaqueSiteRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<DestaqueSite>> GetAllAsync()
    {
        return await _context.Set<DestaqueSite>()
            .OrderBy(d => d.DataCriacao) // Ordenar por data de criação crescente (mais antigo primeiro)
            .ThenBy(d => d.Id) // Em caso de empate na data, ordenar por ID
            .ToListAsync();
    }

    public async Task<DestaqueSite?> GetByIdAsync(int id)
    {
        return await _context.Set<DestaqueSite>()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DestaqueSite> CreateAsync(DestaqueSite destaqueSite)
    {
        destaqueSite.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<DestaqueSite>().Add(destaqueSite);
        await _context.SaveChangesAsync();
        return destaqueSite;
    }

    public async Task<DestaqueSite> UpdateAsync(DestaqueSite destaqueSite)
    {
        _context.Set<DestaqueSite>().Update(destaqueSite);
        await _context.SaveChangesAsync();
        return destaqueSite;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<DestaqueSite>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<DestaqueSite>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}


