namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Envia notificações push (FCM) para dispositivos dos responsáveis.
/// </summary>
public interface IKidsPushNotificationService
{
    /// <summary>
    /// Envia a mesma notificação para todos os dispositivos dos responsáveis (por PessoaId).
    /// Ignora pessoas sem token registrado. Falhas de envio não lançam exceção (log apenas).
    /// </summary>
    Task SendToPessoasAsync(
        IEnumerable<int> pessoaIds,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null);
}
