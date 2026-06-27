using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;
    private readonly IUsuarioRepository _repository;

    public UsuariosController(IUsuarioService service, IUsuarioRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpGet]
    [Authorize] // Requer autenticação
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetAll()
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem listar usuários.");
        }

        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    [Authorize] // Requer autenticação
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem visualizar usuários.");
        }

        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    // Endpoint público apenas se não existir nenhum usuário no banco
    // Caso contrário, requer autenticação
    public async Task<ActionResult<UsuarioDto>> Create(CriarUsuarioDto dto)
    {
        try
        {
            // Verificar se já existe algum usuário
            var existeUsuario = await _repository.ExisteAlgumUsuarioAsync(dto.TenantSlug);
            
            // Se já existir usuário, requer autenticação
            if (existeUsuario)
            {
                // Verificar se o usuário está autenticado
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return Unauthorized("É necessário estar autenticado para criar novos usuários.");
                }

                if (!IsAdminUser())
                {
                    return StatusCode(403, "Apenas administradores podem criar novos usuários.");
                }
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
    [Authorize] // Requer autenticação
    public async Task<ActionResult<UsuarioDto>> Update(int id, AtualizarUsuarioDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem atualizar usuários.");
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
    [Authorize] // Requer autenticação
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem excluir usuários.");
        }

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

    private bool IsAdminUser()
    {
        var tipoUsuarioId = User.FindFirstValue("TipoUsuarioId");
        return tipoUsuarioId == ((int)TipoUsuario.Admin).ToString() ||
               tipoUsuarioId == ((int)TipoUsuario.Ambos).ToString();
    }
}
