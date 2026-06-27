using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class PatrimonioMovimentacaoRepository : IPatrimonioMovimentacaoRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PatrimonioMovimentacaoRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public PatrimonioMovimentacaoRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<PatrimonioMovimentacao>> GetByPatrimonioIdAsync(int patrimonioItemId)
    {
        return await _context.Set<PatrimonioMovimentacao>()
            .Where(m => m.PatrimonioItemId == patrimonioItemId)
            .OrderByDescending(m => m.DataMovimentacao)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task<PatrimonioMovimentacao> CreateAsync(PatrimonioMovimentacao movimentacao)
    {
        movimentacao.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<PatrimonioMovimentacao>().Add(movimentacao);
        await _context.SaveChangesAsync();
        return movimentacao;
    }
}
