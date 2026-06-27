using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class CategoriaNoticiaRepository : ICategoriaNoticiaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CategoriaNoticiaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public CategoriaNoticiaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<CategoriaNoticia>> GetAllAsync()
    {
        return await _context.Set<CategoriaNoticia>()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<CategoriaNoticia?> GetByIdAsync(int id)
    {
        return await _context.Set<CategoriaNoticia>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CategoriaNoticia> CreateAsync(CategoriaNoticia categoriaNoticia)
    {
        categoriaNoticia.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<CategoriaNoticia>().Add(categoriaNoticia);
        await _context.SaveChangesAsync();
        return categoriaNoticia;
    }

    public async Task<CategoriaNoticia> UpdateAsync(CategoriaNoticia categoriaNoticia)
    {
        _context.Set<CategoriaNoticia>().Update(categoriaNoticia);
        await _context.SaveChangesAsync();
        return categoriaNoticia;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<CategoriaNoticia>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<CategoriaNoticia>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}


