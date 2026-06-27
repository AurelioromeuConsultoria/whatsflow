using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class CriancaDetalheRepository : ICriancaDetalheRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CriancaDetalheRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public CriancaDetalheRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<CriancaDetalhe?> GetByPessoaIdAsync(int pessoaId)
    {
        return await _context.Set<CriancaDetalhe>()
            .Include(c => c.Pessoa)
            .FirstOrDefaultAsync(c => c.PessoaId == pessoaId);
    }

    public async Task<CriancaDetalhe> CreateAsync(CriancaDetalhe detalhe)
    {
        detalhe.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<CriancaDetalhe>().Add(detalhe);
        await _context.SaveChangesAsync();
        return detalhe;
    }

    public Task<CriancaDetalhe> CreateWithoutSaveAsync(CriancaDetalhe detalhe)
    {
        detalhe.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<CriancaDetalhe>().Add(detalhe);
        return Task.FromResult(detalhe);
    }

    public async Task<CriancaDetalhe> UpdateAsync(CriancaDetalhe detalhe)
    {
        _context.Set<CriancaDetalhe>().Update(detalhe);
        await _context.SaveChangesAsync();
        return detalhe;
    }

    public async Task DeleteAsync(int pessoaId)
    {
        var detalhe = await _context.Set<CriancaDetalhe>()
            .FirstOrDefaultAsync(c => c.PessoaId == pessoaId);
        if (detalhe != null)
        {
            _context.Set<CriancaDetalhe>().Remove(detalhe);
            await _context.SaveChangesAsync();
        }
    }
}

