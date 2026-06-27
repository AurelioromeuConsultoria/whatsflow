using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComunicacaoAutomacoesController : ControllerBase
{
    private readonly IComunicacaoAutomacaoService _service;

    public ComunicacaoAutomacoesController(IComunicacaoAutomacaoService service)
    {
        _service = service;
    }

    [HttpGet("historico")]
    public async Task<ActionResult<PagedResultDto<ComunicacaoAutomacaoHistoricoItemDto>>> GetHistorico([FromQuery] ComunicacaoAutomacaoHistoricoQueryDto query)
    {
        return Ok(await _service.GetHistoricoAsync(query));
    }
}
