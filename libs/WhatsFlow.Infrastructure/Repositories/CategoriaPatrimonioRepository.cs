using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class CategoriaPatrimonioRepository : ICategoriaPatrimonioRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CategoriaPatrimonioRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public CategoriaPatrimonioRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<CategoriaPatrimonio>> GetAllAsync()
    {
        return await _context.Set<CategoriaPatrimonio>()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<CategoriaPatrimonio?> GetByIdAsync(int id)
    {
        return await _context.Set<CategoriaPatrimonio>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CategoriaPatrimonio?> GetByNomeAsync(string nome)
    {
        return await _context.Set<CategoriaPatrimonio>()
            .FirstOrDefaultAsync(c => c.Nome.ToLower() == nome.ToLower());
    }

    public async Task<CategoriaPatrimonio> CreateAsync(CategoriaPatrimonio categoria)
    {
        categoria.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<CategoriaPatrimonio>().Add(categoria);
        await _context.SaveChangesAsync();
        return categoria;
    }

    public async Task<CategoriaPatrimonio> UpdateAsync(CategoriaPatrimonio categoria)
    {
        _context.Set<CategoriaPatrimonio>().Update(categoria);
        await _context.SaveChangesAsync();
        return categoria;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<CategoriaPatrimonio>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<CategoriaPatrimonio>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
