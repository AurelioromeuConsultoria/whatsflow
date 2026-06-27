using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IPessoaPerfilRepository
{
    Task<IEnumerable<PessoaPerfil>> GetAllAsync();
    Task<PessoaPerfil?> GetByIdAsync(int id);
    Task<PessoaPerfil?> GetPerfilAtivoAsync(int pessoaId, PerfilPessoa perfil);
    Task<PessoaPerfil> CreateAsync(PessoaPerfil pessoaPerfil);
    Task<PessoaPerfil> CreateWithoutSaveAsync(PessoaPerfil pessoaPerfil); // Para uso em transações
    Task<PessoaPerfil> UpdateAsync(PessoaPerfil pessoaPerfil);
    Task DeleteAsync(int id);
    Task<IEnumerable<PessoaPerfil>> GetPerfisPorPessoaAsync(int pessoaId);
}



