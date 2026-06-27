using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class PessoaPerfilRepository : IPessoaPerfilRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PessoaPerfilRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public PessoaPerfilRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PessoaPerfil?> GetPerfilAtivoAsync(int pessoaId, PerfilPessoa perfil)
    {
        var tenantId = await ResolveTenantIdAsync();
        return await _context.Set<PessoaPerfil>()
            .FirstOrDefaultAsync(p => p.PessoaId == pessoaId
                && p.TenantId == tenantId
                && p.Perfil == perfil
                && p.DataFim == null);
    }

    public async Task<PessoaPerfil> CreateAsync(PessoaPerfil pessoaPerfil)
    {
        pessoaPerfil.TenantId = await ResolveTenantIdAsync();
        _context.Set<PessoaPerfil>().Add(pessoaPerfil);
        await _context.SaveChangesAsync();
        return pessoaPerfil;
    }

    public async Task<PessoaPerfil> CreateWithoutSaveAsync(PessoaPerfil pessoaPerfil)
    {
        pessoaPerfil.TenantId = await ResolveTenantIdAsync();
        _context.Set<PessoaPerfil>().Add(pessoaPerfil);
        return pessoaPerfil;
    }

    public async Task<IEnumerable<PessoaPerfil>> GetAllAsync()
    {
        var tenantId = await ResolveTenantIdAsync();
        return await _context.Set<PessoaPerfil>()
            .Include(p => p.Pessoa)
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.DataInicio)
            .ToListAsync();
    }

    public async Task<PessoaPerfil?> GetByIdAsync(int id)
    {
        var tenantId = await ResolveTenantIdAsync();
        return await _context.Set<PessoaPerfil>()
            .Include(p => p.Pessoa)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
    }

    public async Task<PessoaPerfil> UpdateAsync(PessoaPerfil pessoaPerfil)
    {
        _context.Set<PessoaPerfil>().Update(pessoaPerfil);
        await _context.SaveChangesAsync();
        return pessoaPerfil;
    }

    public async Task DeleteAsync(int id)
    {
        var tenantId = await ResolveTenantIdAsync();
        var entity = await _context.Set<PessoaPerfil>()
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (entity != null)
        {
            _context.Set<PessoaPerfil>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<PessoaPerfil>> GetPerfisPorPessoaAsync(int pessoaId)
    {
        var tenantId = await ResolveTenantIdAsync();
        return await _context.Set<PessoaPerfil>()
            .Include(p => p.Pessoa)
            .Where(p => p.PessoaId == pessoaId && p.TenantId == tenantId)
            .OrderByDescending(p => p.DataInicio)
            .ToListAsync();
    }

    private Task<int> ResolveTenantIdAsync()
    {
        return Task.FromResult(_tenantContext.TenantId ?? Tenant.InitialTenantId);
    }
}


