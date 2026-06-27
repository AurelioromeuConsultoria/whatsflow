using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface ICategoriaDespesaRepository
{
    Task<IEnumerable<CategoriaDespesa>> GetAllAsync();
    Task<CategoriaDespesa?> GetByIdAsync(int id);
    Task<CategoriaDespesa> CreateAsync(CategoriaDespesa categoriaDespesa);
    Task<CategoriaDespesa> UpdateAsync(CategoriaDespesa categoriaDespesa);
    Task DeleteAsync(int id);
}
