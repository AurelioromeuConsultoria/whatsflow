using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Repositório do log bruto de webhooks. Escrito pelo pipeline de webhook (Etapa 4C);
/// sem controller público.
/// </summary>
public interface IWebhookEventRepository
{
    Task<WebhookEvent?> GetByIdAsync(int id);
    Task<WebhookEvent?> GetByProviderMessageIdAsync(WhatsAppProviderType provider, string providerMessageId);
    Task<WebhookEvent> CreateAsync(WebhookEvent webhookEvent);
    Task<WebhookEvent> UpdateAsync(WebhookEvent webhookEvent);
    Task<IReadOnlyList<WebhookEvent>> GetPendentesAsync(int limit);
}
