using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IKidsOcorrenciaRepository
{
    Task<KidsOcorrencia?> GetByIdAsync(int id);
    Task<IEnumerable<KidsOcorrencia>> GetByCriancaIdAsync(int criancaPessoaId);
    Task<IEnumerable<KidsOcorrencia>> GetAbertasAsync();
    Task<KidsOcorrencia> CreateAsync(KidsOcorrencia ocorrencia);
    Task<KidsOcorrencia> UpdateAsync(KidsOcorrencia ocorrencia);
}
