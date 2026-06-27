using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IResponsavelCriancaRepository
{
    Task<IEnumerable<ResponsavelCrianca>> GetByCriancaIdAsync(int criancaPessoaId);
    Task<IEnumerable<ResponsavelCrianca>> GetByResponsavelIdAsync(int responsavelPessoaId);
    Task<IEnumerable<int>> GetResponsavelIdsAtivosAsync();
    Task<IEnumerable<int>> GetResponsavelIdsAtivosByCriancaIdsAsync(IEnumerable<int> criancaPessoaIds);
    Task<IEnumerable<int>> GetCriancaIdsAtivosByResponsavelIdAsync(int responsavelPessoaId);
    Task<ResponsavelCrianca?> GetByIdAsync(int id);
    Task<ResponsavelCrianca?> GetByCriancaAndResponsavelAsync(int criancaPessoaId, int responsavelPessoaId);
    Task<bool> ExisteVinculoAtivoAsync(int criancaPessoaId, int responsavelPessoaId);
    Task<ResponsavelCrianca> CreateAsync(ResponsavelCrianca responsavel);
    Task<ResponsavelCrianca> CreateWithoutSaveAsync(ResponsavelCrianca responsavel);
    Task<ResponsavelCrianca> UpdateAsync(ResponsavelCrianca responsavel);
    Task DeleteAsync(int id);
    Task<bool> PodeRetirarAsync(int criancaPessoaId, int responsavelPessoaId);
}
