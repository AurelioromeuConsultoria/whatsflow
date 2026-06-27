using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.API.Controllers;

/// <summary>
/// Recebe os webhooks de entrada do WhatsApp por tenant (sem JWT). O tenant é resolvido pelo
/// slug na rota; o evento bruto é SEMPRE persistido antes de qualquer processamento, para nunca
/// perder eventos. Sempre responde 200 OK rapidamente (evita tempestade de retries do provider).
/// </summary>
[ApiController]
[AllowAnonymous]
public class WhatsAppWebhooksController : ControllerBase
{
    private readonly WhatsFlowDbContext _dbContext;
    private readonly TenantScopeOverride _tenantScope;
    private readonly IWhatsAppAccountRepository _accountRepository;
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IWhatsAppWebhookProcessingService _processingService;
    private readonly ILogger<WhatsAppWebhooksController> _logger;

    public WhatsAppWebhooksController(
        WhatsFlowDbContext dbContext,
        TenantScopeOverride tenantScope,
        IWhatsAppAccountRepository accountRepository,
        IWebhookEventRepository webhookEventRepository,
        IWhatsAppWebhookProcessingService processingService,
        ILogger<WhatsAppWebhooksController> logger)
    {
        _dbContext = dbContext;
        _tenantScope = tenantScope;
        _accountRepository = accountRepository;
        _webhookEventRepository = webhookEventRepository;
        _processingService = processingService;
        _logger = logger;
    }

    [HttpPost("/api/webhooks/whatsapp/{tenantSlug}")]
    public async Task<IActionResult> Receber(string tenantSlug, CancellationToken cancellationToken)
    {
        // Lê o corpo bruto (necessário para validação de assinatura e parsing por provider).
        string rawBody;
        Request.EnableBuffering();
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var headers = Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        // Resolve o tenant pelo slug ANTES de existir contexto de tenant — bypass do filtro.
        var tenant = await ResolverTenantPorSlugAsync(tenantSlug, cancellationToken);
        if (tenant == null)
        {
            return NotFound();
        }

        // Estabelece o escopo de tenant para o resto do request (mesmo mecanismo do worker).
        _tenantScope.SetTenant(tenant.Id, tenant.Slug);

        var conta = await _accountRepository.GetAtivaAsync();

        // SEMPRE persiste o evento bruto primeiro (Status=Recebido), para nunca perder eventos.
        var webhookEvent = new WebhookEvent
        {
            TenantId = tenant.Id,
            WhatsAppAccountId = conta?.Id,
            Provider = conta?.Provider ?? WhatsAppProviderType.Fake,
            RawPayload = string.IsNullOrEmpty(rawBody) ? "{}" : rawBody,
            Status = WebhookEventStatus.Recebido,
            CriadoEm = DateTime.UtcNow
        };
        webhookEvent = await _webhookEventRepository.CreateAsync(webhookEvent);

        if (conta == null)
        {
            _logger.LogWarning(
                "Webhook WhatsApp recebido sem conta ativa para o tenant {TenantSlug} ({TenantId}). WebhookEventId={WebhookEventId}",
                tenant.Slug, tenant.Id, webhookEvent.Id);
            // Evento ficou registrado (Recebido); responde 200 para não gerar retries.
            return Ok();
        }

        // Processa de forma defensiva (o serviço nunca lança e sempre fecha o status do evento).
        await _processingService.ProcessarAsync(webhookEvent, conta, cancellationToken);

        return Ok();
    }

    private async Task<Tenant?> ResolverTenantPorSlugAsync(string tenantSlug, CancellationToken cancellationToken)
    {
        var normalized = (tenantSlug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        // Tenant não é ITenantEntity, mas garantimos o bypass para deixar explícito que não há
        // contexto de tenant ainda neste ponto do pipeline.
        var anterior = _dbContext.IgnoreTenantFilters;
        _dbContext.IgnoreTenantFilters = true;
        try
        {
            return await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Slug == normalized && t.Ativo, cancellationToken);
        }
        finally
        {
            _dbContext.IgnoreTenantFilters = anterior;
        }
    }
}
