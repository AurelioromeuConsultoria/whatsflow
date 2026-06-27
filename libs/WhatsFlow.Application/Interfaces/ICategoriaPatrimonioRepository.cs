using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface ICategoriaPatrimonioRepository
{
    Task<IEnumerable<CategoriaPatrimonio>> GetAllAsync();
    Task<CategoriaPatrimonio?> GetByIdAsync(int id);
    Task<CategoriaPatrimonio?> GetByNomeAsync(string nome);
    Task<CategoriaPatrimonio> CreateAsync(CategoriaPatrimonio categoria);
    Task<CategoriaPatrimonio> UpdateAsync(CategoriaPatrimonio categoria);
    Task DeleteAsync(int id);
}
