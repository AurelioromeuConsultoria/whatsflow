using WhatsFlow.Domain.Entities;
using WhatsFlow.Application.DTOs.Pessoas;

namespace WhatsFlow.Application.Interfaces;

public interface IPessoaRepository
{
    Task<IEnumerable<Pessoa>> GetAllAsync();
    Task<(IReadOnlyList<Pessoa> Items, int Total)> GetPagedAsync(PessoaPagedQuery query);
    Task<Pessoa?> GetByIdAsync(int id);
    Task<Pessoa?> GetByEmailAsync(string email);
    Task<Pessoa?> GetByWhatsAppAsync(string whatsApp);
    Task<Pessoa?> GetByTelefoneAsync(string telefone);
    Task<Pessoa> CreateAsync(Pessoa pessoa);
    Task<Pessoa> CreateWithoutSaveAsync(Pessoa pessoa); // Para uso em transações
    Task<Pessoa> UpdateAsync(Pessoa pessoa);
    Task UpdateWithoutSaveAsync(Pessoa pessoa); // Para uso em transações
    Task DeleteAsync(int id);
    Task<int> ResolveTenantIdAsync(string? tenantSlug = null);
}


