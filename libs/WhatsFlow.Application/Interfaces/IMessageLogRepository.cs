using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Repositório do histórico append-only de transições de status de entregas.
/// Escrito pelo worker/webhook (Etapa 4C); sem controller público.
/// </summary>
public interface IMessageLogRepository
{
    Task<MessageLog> CreateAsync(MessageLog log);
    Task<IReadOnlyList<MessageLog>> GetByEntregaIdAsync(int comunicacaoEntregaId);
}
