using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByEmailAsync(string email, string? tenantSlug = null);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task<Usuario> UpdateAsync(Usuario usuario);
    Task DeleteAsync(int id);
    Task<bool> ExisteAlgumUsuarioAsync(string? tenantSlug = null);
    Task<int> ResolveTenantIdAsync(string? tenantSlug = null);
}
