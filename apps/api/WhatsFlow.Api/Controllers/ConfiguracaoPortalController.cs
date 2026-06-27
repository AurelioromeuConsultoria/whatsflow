using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracaoPortalController : ControllerBase
{
    private readonly IConfiguracaoPortalService _service;

    public ConfiguracaoPortalController(IConfiguracaoPortalService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous] // Permitir acesso público para o Portal
    public async Task<ActionResult<ConfiguracaoPortalDto>> Get()
    {
        var config = await _service.GetAsync();
        return Ok(config);
    }

    [HttpPut]
    [Authorize] // Apenas admin pode atualizar
    public async Task<ActionResult<ConfiguracaoPortalDto>> Update([FromBody] AtualizarConfiguracaoPortalDto dto)
    {
        if (dto.TempoTransicaoCarrossel < 1 || dto.TempoTransicaoCarrossel > 60)
        {
            return BadRequest(new { message = "Tempo de transição deve estar entre 1 e 60 segundos" });
        }

        var updated = await _service.UpdateAsync(dto);
        return Ok(updated);
    }
}
