using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/perfis-acesso")]
[Authorize]
public class PerfisAcessoController : ControllerBase
{
    private readonly IPerfilAcessoService _service;

    public PerfisAcessoController(IPerfilAcessoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PerfilAcessoDto>>> GetAll()
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem listar perfis de acesso.");
        }

        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PerfilAcessoDto>> GetById(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem visualizar perfis de acesso.");
        }

        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<PerfilAcessoDto>> Create(CriarPerfilAcessoDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem criar perfis de acesso.");
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
    public async Task<ActionResult<PerfilAcessoDto>> Update(int id, AtualizarPerfilAcessoDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem atualizar perfis de acesso.");
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
            return StatusCode(403, "Apenas administradores podem excluir perfis de acesso.");
        }

        await _service.DeleteAsync(id);
        return NoContent();
    }

    private bool IsAdminUser()
    {
        var tipoUsuarioId = User.FindFirstValue("TipoUsuarioId");
        return tipoUsuarioId == ((int)TipoUsuario.Admin).ToString() ||
               tipoUsuarioId == ((int)TipoUsuario.Ambos).ToString();
    }
}
