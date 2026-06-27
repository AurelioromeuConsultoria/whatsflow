using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class EventoOcorrenciaRepository : IEventoOcorrenciaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public EventoOcorrenciaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public EventoOcorrenciaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<EventoOcorrencia>> GetByEventoAsync(int eventoId)
    {
        return await _context.EventosOcorrencias
            .Include(o => o.Evento)
            .Include(o => o.Escalas)
            .Where(o => o.EventoId == eventoId)
            .OrderBy(o => o.DataHoraInicio)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventoOcorrencia>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? eventoId = null)
    {
        var query = _context.EventosOcorrencias
            .Include(o => o.Evento)
            .Include(o => o.Escalas)
            .Where(o => o.DataHoraInicio >= dataInicio && o.DataHoraInicio <= dataFim);

        if (eventoId.HasValue)
        {
            query = query.Where(o => o.EventoId == eventoId.Value);
        }

        return await query
            .OrderBy(o => o.DataHoraInicio)
            .ToListAsync();
    }

    public async Task<EventoOcorrencia?> GetByIdAsync(int id)
    {
        return await _context.EventosOcorrencias
            .Include(o => o.Evento)
            .Include(o => o.Escalas)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.EventosOcorrencias.AnyAsync(o => o.Id == id);
    }

    public async Task<EventoOcorrencia> CreateAsync(EventoOcorrencia ocorrencia)
    {
        ocorrencia.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.EventosOcorrencias.Add(ocorrencia);
        await _context.SaveChangesAsync();
        return ocorrencia;
    }

    public async Task<EventoOcorrencia> UpdateAsync(EventoOcorrencia ocorrencia)
    {
        _context.EventosOcorrencias.Update(ocorrencia);
        await _context.SaveChangesAsync();
        return ocorrencia;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.EventosOcorrencias.FindAsync(id);
        if (entity == null) return;

        _context.EventosOcorrencias.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<EventoRecorrencia>> GetRecorrenciasAtivasByEventoAsync(int eventoId)
    {
        return await _context.EventosRecorrencias
            .Where(r => r.EventoId == eventoId && r.Ativo)
            .OrderBy(r => r.DiaSemana)
            .ThenBy(r => r.HoraInicio)
            .ToListAsync();
    }

    public async Task<bool> ExistsOcorrenciaNoHorarioAsync(int eventoId, DateTime dataHoraInicio)
    {
        return await _context.EventosOcorrencias.AnyAsync(o =>
            o.EventoId == eventoId &&
            o.DataHoraInicio == dataHoraInicio);
    }
}
