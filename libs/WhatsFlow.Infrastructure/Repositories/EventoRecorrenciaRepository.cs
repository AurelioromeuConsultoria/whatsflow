using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class EventoRecorrenciaRepository : IEventoRecorrenciaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public EventoRecorrenciaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public EventoRecorrenciaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<EventoRecorrencia>> GetByEventoAsync(int eventoId)
    {
        return await _context.EventosRecorrencias
            .Where(r => r.EventoId == eventoId)
            .OrderBy(r => r.DiaSemana)
            .ThenBy(r => r.HoraInicio)
            .ToListAsync();
    }

    public async Task<EventoRecorrencia?> GetByIdAsync(int id)
    {
        return await _context.EventosRecorrencias.FindAsync(id);
    }

    public async Task<EventoRecorrencia> CreateAsync(EventoRecorrencia recorrencia)
    {
        recorrencia.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.EventosRecorrencias.Add(recorrencia);
        await _context.SaveChangesAsync();
        return recorrencia;
    }

    public async Task<EventoRecorrencia> UpdateAsync(EventoRecorrencia recorrencia)
    {
        _context.EventosRecorrencias.Update(recorrencia);
        await _context.SaveChangesAsync();
        return recorrencia;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.EventosRecorrencias.FindAsync(id);
        if (entity != null)
        {
            _context.EventosRecorrencias.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
