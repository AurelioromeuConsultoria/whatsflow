using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

/// <summary>
/// Billing de assinatura do tenant corrente. Isento do gating de assinatura
/// (precisa ser acessível mesmo com assinatura suspensa, para regularizar).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _service;
    private readonly ICurrentUserContext _currentUser;

    public BillingController(IBillingService service, ICurrentUserContext currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    private int TenantAtual => _currentUser.TenantId ?? Tenant.InitialTenantId;

    [HttpGet("planos")]
    public async Task<ActionResult<IEnumerable<PlanoDto>>> Planos()
        => Ok(await _service.ListarPlanosAsync());

    [HttpGet("minha-assinatura")]
    public async Task<ActionResult<AssinaturaDto>> MinhaAssinatura()
    {
        var assinatura = await _service.ObterPorTenantAsync(TenantAtual);
        return assinatura == null ? NotFound() : Ok(assinatura);
    }

    [HttpGet("faturas")]
    public async Task<ActionResult<IEnumerable<FaturaDto>>> Faturas()
        => Ok(await _service.ListarFaturasAsync(TenantAtual));

    [HttpPost("assinar")]
    public async Task<ActionResult<AssinaturaDto>> Assinar([FromBody] AssinarTenantDto dto)
    {
        try
        {
            dto.TenantId = TenantAtual; // o tenant só pode assinar para si mesmo
            var assinatura = await _service.AssinarAsync(dto);
            return Ok(assinatura);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("cancelar")]
    public async Task<ActionResult<AssinaturaDto>> Cancelar()
    {
        var assinatura = await _service.CancelarAsync(TenantAtual);
        return assinatura == null ? NotFound() : Ok(assinatura);
    }
}
