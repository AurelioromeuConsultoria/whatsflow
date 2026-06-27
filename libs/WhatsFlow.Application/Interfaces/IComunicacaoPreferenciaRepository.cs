using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IComunicacaoPreferenciaRepository
{
    Task<IReadOnlyList<ComunicacaoPreferencia>> GetByPessoaIdAsync(int pessoaId);
    Task<ComunicacaoPreferencia?> GetByPessoaCanalAsync(int pessoaId, CanalComunicacao canal);
    Task<ComunicacaoPreferencia> CreateAsync(ComunicacaoPreferencia preferencia);
    Task<ComunicacaoPreferencia> UpdateAsync(ComunicacaoPreferencia preferencia);
}
