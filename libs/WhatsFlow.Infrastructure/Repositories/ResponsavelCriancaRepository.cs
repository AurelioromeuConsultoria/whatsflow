using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ResponsavelCriancaRepository : IResponsavelCriancaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ResponsavelCriancaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public ResponsavelCriancaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<ResponsavelCrianca>> GetByCriancaIdAsync(int criancaPessoaId)
    {
        return await _context.Set<ResponsavelCrianca>()
            .Include(r => r.Responsavel)
            .Where(r => r.CriancaPessoaId == criancaPessoaId && r.Ativo)
            .ToListAsync();
    }

    public async Task<IEnumerable<ResponsavelCrianca>> GetByResponsavelIdAsync(int responsavelPessoaId)
    {
        return await _context.Set<ResponsavelCrianca>()
            .Include(r => r.Crianca)
            .Where(r => r.ResponsavelPessoaId == responsavelPessoaId && r.Ativo)
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetResponsavelIdsAtivosAsync()
    {
        return await _context.Set<ResponsavelCrianca>()
            .Where(r => r.Ativo)
            .Select(r => r.ResponsavelPessoaId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetResponsavelIdsAtivosByCriancaIdsAsync(IEnumerable<int> criancaPessoaIds)
    {
        var ids = criancaPessoaIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        return await _context.Set<ResponsavelCrianca>()
            .Where(r => r.Ativo && ids.Contains(r.CriancaPessoaId))
            .Select(r => r.ResponsavelPessoaId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetCriancaIdsAtivosByResponsavelIdAsync(int responsavelPessoaId)
    {
        return await _context.Set<ResponsavelCrianca>()
            .Where(r => r.ResponsavelPessoaId == responsavelPessoaId && r.Ativo)
            .Select(r => r.CriancaPessoaId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<ResponsavelCrianca?> GetByIdAsync(int id)
    {
        return await _context.Set<ResponsavelCrianca>()
            .Include(r => r.Crianca)
            .Include(r => r.Responsavel)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ResponsavelCrianca?> GetByCriancaAndResponsavelAsync(int criancaPessoaId, int responsavelPessoaId)
    {
        return await _context.Set<ResponsavelCrianca>()
            .FirstOrDefaultAsync(r => r.CriancaPessoaId == criancaPessoaId && 
                                      r.ResponsavelPessoaId == responsavelPessoaId);
    }

    public async Task<bool> ExisteVinculoAtivoAsync(int criancaPessoaId, int responsavelPessoaId)
    {
        return await _context.Set<ResponsavelCrianca>()
            .AnyAsync(r => r.CriancaPessoaId == criancaPessoaId &&
                           r.ResponsavelPessoaId == responsavelPessoaId &&
                           r.Ativo);
    }

    public async Task<ResponsavelCrianca> CreateAsync(ResponsavelCrianca responsavel)
    {
        responsavel.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<ResponsavelCrianca>().Add(responsavel);
        await _context.SaveChangesAsync();
        return responsavel;
    }

    public Task<ResponsavelCrianca> CreateWithoutSaveAsync(ResponsavelCrianca responsavel)
    {
        responsavel.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<ResponsavelCrianca>().Add(responsavel);
        return Task.FromResult(responsavel);
    }

    public async Task<ResponsavelCrianca> UpdateAsync(ResponsavelCrianca responsavel)
    {
        _context.Set<ResponsavelCrianca>().Update(responsavel);
        await _context.SaveChangesAsync();
        return responsavel;
    }

    public async Task DeleteAsync(int id)
    {
        var responsavel = await _context.Set<ResponsavelCrianca>().FindAsync(id);
        if (responsavel != null)
        {
            responsavel.Ativo = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> PodeRetirarAsync(int criancaPessoaId, int responsavelPessoaId)
    {
        var responsavel = await _context.Set<ResponsavelCrianca>()
            .FirstOrDefaultAsync(r => r.CriancaPessoaId == criancaPessoaId && 
                                      r.ResponsavelPessoaId == responsavelPessoaId && 
                                      r.Ativo);
        return responsavel?.PodeRetirar ?? false;
    }
}
