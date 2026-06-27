using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação para acessar
public class PessoasPerfisController : ControllerBase
{
    private readonly IPessoaPerfilService _service;

    public PessoasPerfisController(IPessoaPerfilService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista todos os perfis de pessoas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PessoaPerfilDto>>> GetAll()
    {
        var perfis = await _service.GetAllAsync();
        return Ok(perfis);
    }

    /// <summary>
    /// Obtém detalhe de um perfil específico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PessoaPerfilDto>> GetById(int id)
    {
        var perfil = await _service.GetByIdAsync(id);
        if (perfil == null)
            return NotFound();

        return Ok(perfil);
    }

    /// <summary>
    /// Lista todos os perfis de uma pessoa específica
    /// </summary>
    [HttpGet("pessoa/{pessoaId}")]
    public async Task<ActionResult<IEnumerable<PessoaPerfilDto>>> GetByPessoa(int pessoaId)
    {
        var perfis = await _service.GetPerfisPorPessoaAsync(pessoaId);
        return Ok(perfis);
    }

    /// <summary>
    /// Cria um novo perfil para uma pessoa
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PessoaPerfilDto>> Create(CriarPessoaPerfilDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem criar perfis de pessoa.");
        }

        try
        {
            var perfil = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = perfil.Id }, perfil);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao criar perfil", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um perfil (ex: encerrar perfil definindo DataFim)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PessoaPerfilDto>> Update(int id, AtualizarPessoaPerfilDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem atualizar perfis de pessoa.");
        }

        try
        {
            var perfil = await _service.UpdateAsync(id, dto);
            return Ok(perfil);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove um perfil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem excluir perfis de pessoa.");
        }

        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsAdminUser()
    {
        var tipoUsuarioId = User.FindFirstValue("TipoUsuarioId");
        return tipoUsuarioId == ((int)TipoUsuario.Admin).ToString() ||
               tipoUsuarioId == ((int)TipoUsuario.Ambos).ToString();
    }
}


