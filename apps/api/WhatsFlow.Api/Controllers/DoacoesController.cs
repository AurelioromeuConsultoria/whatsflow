using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoacoesController : ControllerBase
{
    private readonly IDoacoesService _service;

    public DoacoesController(IDoacoesService service)
    {
        _service = service;
    }

    [HttpGet("finalidades")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FinalidadeDoacaoDto>>> GetFinalidades()
    {
        var items = await _service.GetFinalidadesAsync();
        return Ok(items);
    }

    [HttpGet("finalidades/publicas")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FinalidadeDoacaoDto>>> GetFinalidadesPublicas()
    {
        var items = await _service.GetFinalidadesAsync(publicOnly: true);
        return Ok(items);
    }

    [HttpGet("finalidades/{id:int}")]
    [Authorize]
    public async Task<ActionResult<FinalidadeDoacaoDto>> GetFinalidadeById(int id)
    {
        var item = await _service.GetFinalidadeByIdAsync(id);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost("finalidades")]
    [Authorize]
    public async Task<ActionResult<FinalidadeDoacaoDto>> CreateFinalidade(SalvarFinalidadeDoacaoDto dto)
    {
        try
        {
            var created = await _service.CreateFinalidadeAsync(dto);
            return CreatedAtAction(nameof(GetFinalidadeById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("finalidades/{id:int}")]
    [Authorize]
    public async Task<ActionResult<FinalidadeDoacaoDto>> UpdateFinalidade(int id, SalvarFinalidadeDoacaoDto dto)
    {
        try
        {
            var updated = await _service.UpdateFinalidadeAsync(id, dto);
            return Ok(updated);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("finalidades/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteFinalidade(int id)
    {
        await _service.DeleteFinalidadeAsync(id);
        return NoContent();
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<DoacaoOnlineDto>>> GetDoacoes()
    {
        var items = await _service.GetDoacoesAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<DoacaoOnlineDto>> GetDoacaoById(int id)
    {
        var item = await _service.GetDoacaoByIdAsync(id);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpGet("status/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<DoacaoOnlineDto>> GetStatusDoacao(string token)
    {
        var item = await _service.GetDoacaoByReciboTokenAsync(token);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpGet("recibo/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<DoacaoReciboDto>> GetReciboDoacao(string token)
    {
        var recibo = await _service.GetReciboByTokenAsync(token);
        if (recibo is null) return NotFound();
        return Ok(recibo);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<DoacaoOnlineDto>> CreateDoacao(CriarDoacaoOnlineDto dto)
    {
        try
        {
            var created = await _service.CreateDoacaoAsync(dto);
            return CreatedAtAction(nameof(GetDoacaoById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("configuracao/asaas")]
    [Authorize]
    public async Task<ActionResult<GivingProviderConfigDto>> GetAsaasConfig()
    {
        var config = await _service.GetProviderConfigAsync(WhatsFlow.Domain.Entities.GivingProvider.Asaas);
        return Ok(config);
    }

    [HttpPut("configuracao/asaas")]
    [Authorize]
    public async Task<ActionResult<GivingProviderConfigDto>> SaveAsaasConfig(SalvarGivingProviderConfigDto dto)
    {
        dto.Provider = WhatsFlow.Domain.Entities.GivingProvider.Asaas;
        var config = await _service.SaveProviderConfigAsync(dto);
        return Ok(config);
    }

    [HttpPost("/api/webhooks/asaas")]
    [AllowAnonymous]
    public async Task<IActionResult> AsaasWebhook([FromBody] JsonElement payload)
    {
        var accessToken = Request.Headers["asaas-access-token"].FirstOrDefault();
        var processed = await _service.ProcessAsaasWebhookAsync(payload, accessToken);
        return processed ? Ok() : Unauthorized();
    }
}
