using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Visitantes;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VisitantesController : ControllerBase
{
    private readonly IVisitanteService _visitanteService;
    private readonly IMensagemAgendadaService _mensagemService;
    private readonly ILogger<VisitantesController> _logger;

    public VisitantesController(
        IVisitanteService visitanteService,
        IMensagemAgendadaService mensagemService,
        ILogger<VisitantesController> logger)
    {
        _visitanteService = visitanteService;
        _mensagemService = mensagemService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os visitantes com dados da Pessoa
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VisitanteDto>>> GetAll()
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem listar visitantes.");
        }

        var visitantes = await _visitanteService.GetAllAsync();
        return Ok(visitantes);
    }

    /// <summary>
    /// Lista visitantes com paginação e filtros (server-side).
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDto<VisitanteDto>>> GetPaged([FromQuery] VisitantePagedQueryDto query)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem listar visitantes.");
        }

        var result = await _visitanteService.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Obtém detalhe de um visitante específico, incluindo dados da Pessoa e perfis
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<VisitanteDto>> GetById(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem visualizar visitantes.");
        }

        var visitante = await _visitanteService.GetByIdAsync(id);
        if (visitante == null)
            return NotFound();

        return Ok(visitante);
    }

    /// <summary>
    /// Lista todas as visitas de uma Pessoa específica
    /// </summary>
    [HttpGet("pessoa/{pessoaId}")]
    public async Task<ActionResult<IEnumerable<VisitanteDto>>> GetByPessoa(int pessoaId)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem listar visitantes por pessoa.");
        }

        var visitantes = await _visitanteService.GetVisitantesPorPessoaAsync(pessoaId);
        return Ok(visitantes);
    }

    /// <summary>
    /// Cria um novo visitante seguindo fluxo de deduplicação de Pessoa
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VisitanteResponse>> Create(CreateVisitanteRequest request)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem criar visitantes.");
        }

        try
        {
            var visitante = await _visitanteService.CreateVisitanteAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = visitante.VisitanteId }, visitante);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var rootCause = ex.GetBaseException().Message;
            _logger.LogError(ex, "Erro ao criar visitante. Nome={Nome} WhatsApp={WhatsApp}", request.Nome, request.WhatsApp);
            return StatusCode(500, new
            {
                message = "Erro ao criar visitante",
                error = ex.Message,
                detail = rootCause
            });
        }
    }

    /// <summary>
    /// Atualiza observações e data de visita (não altera dados da Pessoa)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<VisitanteDto>> Update(int id, AtualizarVisitanteDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem atualizar visitantes.");
        }

        try
        {
            var visitante = await _visitanteService.UpdateAsync(id, dto);
            return Ok(visitante);
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
    /// Regera mensagens agendadas do visitante (cancela pendentes e recria conforme configurações ativas).
    /// </summary>
    [HttpPost("{id}/regerar-mensagens")]
    public async Task<ActionResult<RegerarMensagensResultDto>> RegerarMensagens(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem regerar mensagens de visitantes.");
        }

        try
        {
            var result = await _mensagemService.RegerarMensagensParaVisitanteAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao regerar mensagens", error = ex.Message });
        }
    }

    /// <summary>
    /// Remove um registro de visitante
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem excluir visitantes.");
        }

        try
        {
            await _visitanteService.DeleteAsync(id);
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
