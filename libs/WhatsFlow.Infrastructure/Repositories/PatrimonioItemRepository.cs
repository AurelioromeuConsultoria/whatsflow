using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class PatrimonioItemRepository : IPatrimonioItemRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PatrimonioItemRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public PatrimonioItemRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<PatrimonioItem>> GetAllAsync()
    {
        return await Query()
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<PatrimonioItem?> GetByIdAsync(int id)
    {
        return await Query()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PatrimonioItem?> GetByCodigoAsync(string codigo)
    {
        return await _context.Set<PatrimonioItem>()
            .FirstOrDefaultAsync(p => p.Codigo.ToLower() == codigo.ToLower());
    }

    public async Task<PatrimonioItem> CreateAsync(PatrimonioItem item)
    {
        item.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<PatrimonioItem>().Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<PatrimonioItem> UpdateAsync(PatrimonioItem item)
    {
        _context.Set<PatrimonioItem>().Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<PatrimonioItem>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<PatrimonioItem>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    private IQueryable<PatrimonioItem> Query()
    {
        return _context.Set<PatrimonioItem>()
            .Include(p => p.CategoriaPatrimonio)
            .Include(p => p.ResponsavelPessoa)
            .Include(p => p.Fornecedor)
            .Include(p => p.CentroCusto)
            .Include(p => p.Projeto);
    }
}
