using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IWhatsAppAccountRepository
{
    Task<IReadOnlyList<WhatsAppAccount>> GetAllAsync();
    Task<WhatsAppAccount?> GetByIdAsync(int id);
    Task<WhatsAppAccount> CreateAsync(WhatsAppAccount account);
    Task<WhatsAppAccount> UpdateAsync(WhatsAppAccount account);
    Task DeleteAsync(int id);
}
