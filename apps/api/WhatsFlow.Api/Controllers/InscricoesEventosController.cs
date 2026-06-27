using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InscricoesEventosController : ControllerBase
{
    private readonly IInscricaoEventoService _service;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<InscricoesEventosController> _logger;

    public InscricoesEventosController(IInscricaoEventoService service, ICurrentUserContext currentUser, ILogger<InscricoesEventosController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InscricaoEventoDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InscricaoEventoDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("evento/{eventoId}")]
    public async Task<ActionResult<IEnumerable<InscricaoEventoDto>>> GetByEvento(int eventoId)
    {
        var items = await _service.GetByEventoAsync(eventoId);
        return Ok(items);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<InscricaoEventoDto>>> GetByStatus(StatusInscricao status)
    {
        var items = await _service.GetByStatusAsync(status);
        return Ok(items);
    }

    [Authorize]
    [HttpGet("minhas")]
    public async Task<ActionResult<IEnumerable<InscricaoEventoDto>>> GetMinhas()
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserEmail))
        {
            _logger.LogWarning("InscricoesEventos/minhas negado: usuario {UsuarioId} sem email no contexto.", _currentUser.UserId);
            return Unauthorized();
        }

        var items = await _service.GetByEmailAsync(_currentUser.UserEmail);
        _logger.LogInformation("InscricoesEventos/minhas carregado para usuario {UsuarioId} com email {Email}. Total {Total}.", _currentUser.UserId, _currentUser.UserEmail, items.Count());
        return Ok(items);
    }

    [HttpGet("evento/{eventoId}/estatisticas")]
    public async Task<ActionResult<EstatisticasInscricaoDto>> GetEstatisticas(int eventoId)
    {
        try
        {
            var estatisticas = await _service.ObterEstatisticasAsync(eventoId);
            return Ok(estatisticas);
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

    [HttpPost]
    public async Task<ActionResult<InscricaoEventoDto>> Create(CriarInscricaoEventoDto dto)
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
    public async Task<ActionResult<InscricaoEventoDto>> Update(int id, AtualizarInscricaoEventoDto dto)
    {
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

    [HttpPut("{id}/confirmar")]
    public async Task<ActionResult<InscricaoEventoDto>> Confirmar(int id)
    {
        try
        {
            var updated = await _service.ConfirmarInscricaoAsync(id);
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

    [HttpPut("{id}/cancelar")]
    public async Task<ActionResult<InscricaoEventoDto>> Cancelar(int id)
    {
        try
        {
            var updated = await _service.CancelarInscricaoAsync(id);
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



