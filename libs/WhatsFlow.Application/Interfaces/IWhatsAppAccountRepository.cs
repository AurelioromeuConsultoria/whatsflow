using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IWhatsAppAccountRepository
{
    Task<IReadOnlyList<WhatsAppAccount>> GetAllAsync();
    Task<WhatsAppAccount?> GetByIdAsync(int id);

    /// <summary>Primeira conta de WhatsApp ativa do tenant atual (null se nenhuma).</summary>
    Task<WhatsAppAccount?> GetAtivaAsync();
    Task<WhatsAppAccount> CreateAsync(WhatsAppAccount account);
    Task<WhatsAppAccount> UpdateAsync(WhatsAppAccount account);
    Task DeleteAsync(int id);
}
