using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class KidsOcorrenciaRepository : IKidsOcorrenciaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public KidsOcorrenciaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public KidsOcorrenciaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<KidsOcorrencia?> GetByIdAsync(int id)
    {
        return await _context.Set<KidsOcorrencia>()
            .Include(o => o.Crianca)
            .Include(o => o.Checkin)
            .Include(o => o.RegistradoPor)
            .Include(o => o.ContatoResponsavelPor)
            .Include(o => o.EncerradoPor)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<KidsOcorrencia>> GetByCriancaIdAsync(int criancaPessoaId)
    {
        return await _context.Set<KidsOcorrencia>()
            .Include(o => o.Crianca)
            .Include(o => o.Checkin)
            .Include(o => o.RegistradoPor)
            .Include(o => o.ContatoResponsavelPor)
            .Include(o => o.EncerradoPor)
            .Where(o => o.CriancaPessoaId == criancaPessoaId)
            .OrderByDescending(o => o.DataCriacao)
            .ToListAsync();
    }

    public async Task<IEnumerable<KidsOcorrencia>> GetAbertasAsync()
    {
        return await _context.Set<KidsOcorrencia>()
            .Include(o => o.Crianca)
            .Include(o => o.Checkin)
            .Include(o => o.RegistradoPor)
            .Where(o => o.Status != "Encerrada")
            .OrderByDescending(o => o.DataCriacao)
            .ToListAsync();
    }

    public async Task<KidsOcorrencia> CreateAsync(KidsOcorrencia ocorrencia)
    {
        ocorrencia.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<KidsOcorrencia>().Add(ocorrencia);
        await _context.SaveChangesAsync();
        return ocorrencia;
    }

    public async Task<KidsOcorrencia> UpdateAsync(KidsOcorrencia ocorrencia)
    {
        _context.Set<KidsOcorrencia>().Update(ocorrencia);
        await _context.SaveChangesAsync();
        return ocorrencia;
    }
}
