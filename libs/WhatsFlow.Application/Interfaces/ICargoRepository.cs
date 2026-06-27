using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface ICargoRepository
{
    Task<IEnumerable<Cargo>> GetAllAsync();
    Task<Cargo?> GetByIdAsync(int id);
    Task<Cargo> CreateAsync(Cargo cargo);
    Task<Cargo> UpdateAsync(Cargo cargo);
    Task DeleteAsync(int id);
}
