using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class EscalaRepository : IEscalaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public EscalaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public EscalaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    private static IQueryable<Escala> WithFullItensIncludes(IQueryable<Escala> q) =>
        q.Include(e => e.EventoOcorrencia)
             .ThenInclude(o => o.Evento)
         .Include(e => e.Equipe)
         .Include(e => e.CriadoPorUsuario)
             .ThenInclude(u => u!.Pessoa)
         .Include(e => e.Itens)
             .ThenInclude(i => i.Pessoa)
         .Include(e => e.Itens)
             .ThenInclude(i => i.Equipe)
         .Include(e => e.Itens)
             .ThenInclude(i => i.Cargo)
         .Include(e => e.Itens)
             .ThenInclude(i => i.Voluntario)
         .Include(e => e.Itens)
             .ThenInclude(i => i.AprovadoPorUsuario)
                 .ThenInclude(u => u!.Pessoa)
         .Include(e => e.Itens)
             .ThenInclude(i => i.RespondidoPorUsuario)
                 .ThenInclude(u => u!.Pessoa);

    public async Task<Escala?> GetByIdAsync(int id)
    {
        return await WithFullItensIncludes(_context.Escalas)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Escala?> GetByEventoOcorrenciaIdAsync(int eventoOcorrenciaId)
    {
        return await WithFullItensIncludes(_context.Escalas)
            .FirstOrDefaultAsync(e => e.EventoOcorrenciaId == eventoOcorrenciaId);
    }

    public async Task<Escala?> GetByEventoOcorrenciaAndEquipeAsync(int eventoOcorrenciaId, int equipeId)
    {
        return await WithFullItensIncludes(_context.Escalas)
            .FirstOrDefaultAsync(e => e.EventoOcorrenciaId == eventoOcorrenciaId && e.EquipeId == equipeId);
    }

    public async Task<IEnumerable<Escala>> GetAllByEventoOcorrenciaAsync(int eventoOcorrenciaId)
    {
        return await WithFullItensIncludes(_context.Escalas)
            .Where(e => e.EventoOcorrenciaId == eventoOcorrenciaId)
            .OrderBy(e => e.EquipeId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Escala>> GetByPessoaIdAsync(int pessoaId, bool somenteFuturas = false)
    {
        var query = WithFullItensIncludes(_context.Escalas)
            .Where(e => e.Itens.Any(i => i.PessoaId == pessoaId));

        if (somenteFuturas)
        {
            query = query.Where(e => e.EventoOcorrencia.DataHoraInicio >= DateTime.Now.AddDays(-1));
        }

        return await query
            .OrderBy(e => e.EventoOcorrencia.DataHoraInicio)
            .ToListAsync();
    }

    public async Task<Escala> CreateAsync(Escala escala)
    {
        escala.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Escalas.Add(escala);
        await _context.SaveChangesAsync();
        return escala;
    }

    public async Task<Escala> UpdateAsync(Escala escala)
    {
        _context.Escalas.Update(escala);
        await _context.SaveChangesAsync();
        return escala;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Escalas.FindAsync(id);
        if (entity == null) return;

        _context.Escalas.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<EscalaItem?> GetItemByIdAsync(int escalaItemId)
    {
        return await _context.EscalasItens
            .Include(i => i.Escala)
                .ThenInclude(e => e.EventoOcorrencia)
                    .ThenInclude(o => o.Evento)
            .Include(i => i.Escala)
                .ThenInclude(e => e.Equipe)
            .Include(i => i.Equipe)
            .Include(i => i.Cargo)
            .Include(i => i.Pessoa)
            .Include(i => i.Voluntario)
            .Include(i => i.AprovadoPorUsuario)
                .ThenInclude(u => u!.Pessoa)
            .Include(i => i.RespondidoPorUsuario)
                .ThenInclude(u => u!.Pessoa)
            .FirstOrDefaultAsync(i => i.Id == escalaItemId);
    }

    public async Task<IEnumerable<EscalaItem>> GetItensComOcorrenciaNoPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? equipeId = null, int? eventoId = null)
    {
        var query = _context.EscalasItens
            .Include(i => i.Escala)
                .ThenInclude(e => e.EventoOcorrencia)
                    .ThenInclude(o => o.Evento)
            .Include(i => i.Equipe)
            .Include(i => i.Cargo)
            .Include(i => i.Pessoa)
            .Include(i => i.Voluntario)
            .Where(i =>
                i.Escala.EventoOcorrencia.DataHoraInicio >= dataInicio &&
                i.Escala.EventoOcorrencia.DataHoraInicio <= dataFim);

        if (equipeId.HasValue)
        {
            query = query.Where(i => i.EquipeId == equipeId.Value);
        }

        if (eventoId.HasValue)
        {
            query = query.Where(i => i.Escala.EventoOcorrencia.EventoId == eventoId.Value);
        }

        return await query
            .OrderBy(i => i.Escala.EventoOcorrencia.DataHoraInicio)
            .ToListAsync();
    }

    public async Task<EscalaItem> AddItemAsync(EscalaItem item)
    {
        item.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.EscalasItens.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<EscalaItem> UpdateItemAsync(EscalaItem item)
    {
        _context.EscalasItens.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task DeleteItemAsync(int escalaItemId)
    {
        var entity = await _context.EscalasItens.FindAsync(escalaItemId);
        if (entity == null) return;

        _context.EscalasItens.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<EscalaItem?> GetConflitoPessoaNaEscalaAsync(int escalaId, int voluntarioId, int? ignorarEscalaItemId = null)
    {
        var escala = await _context.Escalas.FindAsync(escalaId);
        if (escala == null) return null;

        var pessoaId = await _context.Voluntarios
            .Where(v => v.Id == voluntarioId)
            .Select(v => v.PessoaId)
            .FirstOrDefaultAsync();

        if (pessoaId == 0) return null;

        // Conflito: mesma pessoa já escalada em qualquer equipe desta ocorrência
        return await _context.EscalasItens
            .Include(i => i.Equipe)
            .Include(i => i.Pessoa)
            .Where(i =>
                i.Escala.EventoOcorrenciaId == escala.EventoOcorrenciaId &&
                i.PessoaId == pessoaId &&
                (!ignorarEscalaItemId.HasValue || i.Id != ignorarEscalaItemId.Value))
            .FirstOrDefaultAsync();
    }

    public async Task<HashSet<int>> GetPessoaIdsJaEscaladasAsync(int escalaId)
    {
        var escala = await _context.Escalas.FindAsync(escalaId);
        if (escala == null) return new HashSet<int>();

        // Pessoas já escaladas em qualquer equipe desta ocorrência
        var pessoaIds = await _context.EscalasItens
            .Where(i => i.Escala.EventoOcorrenciaId == escala.EventoOcorrenciaId)
            .Select(i => i.PessoaId)
            .Distinct()
            .ToListAsync();

        return pessoaIds.ToHashSet();
    }

    public async Task<Dictionary<int, int>> GetCargaRecentePorVoluntarioAsync(int equipeId, DateTime dataMinima)
    {
        return await _context.EscalasItens
            .Where(i =>
                i.EquipeId == equipeId &&
                i.VoluntarioId != null &&
                i.Escala.EventoOcorrencia.DataHoraInicio >= dataMinima)
            .GroupBy(i => i.VoluntarioId!.Value)
            .Select(g => new { VoluntarioId = g.Key, Quantidade = g.Count() })
            .ToDictionaryAsync(x => x.VoluntarioId, x => x.Quantidade);
    }

    public async Task<Dictionary<int, int>> GetQuantidadeEscalasNoMesPorVoluntarioAsync(int equipeId, int ano, int mes)
    {
        var inicio = new DateTime(ano, mes, 1, 0, 0, 0);
        var fim = inicio.AddMonths(1).AddTicks(-1);
        return await GetQuantidadeEscalasEmPeriodoPorVoluntarioAsync(equipeId, inicio, fim);
    }

    public async Task<Dictionary<int, int>> GetQuantidadeEscalasEmPeriodoPorVoluntarioAsync(int equipeId, DateTime dataInicio, DateTime dataFim)
    {
        return await _context.EscalasItens
            .Where(i =>
                i.EquipeId == equipeId &&
                i.VoluntarioId != null &&
                i.Escala.EventoOcorrencia.DataHoraInicio >= dataInicio &&
                i.Escala.EventoOcorrencia.DataHoraInicio <= dataFim)
            .GroupBy(i => i.VoluntarioId!.Value)
            .Select(g => new { VoluntarioId = g.Key, Quantidade = g.Count() })
            .ToDictionaryAsync(x => x.VoluntarioId, x => x.Quantidade);
    }
}
