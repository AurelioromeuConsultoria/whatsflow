using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface ICentroCustoRepository
{
    Task<IEnumerable<CentroCusto>> GetAllAsync();
    Task<CentroCusto?> GetByIdAsync(int id);
    Task<CentroCusto> CreateAsync(CentroCusto centroCusto);
    Task<CentroCusto> UpdateAsync(CentroCusto centroCusto);
    Task DeleteAsync(int id);
}
