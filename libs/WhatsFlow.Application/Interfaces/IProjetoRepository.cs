using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IProjetoRepository
{
    Task<IEnumerable<Projeto>> GetAllAsync();
    Task<Projeto?> GetByIdAsync(int id);
    Task<Projeto> CreateAsync(Projeto projeto);
    Task<Projeto> UpdateAsync(Projeto projeto);
    Task DeleteAsync(int id);
}
