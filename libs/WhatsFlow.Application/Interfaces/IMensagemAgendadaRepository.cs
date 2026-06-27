using WhatsFlow.Domain.Entities;
using WhatsFlow.Application.DTOs.MensagensAgendadas;

namespace WhatsFlow.Application.Interfaces;

public interface IMensagemAgendadaRepository
{
    Task<IEnumerable<MensagemAgendada>> GetAllAsync();
    Task<(IReadOnlyList<MensagemAgendada> Items, int Total)> GetPagedAsync(MensagemAgendadaPagedQuery query);
    Task<MensagemAgendadaStatsDto> GetStatsAsync();
    Task<MensagemAgendada?> GetByIdAsync(int id);
    Task<MensagemAgendada> CreateAsync(MensagemAgendada mensagem);
    Task<MensagemAgendada> UpdateAsync(MensagemAgendada mensagem);
    Task DeleteAsync(int id);
    Task<IEnumerable<MensagemAgendada>> GetMensagensProntasParaEnvioAsync();
    /// <summary>Reserva transacionalmente mensagens Agendada com DataEnvio &lt;= agora (status → EmProcessamento). Evita dupla execução.</summary>
    Task<IEnumerable<MensagemAgendada>> ReservarProntasParaEnvioAsync(int limit);
    Task<IEnumerable<MensagemAgendada>> GetMensagensPorContatoAsync(int contatoId);
    Task<IEnumerable<MensagemAgendada>> GetMensagensPorStatusAsync(StatusMensagem status);

    /// <summary>
    /// Cancela mensagens do contato que ainda não foram enviadas (Status != Enviada).
    /// Retorna a quantidade afetada.
    /// </summary>
    Task<int> CancelarPendentesPorContatoAsync(int contatoId, string motivo);
}

