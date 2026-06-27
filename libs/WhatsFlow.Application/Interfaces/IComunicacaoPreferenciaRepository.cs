using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IComunicacaoPreferenciaRepository
{
    Task<IReadOnlyList<ComunicacaoPreferencia>> GetByContatoIdAsync(int contatoId);
    Task<ComunicacaoPreferencia?> GetByContatoCanalAsync(int contatoId, CanalComunicacao canal);
    Task<ComunicacaoPreferencia> CreateAsync(ComunicacaoPreferencia preferencia);
    Task<ComunicacaoPreferencia> UpdateAsync(ComunicacaoPreferencia preferencia);
}
