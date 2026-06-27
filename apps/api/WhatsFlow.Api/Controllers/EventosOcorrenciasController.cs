using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosOcorrenciasController : ControllerBase
{
    private readonly IEventoOcorrenciaService _service;

    public EventosOcorrenciasController(IEventoOcorrenciaService service)
    {
        _service = service;
    }

    [HttpGet("evento/{eventoId}")]
    public async Task<ActionResult<IEnumerable<EventoOcorrenciaDto>>> GetByEvento(int eventoId)
    {
        var items = await _service.GetByEventoAsync(eventoId);
        return Ok(items);
    }

    [HttpGet("periodo")]
    public async Task<ActionResult<IEnumerable<EventoOcorrenciaDto>>> GetByPeriodo(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim,
        [FromQuery] int? eventoId = null)
    {
        var items = await _service.GetByPeriodoAsync(dataInicio, dataFim, eventoId);
        return Ok(items);
    }

    [HttpGet("periodo/cobertura-voluntariado")]
    public async Task<ActionResult<IEnumerable<CoberturaVoluntariadoOcorrenciaDto>>> GetCoberturaVoluntariado(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim,
        [FromQuery] int? eventoId = null,
        [FromQuery] string? nivelRisco = null)
    {
        var items = await _service.GetCoberturaVoluntariadoAsync(dataInicio, dataFim, eventoId, nivelRisco);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventoOcorrenciaDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EventoOcorrenciaDto>> Create(CriarEventoOcorrenciaDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EventoOcorrenciaDto>> Update(int id, AtualizarEventoOcorrenciaDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("não encontrada"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("gerar-recorrencia")]
    public async Task<ActionResult<object>> GerarPorRecorrencia(
        [FromQuery] int eventoId,
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim)
    {
        try
        {
            var total = await _service.GerarPorRecorrenciaAsync(eventoId, dataInicio, dataFim);
            return Ok(new { totalCriadas = total });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
