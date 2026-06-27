using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class PerfilAcessoRepository : IPerfilAcessoRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PerfilAcessoRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public PerfilAcessoRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<PerfilAcesso>> GetAllAsync()
    {
        return await _context.Set<PerfilAcesso>()
            .Include(p => p.Permissoes)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<PerfilAcesso?> GetByIdAsync(int id)
    {
        return await _context.Set<PerfilAcesso>()
            .Include(p => p.Permissoes)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PerfilAcesso?> GetByIdIgnoringTenantAsync(int id)
    {
        var previousIgnoreTenantFilters = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            return await _context.Set<PerfilAcesso>()
                .Include(p => p.Permissoes)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        finally
        {
            _context.IgnoreTenantFilters = previousIgnoreTenantFilters;
        }
    }

    public async Task<PerfilAcesso> CreateAsync(PerfilAcesso perfil)
    {
        var tenantId = _tenantContext.TenantId ?? perfil.TenantId;
        perfil.TenantId = tenantId == 0 ? Tenant.InitialTenantId : tenantId;
        foreach (var permissao in perfil.Permissoes)
        {
            permissao.TenantId = perfil.TenantId;
        }

        _context.Set<PerfilAcesso>().Add(perfil);
        await _context.SaveChangesAsync();
        return perfil;
    }

    public async Task<PerfilAcesso> UpdateAsync(PerfilAcesso perfil)
    {
        var tenantId = _tenantContext.TenantId ?? perfil.TenantId;

        perfil.TenantId = tenantId == 0 ? Tenant.InitialTenantId : tenantId;
        foreach (var permissao in perfil.Permissoes)
        {
            permissao.TenantId = perfil.TenantId;
        }

        _context.Set<PerfilAcesso>().Update(perfil);
        await _context.SaveChangesAsync();
        return perfil;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<PerfilAcesso>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<PerfilAcesso>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
