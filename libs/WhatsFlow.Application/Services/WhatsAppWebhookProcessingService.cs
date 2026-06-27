using Microsoft.Extensions.Logging;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services.WhatsApp;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IWhatsAppWebhookProcessingService
{
    /// <summary>
    /// Processa um WebhookEvent já persistido (Status=Recebido). Valida a assinatura,
    /// faz o parse e aplica os eventos de mensagem de forma idempotente.
    /// Nunca lança: marca o WebhookEvent como Processado/Ignorado/Falhou e registra o erro.
    /// </summary>
    Task ProcessarAsync(WebhookEvent webhookEvent, WhatsAppAccount account, CancellationToken cancellationToken = default);
}

public class WhatsAppWebhookProcessingService : IWhatsAppWebhookProcessingService
{
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IComunicacaoEntregaRepository _entregaRepository;
    private readonly IMessageLogRepository _messageLogRepository;
    private readonly IContatoRepository _contatoRepository;
    private readonly IWhatsAppProviderResolver _providerResolver;
    private readonly ILogger<WhatsAppWebhookProcessingService> _logger;

    public WhatsAppWebhookProcessingService(
        IWebhookEventRepository webhookEventRepository,
        IComunicacaoEntregaRepository entregaRepository,
        IMessageLogRepository messageLogRepository,
        IContatoRepository contatoRepository,
        IWhatsAppProviderResolver providerResolver,
        ILogger<WhatsAppWebhookProcessingService> logger)
    {
        _webhookEventRepository = webhookEventRepository;
        _entregaRepository = entregaRepository;
        _messageLogRepository = messageLogRepository;
        _contatoRepository = contatoRepository;
        _providerResolver = providerResolver;
        _logger = logger;
    }

    public async Task ProcessarAsync(WebhookEvent webhookEvent, WhatsAppAccount account, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = _providerResolver.ResolveFor(account);
            var context = new WebhookContext(
                account,
                webhookEvent.RawPayload,
                new Dictionary<string, string>());

            var valido = await provider.ValidateWebhookAsync(context, cancellationToken);
            if (!valido)
            {
                _logger.LogWarning(
                    "Webhook WhatsApp inválido (assinatura). WebhookEventId={WebhookEventId} TenantId={TenantId}",
                    webhookEvent.Id, webhookEvent.TenantId);
                webhookEvent.Status = WebhookEventStatus.Ignorado;
                webhookEvent.Erro = "Assinatura/validação do webhook falhou.";
                webhookEvent.ProcessadoEm = DateTime.UtcNow;
                await _webhookEventRepository.UpdateAsync(webhookEvent);
                return;
            }

            // TODO(WhatsFlow Etapa 4D): parsing real do payload por provider
            var eventos = await provider.ParseWebhookAsync(context, cancellationToken);

            foreach (var evento in eventos)
            {
                await AplicarEventoAsync(evento, cancellationToken);
            }

            webhookEvent.Status = WebhookEventStatus.Processado;
            webhookEvent.ProcessadoEm = DateTime.UtcNow;
            await _webhookEventRepository.UpdateAsync(webhookEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao processar webhook WhatsApp. WebhookEventId={WebhookEventId} TenantId={TenantId}",
                webhookEvent.Id, webhookEvent.TenantId);
            webhookEvent.Status = WebhookEventStatus.Falhou;
            webhookEvent.Erro = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            webhookEvent.ProcessadoEm = DateTime.UtcNow;
            try
            {
                await _webhookEventRepository.UpdateAsync(webhookEvent);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx,
                    "Falha ao registrar o erro do webhook WhatsApp. WebhookEventId={WebhookEventId}",
                    webhookEvent.Id);
            }
        }
    }

    private async Task AplicarEventoAsync(WebhookMessageEvent evento, CancellationToken cancellationToken)
    {
        if (evento.Type == MessageEventType.OptOut)
        {
            await AplicarOptOutAsync(evento);
            return;
        }

        if (string.IsNullOrWhiteSpace(evento.ProviderMessageId))
        {
            // Sem id de mensagem não há como casar com uma entrega (idempotência).
            return;
        }

        // Idempotente: casa a entrega pelo ProviderMessageId dentro do tenant atual.
        var entrega = await _entregaRepository.GetByProviderMessageIdAsync(evento.ProviderMessageId);
        if (entrega == null)
        {
            return;
        }

        var (novoStatus, aplica) = MapearStatus(evento.Type);
        if (!aplica)
        {
            return;
        }

        entrega.Status = novoStatus;
        entrega.AtualizadoEm = DateTime.UtcNow;

        switch (evento.Type)
        {
            case MessageEventType.Delivered:
                entrega.EntregueEm = evento.OccurredAt;
                break;
            case MessageEventType.Read:
                entrega.LidoEm = evento.OccurredAt;
                break;
            case MessageEventType.Failed:
                entrega.ErrorCode = Truncar(evento.Detail, 60);
                entrega.Erro = Truncar(evento.Detail, 1000);
                break;
        }

        await _entregaRepository.UpdateAsync(entrega);

        await _messageLogRepository.CreateAsync(new MessageLog
        {
            TenantId = entrega.TenantId,
            ComunicacaoEntregaId = entrega.Id,
            Status = novoStatus,
            ProviderMessageId = evento.ProviderMessageId,
            ErrorCode = evento.Type == MessageEventType.Failed ? Truncar(evento.Detail, 60) : null,
            Detalhe = Truncar(evento.Detail, 1000),
            CriadoEm = DateTime.UtcNow
        });
    }

    private async Task AplicarOptOutAsync(WebhookMessageEvent evento)
    {
        if (string.IsNullOrWhiteSpace(evento.FromPhoneE164))
        {
            return;
        }

        var contato = await _contatoRepository.GetByTelefoneWhatsAppAsync(evento.FromPhoneE164);
        if (contato == null || !contato.OptIn)
        {
            return;
        }

        contato.OptIn = false;
        contato.DataOptOut = DateTime.UtcNow;
        contato.AtualizadoEm = DateTime.UtcNow;
        await _contatoRepository.UpdateAsync(contato);

        _logger.LogInformation(
            "Opt-out registrado via webhook. ContatoId={ContatoId} TenantId={TenantId}",
            contato.Id, contato.TenantId);
    }

    private static (StatusComunicacaoEntrega Status, bool Aplica) MapearStatus(MessageEventType type)
        => type switch
        {
            MessageEventType.Sent => (StatusComunicacaoEntrega.Enviado, true),
            MessageEventType.Delivered => (StatusComunicacaoEntrega.Entregue, true),
            MessageEventType.Read => (StatusComunicacaoEntrega.Lido, true),
            MessageEventType.Failed => (StatusComunicacaoEntrega.Falhou, true),
            _ => (StatusComunicacaoEntrega.Pendente, false)
        };

    private static string? Truncar(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= max ? value : value[..max];
    }
}
