using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ReceitaRepository : IReceitaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ReceitaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public ReceitaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    private IQueryable<Receita> WithIncludes()
    {
        return _context.Set<Receita>()
            .Include(r => r.CategoriaReceita)
            .Include(r => r.ContaBancaria)
            .Include(r => r.CentroCusto)
            .Include(r => r.Projeto)
            .Include(r => r.Pessoa)
            .Include(r => r.Usuario)
                .ThenInclude(u => u!.Pessoa);
    }

    public async Task<IEnumerable<Receita>> GetAllAsync()
    {
        return await WithIncludes()
            .OrderByDescending(r => r.DataRecebimento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Receita>> GetByPessoaIdAsync(int pessoaId)
    {
        return await WithIncludes()
            .Where(r => r.PessoaId == pessoaId)
            .OrderByDescending(r => r.DataRecebimento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Receita>> GetContribuicoesNoPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? categoriaId = null)
    {
        var query = WithIncludes()
            .Where(r => r.PessoaId != null
                     && r.DataRecebimento >= dataInicio
                     && r.DataRecebimento <= dataFim
                     && r.Status != StatusReceita.Cancelada);

        if (categoriaId.HasValue)
            query = query.Where(r => r.CategoriaReceitaId == categoriaId.Value);

        return await query.OrderByDescending(r => r.DataRecebimento).ToListAsync();
    }

    public async Task<IEnumerable<Receita>> GetPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
    {
        return await WithIncludes()
            .Where(r => r.DataRecebimento >= dataInicio
                     && r.DataRecebimento <= dataFim
                     && r.Status != StatusReceita.Cancelada)
            .OrderBy(r => r.DataRecebimento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Receita>> GetInformeAnualAsync(int pessoaId, int ano)
    {
        return await WithIncludes()
            .Where(r => r.PessoaId == pessoaId
                     && r.DataRecebimento.Year == ano
                     && r.Status != StatusReceita.Cancelada)
            .OrderBy(r => r.DataRecebimento)
            .ToListAsync();
    }

    public async Task<Receita?> GetByIdAsync(int id)
    {
        return await WithIncludes()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Receita> CreateAsync(Receita receita)
    {
        receita.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<Receita>().Add(receita);
        await _context.SaveChangesAsync();
        return receita;
    }

    public async Task<Receita> UpdateAsync(Receita receita)
    {
        _context.Set<Receita>().Update(receita);
        await _context.SaveChangesAsync();
        return receita;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<Receita>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Receita>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
