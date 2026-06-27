using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComunicacaoEntregasController : ControllerBase
{
    private readonly IComunicacaoEntregaService _service;
    private readonly IComunicacaoProcessamentoService _processamentoService;

    public ComunicacaoEntregasController(IComunicacaoEntregaService service, IComunicacaoProcessamentoService processamentoService)
    {
        _service = service;
        _processamentoService = processamentoService;
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDto<ComunicacaoEntregaResumoDto>>> GetPaged([FromQuery] ComunicacaoEntregaPagedQueryDto query)
    {
        return Ok(await _service.GetPagedAsync(query));
    }

    [HttpPost("processar")]
    public async Task<ActionResult<object>> ProcessarPendentes([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var processadas = await _processamentoService.ProcessarPendentesAsync(limit, cancellationToken);
        return Ok(new { processadas });
    }

    [HttpPost("reprocessar/{id:int}")]
    public async Task<ActionResult<ComunicacaoEntregaResumoDto>> Reprocessar(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _service.PrepararReprocessamentoAsync(id);
            await _processamentoService.ProcessarEntregaAsync(id, cancellationToken);
            var item = await _service.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
