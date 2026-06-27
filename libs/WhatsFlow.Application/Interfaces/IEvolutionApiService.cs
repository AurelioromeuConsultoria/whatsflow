using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Interface para integração com Evolution API
/// </summary>
public interface IEvolutionApiService
{
    /// <summary>
    /// Envia uma mensagem de texto via Evolution API
    /// </summary>
    /// <param name="numero">Número do destinatário (será formatado automaticamente)</param>
    /// <param name="mensagem">Texto da mensagem</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da API com status do envio</returns>
    Task<EvolutionApiResponse> EnviarMensagemTextoAsync(
        string numero, 
        string mensagem, 
        CancellationToken cancellationToken = default);

    Task<EvolutionApiResponse> EnviarMensagemImagemAsync(
        string numero,
        string imageUrl,
        string legenda,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a instância está conectada e funcionando
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se a instância está conectada, False caso contrário</returns>
    Task<bool> ValidarInstanciaAsync(CancellationToken cancellationToken = default);
}
