using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IContaBancariaRepository
{
    Task<IEnumerable<ContaBancaria>> GetAllAsync();
    Task<ContaBancaria?> GetByIdAsync(int id);
    Task<ContaBancaria> CreateAsync(ContaBancaria contaBancaria);
    Task<ContaBancaria> UpdateAsync(ContaBancaria contaBancaria);
    Task DeleteAsync(int id);
}
