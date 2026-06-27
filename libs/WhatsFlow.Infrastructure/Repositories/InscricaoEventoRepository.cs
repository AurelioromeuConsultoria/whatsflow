using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class InscricaoEventoRepository : IInscricaoEventoRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public InscricaoEventoRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public InscricaoEventoRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<InscricaoEvento>> GetAllAsync()
    {
        return await _context.Set<InscricaoEvento>()
            .Include(i => i.Evento)
            .OrderByDescending(i => i.DataInscricao)
            .ToListAsync();
    }

    public async Task<InscricaoEvento?> GetByIdAsync(int id)
    {
        return await _context.Set<InscricaoEvento>()
            .Include(i => i.Evento)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<InscricaoEvento>> GetByEventoAsync(int eventoId)
    {
        return await _context.Set<InscricaoEvento>()
            .Include(i => i.Evento)
            .Where(i => i.EventoId == eventoId)
            .OrderByDescending(i => i.DataInscricao)
            .ToListAsync();
    }

    public async Task<IEnumerable<InscricaoEvento>> GetByStatusAsync(StatusInscricao status)
    {
        return await _context.Set<InscricaoEvento>()
            .Include(i => i.Evento)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.DataInscricao)
            .ToListAsync();
    }

    public async Task<IEnumerable<InscricaoEvento>> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();

        return await _context.Set<InscricaoEvento>()
            .Include(i => i.Evento)
            .Where(i => i.Email != null && i.Email.ToLower() == normalized)
            .OrderByDescending(i => i.DataInscricao)
            .ToListAsync();
    }

    public async Task<int> ContarInscricoesPorEventoAsync(int eventoId)
    {
        return await _context.Set<InscricaoEvento>()
            .CountAsync(i => i.EventoId == eventoId);
    }

    public async Task<int> ContarInscricoesConfirmadasPorEventoAsync(int eventoId)
    {
        return await _context.Set<InscricaoEvento>()
            .CountAsync(i => i.EventoId == eventoId && i.Status == StatusInscricao.Confirmada);
    }

    public async Task<bool> ExisteInscricaoAsync(int eventoId, string whatsApp)
    {
        return await _context.Set<InscricaoEvento>()
            .AnyAsync(i => i.EventoId == eventoId && i.WhatsApp == whatsApp);
    }

    public async Task<InscricaoEvento> CreateAsync(InscricaoEvento inscricaoEvento)
    {
        inscricaoEvento.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<InscricaoEvento>().Add(inscricaoEvento);
        await _context.SaveChangesAsync();
        return inscricaoEvento;
    }

    public async Task<InscricaoEvento> UpdateAsync(InscricaoEvento inscricaoEvento)
    {
        _context.Set<InscricaoEvento>().Update(inscricaoEvento);
        await _context.SaveChangesAsync();
        return inscricaoEvento;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<InscricaoEvento>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<InscricaoEvento>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}




