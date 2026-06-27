using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class EquipeRepository : IEquipeRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public EquipeRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public EquipeRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Equipe>> GetAllAsync()
    {
        return await _context.Set<Equipe>()
            .Include(e => e.LiderUsuario)
                .ThenInclude(u => u!.Pessoa)
            .Include(e => e.Voluntarios)
            .OrderBy(e => e.Nome)
            .ToListAsync();
    }

    public async Task<Equipe?> GetByIdAsync(int id)
    {
        return await _context.Set<Equipe>()
            .Include(e => e.LiderUsuario)
                .ThenInclude(u => u!.Pessoa)
            .Include(e => e.Voluntarios)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> IsLiderUsuarioDaEquipeAsync(int usuarioId, int equipeId)
    {
        return await _context.Set<Equipe>()
            .AnyAsync(e => e.Id == equipeId && e.LiderUsuarioId == usuarioId);
    }

    public async Task<Equipe> CreateAsync(Equipe equipe)
    {
        equipe.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<Equipe>().Add(equipe);
        await _context.SaveChangesAsync();
        return equipe;
    }

    public async Task<Equipe> UpdateAsync(Equipe equipe)
    {
        _context.Set<Equipe>().Update(equipe);
        await _context.SaveChangesAsync();
        return equipe;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<Equipe>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Equipe>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
