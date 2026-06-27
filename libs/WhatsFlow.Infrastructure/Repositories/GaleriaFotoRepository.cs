using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class GaleriaFotoRepository : IGaleriaFotoRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GaleriaFotoRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public GaleriaFotoRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<GaleriaFoto>> GetAllAsync()
    {
        return await _context.Set<GaleriaFoto>()
            .Include(g => g.Evento)
            .Include(g => g.CategoriaMidia)
            .OrderByDescending(g => g.Data)
            .ToListAsync();
    }

    public async Task<IEnumerable<GaleriaFoto>> GetAtivasAsync()
    {
        return await _context.Set<GaleriaFoto>()
            .Include(g => g.Evento)
            .Include(g => g.CategoriaMidia)
            .Where(g => g.Ativo)
            .OrderByDescending(g => g.Data)
            .ToListAsync();
    }

    public async Task<GaleriaFoto?> GetByIdAsync(int id)
    {
        return await _context.Set<GaleriaFoto>()
            .Include(g => g.Evento)
            .Include(g => g.CategoriaMidia)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<IEnumerable<GaleriaFoto>> GetByEventoIdAsync(int eventoId)
    {
        return await _context.Set<GaleriaFoto>()
            .Include(g => g.Evento)
            .Include(g => g.CategoriaMidia)
            .Where(g => g.EventoId == eventoId)
            .OrderByDescending(g => g.Data)
            .ToListAsync();
    }

    public async Task<IEnumerable<GaleriaFoto>> GetByCategoriaMidiaIdAsync(int categoriaMidiaId)
    {
        return await _context.Set<GaleriaFoto>()
            .Include(g => g.Evento)
            .Include(g => g.CategoriaMidia)
            .Where(g => g.CategoriaMidiaId == categoriaMidiaId)
            .OrderByDescending(g => g.Data)
            .ToListAsync();
    }

    public async Task<GaleriaFoto> CreateAsync(GaleriaFoto galeria)
    {
        galeria.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<GaleriaFoto>().Add(galeria);
        await _context.SaveChangesAsync();
        return galeria;
    }

    public async Task<GaleriaFoto> UpdateAsync(GaleriaFoto galeria)
    {
        _context.Set<GaleriaFoto>().Update(galeria);
        await _context.SaveChangesAsync();
        return galeria;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Set<GaleriaFoto>().FindAsync(id);
        if (entity == null) return false;

        _context.Set<GaleriaFoto>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}




