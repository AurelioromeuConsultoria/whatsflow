using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services.WhatsApp;

/// <summary>
/// Abstração de um provedor de WhatsApp. A regra de negócio (campanha, fila) nunca referencia
/// uma implementação concreta — sempre passa por esta interface (ADR-03).
/// Implementações: Fake (dev/testes), Evolution (atual), Cloud API oficial (futuro), etc.
/// </summary>
public interface IWhatsAppProvider
{
    WhatsAppProviderType Type { get; }

    Task<ProviderSendResult> SendTextMessageAsync(
        WhatsAppAccount account,
        string toPhoneE164,
        string body,
        CancellationToken cancellationToken = default);

    Task<ProviderSendResult> SendTemplateMessageAsync(
        WhatsAppAccount account,
        string toPhoneE164,
        string providerTemplateRef,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default);

    /// <summary>Opcional — nem todo provider expõe consulta de status (pode retornar null).</summary>
    Task<ProviderMessageStatus?> GetMessageStatusAsync(
        WhatsAppAccount account,
        string providerMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>Valida a autenticidade do webhook (assinatura/segredo).</summary>
    Task<bool> ValidateWebhookAsync(
        WebhookContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Converte o payload bruto do webhook em eventos de mensagem normalizados.</summary>
    Task<IReadOnlyList<WebhookMessageEvent>> ParseWebhookAsync(
        WebhookContext context,
        CancellationToken cancellationToken = default);
}

public sealed record ProviderSendResult(
    bool Success,
    string? ProviderMessageId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ProviderSendResult Ok(string? providerMessageId) =>
        new(true, providerMessageId, null, null);

    public static ProviderSendResult Fail(string? errorCode, string? errorMessage) =>
        new(false, null, errorCode, errorMessage);
}

public sealed record ProviderMessageStatus(
    string ProviderMessageId,
    MessageEventType Status,
    DateTime? OccurredAt);

/// <summary>Contexto bruto de uma requisição de webhook, agnóstico de framework web.</summary>
public sealed record WebhookContext(
    WhatsAppAccount Account,
    string RawBody,
    IReadOnlyDictionary<string, string> Headers,
    string? QuerySignature = null);

public sealed record WebhookMessageEvent(
    string? ProviderMessageId,
    MessageEventType Type,
    DateTime OccurredAt,
    string? FromPhoneE164 = null,
    string? Detail = null);

public enum MessageEventType
{
    Sent = 1,
    Delivered = 2,
    Read = 3,
    Failed = 4,
    Inbound = 5,
    OptOut = 6,
    Unknown = 99
}

/// <summary>Seleciona a implementação de IWhatsAppProvider conforme o provider da conta.</summary>
public interface IWhatsAppProviderResolver
{
    IWhatsAppProvider Resolve(WhatsAppProviderType type);
    IWhatsAppProvider ResolveFor(WhatsAppAccount account);
}
