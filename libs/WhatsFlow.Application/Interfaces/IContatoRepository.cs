using WhatsFlow.Application.DTOs;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IContatoRepository
{
    Task<IEnumerable<Contato>> GetAllAsync();
    Task<(IReadOnlyList<Contato> Items, int Total)> GetPagedAsync(ContatoPagedQueryDto query);
    Task<Contato?> GetByIdAsync(int id);
    Task<Contato?> GetByTelefoneWhatsAppAsync(string telefoneWhatsApp, int? ignoreId = null);
    Task<IReadOnlyList<Tag>> GetTagsByIdsAsync(IEnumerable<int> tagIds);
    Task<Contato> CreateAsync(Contato contato);
    Task<Contato> UpdateAsync(Contato contato);
    Task DeleteAsync(int id);
}
