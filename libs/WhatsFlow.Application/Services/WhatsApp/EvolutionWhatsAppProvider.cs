using Microsoft.Extensions.Logging;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services.WhatsApp;

/// <summary>
/// Implementação de IWhatsAppProvider sobre a Evolution API (já existente).
/// Embrulha IEvolutionApiService. A configuração efetiva (BaseUrl/ApiKey/Instance) hoje vem
/// das settings globais; a migração para ler de WhatsAppAccount.ConfiguracoesJson é incremental.
/// </summary>
public sealed class EvolutionWhatsAppProvider : IWhatsAppProvider
{
    private readonly IEvolutionApiService _evolution;
    private readonly ILogger<EvolutionWhatsAppProvider> _logger;

    public EvolutionWhatsAppProvider(IEvolutionApiService evolution, ILogger<EvolutionWhatsAppProvider> logger)
    {
        _evolution = evolution;
        _logger = logger;
    }

    public WhatsAppProviderType Type => WhatsAppProviderType.EvolutionApi;

    public async Task<ProviderSendResult> SendTextMessageAsync(
        WhatsAppAccount account, string toPhoneE164, string body, CancellationToken cancellationToken = default)
    {
        var resp = await _evolution.EnviarMensagemTextoAsync(toPhoneE164, body, cancellationToken);
        return resp.Sucesso
            ? ProviderSendResult.Ok(resp.MessageId)
            : ProviderSendResult.Fail(resp.StatusCode.ToString(), resp.MensagemErro);
    }

    public async Task<ProviderSendResult> SendTemplateMessageAsync(
        WhatsAppAccount account, string toPhoneE164, string providerTemplateRef,
        IReadOnlyDictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        // Evolution não tem fluxo de "template aprovado" como a Cloud API: o corpo já vem renderizado
        // pelo chamador. Aqui tratamos o providerTemplateRef como o texto final quando aplicável.
        var resp = await _evolution.EnviarMensagemTextoAsync(toPhoneE164, providerTemplateRef, cancellationToken);
        return resp.Sucesso
            ? ProviderSendResult.Ok(resp.MessageId)
            : ProviderSendResult.Fail(resp.StatusCode.ToString(), resp.MensagemErro);
    }

    public Task<ProviderMessageStatus?> GetMessageStatusAsync(
        WhatsAppAccount account, string providerMessageId, CancellationToken cancellationToken = default)
        => Task.FromResult<ProviderMessageStatus?>(null); // status vem via webhook

    public Task<bool> ValidateWebhookAsync(WebhookContext context, CancellationToken cancellationToken = default)
    {
        // Evolution normalmente valida por apikey/header. Se houver WebhookSecret configurado, compara.
        if (string.IsNullOrWhiteSpace(context.Account.WebhookSecret))
            return Task.FromResult(true);

        var ok = context.Headers.TryGetValue("apikey", out var apikey)
                 && apikey == context.Account.WebhookSecret;
        return Task.FromResult(ok);
    }

    public Task<IReadOnlyList<WebhookMessageEvent>> ParseWebhookAsync(
        WebhookContext context, CancellationToken cancellationToken = default)
    {
        // TODO(WhatsFlow Etapa 4C/4D): mapear payload real da Evolution (messages.upsert / status)
        // para WebhookMessageEvent. Por ora retorna vazio (não quebra o pipeline).
        _logger.LogDebug("[Evolution] webhook recebido ({Bytes} bytes) — parsing detalhado pendente.",
            context.RawBody?.Length ?? 0);
        return Task.FromResult<IReadOnlyList<WebhookMessageEvent>>(Array.Empty<WebhookMessageEvent>());
    }
}
