using Microsoft.Extensions.Logging;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services.WhatsApp;

/// <summary>
/// Provider de WhatsApp simulado para desenvolvimento/testes locais.
/// Não chama rede: registra a "mensagem" no log e devolve um ProviderMessageId fake de sucesso.
/// </summary>
public sealed class FakeWhatsAppProvider : IWhatsAppProvider
{
    private readonly ILogger<FakeWhatsAppProvider> _logger;

    public FakeWhatsAppProvider(ILogger<FakeWhatsAppProvider> logger)
    {
        _logger = logger;
    }

    public WhatsAppProviderType Type => WhatsAppProviderType.Fake;

    public Task<ProviderSendResult> SendTextMessageAsync(
        WhatsAppAccount account, string toPhoneE164, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[FakeWhatsApp] tenant={TenantId} conta={Conta} -> {Destino}: {Corpo}",
            account.TenantId, account.Nome, toPhoneE164, Truncar(body));
        return Task.FromResult(ProviderSendResult.Ok($"fake-{Guid.NewGuid():N}"));
    }

    public Task<ProviderSendResult> SendTemplateMessageAsync(
        WhatsAppAccount account, string toPhoneE164, string providerTemplateRef,
        IReadOnlyDictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[FakeWhatsApp] tenant={TenantId} template={Template} -> {Destino} ({NVars} variáveis)",
            account.TenantId, providerTemplateRef, toPhoneE164, variables.Count);
        return Task.FromResult(ProviderSendResult.Ok($"fake-{Guid.NewGuid():N}"));
    }

    public Task<ProviderMessageStatus?> GetMessageStatusAsync(
        WhatsAppAccount account, string providerMessageId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ProviderMessageStatus?>(
            new ProviderMessageStatus(providerMessageId, MessageEventType.Delivered, DateTime.UtcNow));
    }

    public Task<bool> ValidateWebhookAsync(WebhookContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<WebhookMessageEvent>> ParseWebhookAsync(
        WebhookContext context, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WebhookMessageEvent>>(Array.Empty<WebhookMessageEvent>());

    private static string Truncar(string s) => s.Length <= 80 ? s : s[..80] + "…";
}
