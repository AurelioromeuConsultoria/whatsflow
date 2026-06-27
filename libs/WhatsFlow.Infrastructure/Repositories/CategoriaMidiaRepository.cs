using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class CategoriaMidiaRepository : ICategoriaMidiaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CategoriaMidiaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public CategoriaMidiaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<CategoriaMidia>> GetAllAsync()
    {
        return await _context.Set<CategoriaMidia>()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<CategoriaMidia?> GetByIdAsync(int id)
    {
        return await _context.Set<CategoriaMidia>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CategoriaMidia> CreateAsync(CategoriaMidia categoria)
    {
        categoria.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<CategoriaMidia>().Add(categoria);
        await _context.SaveChangesAsync();
        return categoria;
    }

    public async Task<CategoriaMidia> UpdateAsync(CategoriaMidia categoria)
    {
        _context.Set<CategoriaMidia>().Update(categoria);
        await _context.SaveChangesAsync();
        return categoria;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Set<CategoriaMidia>().FindAsync(id);
        if (entity == null) return false;

        _context.Set<CategoriaMidia>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}




