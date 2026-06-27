using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IEquipeRepository
{
    Task<IEnumerable<Equipe>> GetAllAsync();
    Task<Equipe?> GetByIdAsync(int id);
    Task<bool> IsLiderUsuarioDaEquipeAsync(int usuarioId, int equipeId);
    Task<Equipe> CreateAsync(Equipe equipe);
    Task<Equipe> UpdateAsync(Equipe equipe);
    Task DeleteAsync(int id);
}
