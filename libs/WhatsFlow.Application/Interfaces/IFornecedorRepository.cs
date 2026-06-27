using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IFornecedorRepository
{
    Task<IEnumerable<Fornecedor>> GetAllAsync();
    Task<Fornecedor?> GetByIdAsync(int id);
    Task<Fornecedor> CreateAsync(Fornecedor fornecedor);
    Task<Fornecedor> UpdateAsync(Fornecedor fornecedor);
    Task DeleteAsync(int id);
}
