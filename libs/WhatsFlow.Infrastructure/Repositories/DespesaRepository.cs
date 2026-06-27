using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class DespesaRepository : IDespesaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public DespesaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public DespesaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    private IQueryable<Despesa> WithIncludes()
    {
        return _context.Set<Despesa>()
            .Include(d => d.Fornecedor)
            .Include(d => d.CategoriaDespesa)
            .Include(d => d.ContaBancaria)
            .Include(d => d.CentroCusto)
            .Include(d => d.Projeto)
            .Include(d => d.Usuario)
                .ThenInclude(u => u!.Pessoa);
    }

    public async Task<IEnumerable<Despesa>> GetAllAsync()
    {
        return await WithIncludes()
            .OrderByDescending(d => d.DataVencimento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Despesa>> GetPendentesAteDataAsync(DateTime ate)
    {
        return await WithIncludes()
            .Where(d => d.Status == StatusDespesa.Pendente && d.DataVencimento.Date <= ate.Date)
            .OrderBy(d => d.DataVencimento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Despesa>> GetPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
    {
        return await WithIncludes()
            .Where(d => d.Status != StatusDespesa.Cancelada
                     && d.DataVencimento.Date >= dataInicio.Date
                     && d.DataVencimento.Date <= dataFim.Date)
            .OrderBy(d => d.DataVencimento)
            .ToListAsync();
    }

    public async Task<Despesa?> GetByIdAsync(int id)
    {
        return await WithIncludes()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Despesa> CreateAsync(Despesa despesa)
    {
        despesa.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<Despesa>().Add(despesa);
        await _context.SaveChangesAsync();
        return despesa;
    }

    public async Task<Despesa> UpdateAsync(Despesa despesa)
    {
        _context.Set<Despesa>().Update(despesa);
        await _context.SaveChangesAsync();
        return despesa;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<Despesa>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Despesa>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
