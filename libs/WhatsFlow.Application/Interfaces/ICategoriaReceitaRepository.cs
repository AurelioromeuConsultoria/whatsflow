using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface ICategoriaReceitaRepository
{
    Task<IEnumerable<CategoriaReceita>> GetAllAsync();
    Task<CategoriaReceita?> GetByIdAsync(int id);
    Task<CategoriaReceita> CreateAsync(CategoriaReceita categoriaReceita);
    Task<CategoriaReceita> UpdateAsync(CategoriaReceita categoriaReceita);
    Task DeleteAsync(int id);
}
