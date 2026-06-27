using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnquetesController : ControllerBase
{
    private readonly IEnqueteService _service;

    public EnquetesController(IEnqueteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnqueteDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("ativas")]
    public async Task<ActionResult<IEnumerable<EnqueteDto>>> GetAtivas()
    {
        var items = await _service.GetAtivasAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EnqueteDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EnqueteDto>> Create(CriarEnqueteDto dto)
    {
        try
        {
            if (dto.Opcoes == null || !dto.Opcoes.Any())
            {
                return BadRequest("A enquete deve ter pelo menos uma opção");
            }

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EnqueteDto>> Update(int id, AtualizarEnqueteDto dto)
    {
        try
        {
            if (dto.Opcoes == null || !dto.Opcoes.Any())
            {
                return BadRequest("A enquete deve ter pelo menos uma opção");
            }

            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (ArgumentException)
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
}
