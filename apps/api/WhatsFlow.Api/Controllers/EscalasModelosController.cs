using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EscalasModelosController : ControllerBase
{
    private readonly IEscalaModeloService _service;

    public EscalasModelosController(IEscalaModeloService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EscalaModeloDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("equipe/{equipeId}")]
    public async Task<ActionResult<IEnumerable<EscalaModeloDto>>> GetByEquipe(int equipeId)
    {
        var items = await _service.GetByEquipeAsync(equipeId);
        return Ok(items);
    }

    [HttpGet("evento/{eventoId}")]
    public async Task<ActionResult<IEnumerable<EscalaModeloDto>>> GetByEvento(int eventoId)
    {
        var items = await _service.GetByEventoAsync(eventoId);
        return Ok(items);
    }

    [HttpGet("evento-equipe")]
    public async Task<ActionResult<EscalaModeloDto>> GetByEventoAndEquipe([FromQuery] int? eventoId, [FromQuery] int equipeId)
    {
        var item = await _service.GetByEventoAndEquipeAsync(eventoId, equipeId);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EscalaModeloDto>> Create(CriarEscalaModeloDto dto)
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
    public async Task<ActionResult<EscalaModeloDto>> Update(int id, AtualizarEscalaModeloDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("não encontrado"))
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
        catch (ArgumentException ex) when (ex.Message.Contains("não encontrado"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
