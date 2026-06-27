using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IndisponibilidadesVoluntariosController : ControllerBase
{
    private readonly IIndisponibilidadeVoluntarioService _service;

    public IndisponibilidadesVoluntariosController(IIndisponibilidadeVoluntarioService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IndisponibilidadeVoluntarioDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("voluntario/{voluntarioId}")]
    public async Task<ActionResult<IEnumerable<IndisponibilidadeVoluntarioDto>>> GetByVoluntario(
        int voluntarioId,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        var items = await _service.GetByVoluntarioAsync(voluntarioId, dataInicio, dataFim);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<IndisponibilidadeVoluntarioDto>> Create(CriarIndisponibilidadeVoluntarioDto dto)
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
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
}
