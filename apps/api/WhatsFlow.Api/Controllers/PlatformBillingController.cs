using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.API.Controllers;

/// <summary>
/// Gestão de billing pelo admin de plataforma (VerboPlus): visão de todas as
/// assinaturas, suspensão/reativação manual e disparo do ciclo. Restrito a platform admin.
/// </summary>
[ApiController]
[Route("api/platform-billing")]
[Authorize]
public class PlatformBillingController : ControllerBase
{
    private readonly IBillingService _billing;
    private readonly IBillingCycleService _cycle;

    public PlatformBillingController(IBillingService billing, IBillingCycleService cycle)
    {
        _billing = billing;
        _cycle = cycle;
    }

    private bool IsPlatformAdmin =>
        string.Equals(User.FindFirstValue("IsPlatformAdmin"), "true", StringComparison.OrdinalIgnoreCase);

    [HttpGet("assinaturas")]
    public async Task<ActionResult<IEnumerable<AssinaturaDto>>> Assinaturas()
    {
        if (!IsPlatformAdmin) return Forbid();
        return Ok(await _billing.ListarTodasAsync());
    }

    [HttpPut("{tenantId}/suspender")]
    public async Task<ActionResult<AssinaturaDto>> Suspender(int tenantId)
    {
        if (!IsPlatformAdmin) return Forbid();
        var result = await _billing.SuspenderAsync(tenantId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("{tenantId}/reativar")]
    public async Task<ActionResult<AssinaturaDto>> Reativar(int tenantId)
    {
        if (!IsPlatformAdmin) return Forbid();
        var result = await _billing.ReativarAsync(tenantId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("processar-ciclo")]
    public async Task<ActionResult<CicloBillingResultado>> ProcessarCiclo()
    {
        if (!IsPlatformAdmin) return Forbid();
        return Ok(await _cycle.ExecutarTransicoesAutomaticasAsync());
    }
}
