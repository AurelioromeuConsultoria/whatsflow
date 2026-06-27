using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ConsentimentoRegistroRepository : IConsentimentoRegistroRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ConsentimentoRegistroRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public ConsentimentoRegistroRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<ConsentimentoRegistro> CreateWithoutSaveAsync(ConsentimentoRegistro registro)
    {
        if (registro.TenantId <= 0)
        {
            registro.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        }

        await _context.Set<ConsentimentoRegistro>().AddAsync(registro);
        return registro;
    }

    public async Task<IEnumerable<ConsentimentoRegistro>> GetByPessoaAsync(int pessoaId)
    {
        return await _context.Set<ConsentimentoRegistro>()
            .Where(c => c.PessoaId == pessoaId)
            .OrderByDescending(c => c.AceitoEm)
            .ToListAsync();
    }
}
