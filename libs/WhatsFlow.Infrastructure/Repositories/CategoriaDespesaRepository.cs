using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class CategoriaDespesaRepository : ICategoriaDespesaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CategoriaDespesaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public CategoriaDespesaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<CategoriaDespesa>> GetAllAsync()
    {
        return await _context.Set<CategoriaDespesa>()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<CategoriaDespesa?> GetByIdAsync(int id)
    {
        return await _context.Set<CategoriaDespesa>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CategoriaDespesa> CreateAsync(CategoriaDespesa categoriaDespesa)
    {
        categoriaDespesa.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<CategoriaDespesa>().Add(categoriaDespesa);
        await _context.SaveChangesAsync();
        return categoriaDespesa;
    }

    public async Task<CategoriaDespesa> UpdateAsync(CategoriaDespesa categoriaDespesa)
    {
        _context.Set<CategoriaDespesa>().Update(categoriaDespesa);
        await _context.SaveChangesAsync();
        return categoriaDespesa;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<CategoriaDespesa>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<CategoriaDespesa>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
