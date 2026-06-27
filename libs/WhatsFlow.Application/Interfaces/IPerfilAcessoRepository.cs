using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IPerfilAcessoRepository
{
    Task<IEnumerable<PerfilAcesso>> GetAllAsync();
    Task<PerfilAcesso?> GetByIdAsync(int id);
    Task<PerfilAcesso?> GetByIdIgnoringTenantAsync(int id);
    Task<PerfilAcesso> CreateAsync(PerfilAcesso perfil);
    Task<PerfilAcesso> UpdateAsync(PerfilAcesso perfil);
    Task DeleteAsync(int id);
}
