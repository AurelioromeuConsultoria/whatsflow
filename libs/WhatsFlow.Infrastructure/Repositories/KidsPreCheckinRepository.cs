using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class KidsPreCheckinRepository : IKidsPreCheckinRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public KidsPreCheckinRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public KidsPreCheckinRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<KidsPreCheckin?> GetByIdAsync(int id)
    {
        return await BaseQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<KidsPreCheckin?> GetByQrTokenAsync(string qrToken)
    {
        return await BaseQuery()
            .FirstOrDefaultAsync(x => x.QrToken == qrToken);
    }

    public async Task<KidsPreCheckin?> GetByCodigoCurtoAsync(string codigoCurto)
    {
        return await BaseQuery()
            .FirstOrDefaultAsync(x => x.CodigoCurto == codigoCurto);
    }

    public async Task<KidsPreCheckin?> GetAtivoPorCriancaESessaoAsync(int criancaPessoaId, int? eventoOcorrenciaId)
    {
        return await BaseQuery()
            .FirstOrDefaultAsync(x =>
                x.CriancaPessoaId == criancaPessoaId &&
                x.EventoOcorrenciaId == eventoOcorrenciaId &&
                x.Status == "Pending");
    }

    public async Task<IEnumerable<KidsPreCheckin>> GetByResponsavelIdAsync(int responsavelPessoaId, string? status = null, bool somenteAtivos = false)
    {
        IQueryable<KidsPreCheckin> query = BaseQuery()
            .Where(x => x.ResponsavelPessoaId == responsavelPessoaId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (somenteAtivos)
        {
            query = query.Where(x => x.Status == "Pending" || x.Status == "Confirmed");
        }

        return await query
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync();
    }

    public async Task<IEnumerable<KidsPreCheckin>> GetPendentesAsync(int? eventoOcorrenciaId = null, string? salaId = null, string? turmaId = null)
    {
        IQueryable<KidsPreCheckin> query = BaseQuery()
            .Where(x => x.Status == "Pending");

        if (eventoOcorrenciaId.HasValue)
        {
            query = query.Where(x => x.EventoOcorrenciaId == eventoOcorrenciaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(salaId))
        {
            query = query.Where(x => x.SalaId == salaId);
        }

        if (!string.IsNullOrWhiteSpace(turmaId))
        {
            query = query.Where(x => x.TurmaId == turmaId);
        }

        return await query
            .OrderBy(x => x.ExpiraEm)
            .ThenBy(x => x.CriadoEm)
            .ToListAsync();
    }

    public async Task<IEnumerable<KidsPreCheckin>> GetExpiradosPendentesAsync(DateTime referenciaUtc)
    {
        return await BaseQuery()
            .Where(x => x.Status == "Pending" && x.ExpiraEm <= referenciaUtc)
            .OrderBy(x => x.ExpiraEm)
            .ToListAsync();
    }

    public async Task<KidsPreCheckin> CreateAsync(KidsPreCheckin preCheckin)
    {
        preCheckin.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<KidsPreCheckin>().Add(preCheckin);
        await _context.SaveChangesAsync();
        return preCheckin;
    }

    public Task<KidsPreCheckin> CreateWithoutSaveAsync(KidsPreCheckin preCheckin)
    {
        preCheckin.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<KidsPreCheckin>().Add(preCheckin);
        return Task.FromResult(preCheckin);
    }

    public async Task<KidsPreCheckin> UpdateAsync(KidsPreCheckin preCheckin)
    {
        _context.Set<KidsPreCheckin>().Update(preCheckin);
        await _context.SaveChangesAsync();
        return preCheckin;
    }

    public Task UpdateWithoutSaveAsync(KidsPreCheckin preCheckin)
    {
        _context.Set<KidsPreCheckin>().Update(preCheckin);
        return Task.CompletedTask;
    }

    private IQueryable<KidsPreCheckin> BaseQuery()
    {
        return _context.Set<KidsPreCheckin>()
            .Include(x => x.Crianca)
            .Include(x => x.Responsavel)
            .Include(x => x.EventoOcorrencia)
            .Include(x => x.Checkin)
            .Include(x => x.ConfirmadoPor)
            .Include(x => x.CanceladoPor);
    }
}
