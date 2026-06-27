using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VoluntariosController : ControllerBase
{
    private readonly IVoluntarioService _service;

    public VoluntariosController(IVoluntarioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VoluntarioDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("pessoa/{pessoaId}")]
    public async Task<ActionResult<IEnumerable<VoluntarioDto>>> GetByPessoa(int pessoaId)
    {
        var items = await _service.GetVoluntariosPorPessoaAsync(pessoaId);
        return Ok(items);
    }

    [HttpGet("equipe/{equipeId}")]
    public async Task<ActionResult<IEnumerable<VoluntarioDto>>> GetByEquipe(int equipeId)
    {
        var items = await _service.GetVoluntariosPorEquipeAsync(equipeId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VoluntarioDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<VoluntarioDto>> Create(CriarVoluntarioDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem criar voluntários.");
        }

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
    public async Task<ActionResult<VoluntarioDto>> Update(int id, AtualizarVoluntarioDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem atualizar voluntários.");
        }

        try
        {
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
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem excluir voluntários.");
        }

        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private bool IsAdminUser()
    {
        var tipoUsuarioId = User.FindFirstValue("TipoUsuarioId");
        return tipoUsuarioId == ((int)TipoUsuario.Admin).ToString() ||
               tipoUsuarioId == ((int)TipoUsuario.Ambos).ToString();
    }
}
