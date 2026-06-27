using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.API.Controllers;

/// <summary>
/// Recebe os webhooks de billing do Asaas (conta da plataforma). Separado do webhook
/// de doações (/api/webhooks/asaas), que é por-tenant.
/// </summary>
[ApiController]
[AllowAnonymous]
public class WebhooksBillingController : ControllerBase
{
    private readonly IBillingService _service;

    public WebhooksBillingController(IBillingService service)
    {
        _service = service;
    }

    [HttpPost("/api/webhooks/billing/asaas")]
    public async Task<IActionResult> AsaasBilling([FromBody] JsonElement payload)
    {
        var accessToken = Request.Headers["asaas-access-token"].FirstOrDefault();
        var processado = await _service.ProcessarWebhookAsync(payload, accessToken);
        return processado ? Ok() : Unauthorized();
    }
}
