using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SolicitacoesTrocasEscalasController : ControllerBase
{
    private readonly ISolicitacaoTrocaEscalaService _service;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<SolicitacoesTrocasEscalasController> _logger;

    public SolicitacoesTrocasEscalasController(ISolicitacaoTrocaEscalaService service, IUsuarioRepository usuarioRepository, ILogger<SolicitacoesTrocasEscalasController> logger)
    {
        _service = service;
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    [HttpGet("escala/{escalaId}")]
    public async Task<ActionResult<IEnumerable<SolicitacaoTrocaEscalaDto>>> GetByEscala(int escalaId)
    {
        try
        {
            var items = await _service.GetByEscalaAsync(escalaId, GetUsuarioId(), IsAdminUser());
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SolicitacaoTrocaEscalaDto>>> GetGerenciaveis([FromQuery] int? equipeId, [FromQuery] StatusSolicitacaoTrocaEscala? status)
    {
        var items = await _service.GetGerenciaveisAsync(GetUsuarioId(), IsAdminUser(), equipeId, status);
        return Ok(items);
    }

    [HttpGet("minhas")]
    public async Task<ActionResult<IEnumerable<SolicitacaoTrocaEscalaDto>>> GetMinhas()
    {
        var pessoaId = await GetUsuarioPessoaIdAsync();
        if (!pessoaId.HasValue)
        {
            _logger.LogWarning("SolicitacoesTrocasEscalas/minhas negado: usuario {UsuarioId} sem pessoa vinculada.", GetUsuarioId());
            return Unauthorized();
        }

        var items = await _service.GetMinhasAsync(pessoaId.Value);
        _logger.LogInformation("SolicitacoesTrocasEscalas/minhas carregado para usuario {UsuarioId}, pessoa {PessoaId}. Total {Total}.", GetUsuarioId(), pessoaId.Value, items.Count());
        return Ok(items);
    }

    [HttpPost("escala/{escalaId}/item/{escalaItemId}")]
    public async Task<ActionResult<SolicitacaoTrocaEscalaDto>> Create(int escalaId, int escalaItemId, CriarSolicitacaoTrocaEscalaDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(escalaId, escalaItemId, dto, GetUsuarioId(), IsAdminUser(), await GetUsuarioPessoaIdAsync());
            _logger.LogInformation("Solicitacao de troca criada. Usuario {UsuarioId}, escala {EscalaId}, item {EscalaItemId}, solicitacao {SolicitacaoId}.", GetUsuarioId(), escalaId, escalaItemId, created.Id);
            return Ok(created);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Solicitacao de troca negada para usuario {UsuarioId}, escala {EscalaId}, item {EscalaItemId}.", GetUsuarioId(), escalaId, escalaItemId);
            return StatusCode(403, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Solicitacao de troca invalida para usuario {UsuarioId}, escala {EscalaId}, item {EscalaItemId}.", GetUsuarioId(), escalaId, escalaItemId);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/aprovar")]
    public async Task<ActionResult<SolicitacaoTrocaEscalaDto>> Aprovar(int id, AprovarSolicitacaoTrocaEscalaDto dto)
    {
        try
        {
            var item = await _service.AprovarAsync(id, dto, GetUsuarioId(), IsAdminUser());
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/rejeitar")]
    public async Task<ActionResult<SolicitacaoTrocaEscalaDto>> Rejeitar(int id, RejeitarSolicitacaoTrocaEscalaDto dto)
    {
        try
        {
            var item = await _service.RejeitarAsync(id, dto, GetUsuarioId(), IsAdminUser());
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private int GetUsuarioId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    }

    private bool IsAdminUser()
    {
        var tipoUsuarioId = User.FindFirstValue("TipoUsuarioId");
        return tipoUsuarioId == ((int)TipoUsuario.Admin).ToString() ||
               tipoUsuarioId == ((int)TipoUsuario.Ambos).ToString();
    }

    private async Task<int?> GetUsuarioPessoaIdAsync()
    {
        var usuarioId = GetUsuarioId();
        if (usuarioId <= 0) return null;
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        return usuario?.PessoaId;
    }
}
