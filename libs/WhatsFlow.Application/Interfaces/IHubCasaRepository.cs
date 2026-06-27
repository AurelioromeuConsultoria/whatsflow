using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IHubCasaRepository
{
    Task<IEnumerable<HubCasa>> GetAllAsync();
    Task<HubCasa?> GetByIdAsync(int id);
    Task<HubCasa> CreateAsync(HubCasa casa);
    Task<HubCasa> UpdateAsync(HubCasa casa);
    Task DeleteAsync(int id);
}
