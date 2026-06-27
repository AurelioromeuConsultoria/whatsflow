using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/Eventos/{eventoId:int}/recorrencias")]
public class EventosRecorrenciasController : ControllerBase
{
    private readonly IEventoRecorrenciaService _service;

    public EventosRecorrenciasController(IEventoRecorrenciaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventoRecorrenciaDto>>> GetByEvento(int eventoId)
    {
        try
        {
            var items = await _service.GetByEventoAsync(eventoId);
            return Ok(items);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("não encontrado"))
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:int}", Name = nameof(GetRecorrenciaById))]
    public async Task<ActionResult<EventoRecorrenciaDto>> GetRecorrenciaById(int eventoId, int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (item.EventoId != eventoId) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EventoRecorrenciaDto>> Create(int eventoId, [FromBody] CriarEventoRecorrenciaDto dto)
    {
        if (dto.EventoId != eventoId)
            return BadRequest("EventoId do corpo deve coincidir com o da URL");
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtRoute(nameof(GetRecorrenciaById), new { eventoId, id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EventoRecorrenciaDto>> Update(int eventoId, int id, [FromBody] AtualizarEventoRecorrenciaDto dto)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.EventoId != eventoId) return NotFound();
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int eventoId, int id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.EventoId != eventoId) return NotFound();
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
