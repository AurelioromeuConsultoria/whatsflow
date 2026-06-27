using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class OrcamentoCategoriaRepository : IOrcamentoCategoriaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public OrcamentoCategoriaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public OrcamentoCategoriaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext()) { }

    private IQueryable<OrcamentoCategoria> WithIncludes()
    {
        return _context.Set<OrcamentoCategoria>()
            .Include(o => o.CategoriaReceita)
            .Include(o => o.CategoriaDespesa);
    }

    public async Task<IEnumerable<OrcamentoCategoria>> GetByAnoAsync(int ano)
    {
        return await WithIncludes()
            .Where(o => o.Ano == ano)
            .OrderBy(o => o.Tipo)
            .ThenBy(o => o.CategoriaReceita != null ? o.CategoriaReceita.Nome : o.CategoriaDespesa!.Nome)
            .ToListAsync();
    }

    public async Task<OrcamentoCategoria?> GetByIdAsync(int id)
    {
        return await WithIncludes().FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OrcamentoCategoria?> FindAsync(int ano, TipoOrcamento tipo, int? categoriaReceitaId, int? categoriaDespesaId)
    {
        return await _context.Set<OrcamentoCategoria>()
            .FirstOrDefaultAsync(o =>
                o.Ano == ano &&
                o.Tipo == tipo &&
                o.CategoriaReceitaId == categoriaReceitaId &&
                o.CategoriaDespesaId == categoriaDespesaId);
    }

    public async Task<OrcamentoCategoria> CreateAsync(OrcamentoCategoria entity)
    {
        entity.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<OrcamentoCategoria>().Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<OrcamentoCategoria> UpdateAsync(OrcamentoCategoria entity)
    {
        _context.Set<OrcamentoCategoria>().Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<OrcamentoCategoria>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<OrcamentoCategoria>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
