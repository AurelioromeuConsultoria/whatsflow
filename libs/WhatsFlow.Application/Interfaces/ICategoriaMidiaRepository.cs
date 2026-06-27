using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface ICategoriaMidiaRepository
{
    Task<IEnumerable<CategoriaMidia>> GetAllAsync();
    Task<CategoriaMidia?> GetByIdAsync(int id);
    Task<CategoriaMidia> CreateAsync(CategoriaMidia categoria);
    Task<CategoriaMidia> UpdateAsync(CategoriaMidia categoria);
    Task<bool> DeleteAsync(int id);
}





