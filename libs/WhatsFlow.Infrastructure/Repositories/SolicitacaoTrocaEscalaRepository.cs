using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class SolicitacaoTrocaEscalaRepository : ISolicitacaoTrocaEscalaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public SolicitacaoTrocaEscalaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public SolicitacaoTrocaEscalaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<SolicitacaoTrocaEscala?> GetByIdAsync(int id)
    {
        return await Query().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<SolicitacaoTrocaEscala?> GetPendenteByEscalaItemAsync(int escalaItemId)
    {
        return await Query()
            .FirstOrDefaultAsync(x => x.EscalaItemId == escalaItemId && x.Status == StatusSolicitacaoTrocaEscala.Pendente);
    }

    public async Task<IEnumerable<SolicitacaoTrocaEscala>> GetGerenciaveisAsync(int usuarioId, bool isAdmin, int? equipeId, StatusSolicitacaoTrocaEscala? status)
    {
        var query = Query();

        if (!isAdmin)
        {
            query = query.Where(x => x.EscalaItem.Equipe.LiderUsuarioId == usuarioId);
        }

        if (equipeId.HasValue)
        {
            query = query.Where(x => x.EscalaItem.EquipeId == equipeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderBy(x => x.Status == StatusSolicitacaoTrocaEscala.Pendente ? 0 : 1)
            .ThenBy(x => x.EscalaItem.Escala.EventoOcorrencia.DataHoraInicio)
            .ThenByDescending(x => x.DataSolicitacao)
            .ToListAsync();
    }

    public async Task<IEnumerable<SolicitacaoTrocaEscala>> GetByEscalaAsync(int escalaId)
    {
        return await Query()
            .Where(x => x.EscalaItem.EscalaId == escalaId)
            .OrderByDescending(x => x.DataSolicitacao)
            .ToListAsync();
    }

    public async Task<IEnumerable<SolicitacaoTrocaEscala>> GetByPessoaAsync(int pessoaId)
    {
        return await Query()
            .Where(x => x.VoluntarioSolicitante.PessoaId == pessoaId)
            .OrderByDescending(x => x.DataSolicitacao)
            .ToListAsync();
    }

    public async Task<SolicitacaoTrocaEscala> CreateAsync(SolicitacaoTrocaEscala solicitacao)
    {
        solicitacao.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<SolicitacaoTrocaEscala>().Add(solicitacao);
        await _context.SaveChangesAsync();
        return solicitacao;
    }

    public async Task<SolicitacaoTrocaEscala> UpdateAsync(SolicitacaoTrocaEscala solicitacao)
    {
        _context.Set<SolicitacaoTrocaEscala>().Update(solicitacao);
        await _context.SaveChangesAsync();
        return solicitacao;
    }

    private IQueryable<SolicitacaoTrocaEscala> Query()
    {
        return _context.Set<SolicitacaoTrocaEscala>()
            .Include(x => x.EscalaItem)
                .ThenInclude(i => i.Escala)
                    .ThenInclude(e => e.EventoOcorrencia)
                        .ThenInclude(o => o.Evento)
            .Include(x => x.EscalaItem)
                .ThenInclude(i => i.Equipe)
            .Include(x => x.VoluntarioSolicitante)
                .ThenInclude(v => v.Pessoa)
            .Include(x => x.VoluntarioSubstituto)
                .ThenInclude(v => v!.Pessoa)
            .Include(x => x.RespondidoPorUsuario)
                .ThenInclude(u => u!.Pessoa);
    }
}
