using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public UsuarioRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public UsuarioRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        return await _context.Set<Usuario>()
            .Include(u => u.Tenant)
            .Include(u => u.PerfilAcesso)
                .ThenInclude(p => p!.Permissoes)
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        return await _context.Set<Usuario>()
            .Include(u => u.Tenant)
            .Include(u => u.PerfilAcesso)
                .ThenInclude(p => p!.Permissoes)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> GetByEmailAsync(string email, string? tenantSlug = null)
    {
        // Login self-service é só por e-mail+senha: o e-mail é único globalmente, então a
        // busca ignora o filtro de tenant para localizar o usuário em qualquer organização.
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var query = _context.Set<Usuario>()
                .Include(u => u.Tenant)
                .Include(u => u.PerfilAcesso)
                    .ThenInclude(p => p!.Permissoes)
                .Where(u => u.EmailLogin.ToLower() == email.ToLower());

            if (!string.IsNullOrWhiteSpace(tenantSlug))
            {
                var normalizedSlug = tenantSlug.Trim().ToLowerInvariant();
                query = query.Where(u => u.Tenant.Slug.ToLower() == normalizedSlug);
            }

            return await query.FirstOrDefaultAsync();
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        _context.Set<Usuario>().Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario> UpdateAsync(Usuario usuario)
    {
        _context.Set<Usuario>().Update(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<Usuario>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Usuario>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExisteAlgumUsuarioAsync(string? tenantSlug = null)
    {
        var query = _context.Set<Usuario>().AsQueryable();

        if (_tenantContext.TenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == _tenantContext.TenantId.Value);
        }
        else
        {
            var normalizedSlug = string.IsNullOrWhiteSpace(tenantSlug)
                ? Tenant.InitialTenantSlug
                : tenantSlug.Trim().ToLowerInvariant();

            query = query.Where(u => u.Tenant.Slug.ToLower() == normalizedSlug);
        }

        return await query.AnyAsync();
    }

    public async Task<int> ResolveTenantIdAsync(string? tenantSlug = null)
    {
        if (_tenantContext.TenantId.HasValue && _tenantContext.TenantId.Value > 0)
        {
            return _tenantContext.TenantId.Value;
        }

        var normalizedSlug = string.IsNullOrWhiteSpace(tenantSlug)
            ? Tenant.InitialTenantSlug
            : tenantSlug.Trim().ToLowerInvariant();

        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var tenantId = await _context.Tenants
                .Where(t => t.Slug.ToLower() == normalizedSlug)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            return tenantId == 0 ? Tenant.InitialTenantId : tenantId;
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }
}
