using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Pessoas;
using WhatsFlow.Application.Services;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação para acessar
public class PessoasController : ControllerBase
{
    private readonly IPessoaService _service;
    private readonly ICurrentUserContext _currentUser;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<PessoasController> _logger;
    private readonly IDadosPessoaisService? _dadosPessoais;

    public PessoasController(IPessoaService service, ICurrentUserContext currentUser, IUsuarioRepository usuarioRepository, ILogger<PessoasController> logger, IDadosPessoaisService? dadosPessoais = null)
    {
        _service = service;
        _currentUser = currentUser;
        _usuarioRepository = usuarioRepository;
        _logger = logger;
        _dadosPessoais = dadosPessoais;
    }

    /// <summary>
    /// Lista todas as pessoas com seus perfis
    /// </summary>
    [HttpGet("aniversariantes")]
    public async Task<ActionResult<IEnumerable<AniversarianteDto>>> GetAniversariantes(
        [FromQuery] int dias = 30,
        [FromQuery] int limite = 50,
        [FromQuery] int? mes = null)
    {
        var items = mes.HasValue
            ? await _service.GetAniversariantesPorMesAsync(mes.Value, limite)
            : await _service.GetProximosAniversariantesAsync(dias, limite);
        return Ok(items);
    }

    /// <summary>
    /// Lista todas as pessoas com seus perfis
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PessoaDto>>> GetAll()
    {
        var pessoas = await _service.GetAllAsync();
        return Ok(pessoas);
    }

    /// <summary>
    /// Lista pessoas com paginação e filtros (server-side).
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDto<PessoaDto>>> GetPaged([FromQuery] PessoaPagedQueryDto query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Obtém detalhe de uma pessoa específica com seus perfis
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PessoaDto>> GetById(int id)
    {
        var pessoa = await _service.GetByIdAsync(id);
        if (pessoa == null)
            return NotFound();

        return Ok(pessoa);
    }

    /// <summary>
    /// Exporta os dados pessoais do titular (LGPD, Art. 18 — acesso e portabilidade).
    /// </summary>
    [HttpGet("{id}/dados-pessoais")]
    public async Task<ActionResult<DadosPessoaisExportDto>> ExportarDadosPessoais(int id)
    {
        if (_dadosPessoais == null)
            return StatusCode(501, new { message = "Serviço de dados pessoais não disponível." });

        var dados = await _dadosPessoais.ExportarAsync(id);
        if (dados == null)
            return NotFound();

        return Ok(dados);
    }

    /// <summary>
    /// Anonimiza os dados do titular (LGPD — direito ao esquecimento), preservando
    /// vínculos e agregados (financeiro/presença) e revogando consentimentos ativos.
    /// </summary>
    [HttpPost("{id}/anonimizar")]
    public async Task<ActionResult<AnonimizacaoResultadoDto>> Anonimizar(int id)
    {
        if (_dadosPessoais == null)
            return StatusCode(501, new { message = "Serviço de dados pessoais não disponível." });

        var resultado = await _dadosPessoais.AnonimizarAsync(id);
        if (resultado == null)
            return NotFound();

        _logger.LogInformation("Pessoa {PessoaId} anonimizada (LGPD) pelo usuário {UserId}", id, _currentUser.UserId);
        return Ok(resultado);
    }

    [HttpGet("me")]
    public async Task<ActionResult<PessoaDto>> GetMe()
    {
        var pessoaId = await GetCurrentPessoaIdAsync();
        if (!pessoaId.HasValue)
        {
            _logger.LogWarning("Pessoas/me negado: usuario {UsuarioId} sem pessoa vinculada.", _currentUser.UserId);
            return Unauthorized();
        }

        var pessoa = await _service.GetByIdAsync(pessoaId.Value);
        if (pessoa == null) return NotFound();

        _logger.LogInformation("Pessoas/me carregado para usuario {UsuarioId} e pessoa {PessoaId}.", _currentUser.UserId, pessoaId.Value);
        return Ok(pessoa);
    }

    /// <summary>
    /// Visão consolidada 360° da pessoa (perfis, visitas, voluntariado, usuário)
    /// </summary>
    [HttpGet("{id}/360")]
    public async Task<ActionResult<Pessoa360Dto>> Get360(int id)
    {
        var result = await _service.Get360Async(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Cria uma nova pessoa
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PessoaDto>> Create(CriarPessoaDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem criar pessoas.");
        }

        try
        {
            var pessoa = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = pessoa.Id }, pessoa);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao criar pessoa", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza dados de uma pessoa
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PessoaDto>> Update(int id, AtualizarPessoaDto dto)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem atualizar pessoas.");
        }

        try
        {
            var pessoa = await _service.UpdateAsync(id, dto);
            return Ok(pessoa);
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

    [HttpPut("me")]
    public async Task<ActionResult<PessoaDto>> UpdateMe(AtualizarMinhaPessoaDto dto)
    {
        var pessoaId = await GetCurrentPessoaIdAsync();
        if (!pessoaId.HasValue)
        {
            _logger.LogWarning("Pessoas/me update negado: usuario {UsuarioId} sem pessoa vinculada.", _currentUser.UserId);
            return Unauthorized();
        }

        try
        {
            var pessoa = await _service.UpdateMinhaPessoaAsync(pessoaId.Value, dto);
            _logger.LogInformation("Pessoas/me atualizado para usuario {UsuarioId} e pessoa {PessoaId}.", _currentUser.UserId, pessoaId.Value);
            return Ok(pessoa);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Pessoas/me update invalido para usuario {UsuarioId} e pessoa {PessoaId}.", _currentUser.UserId, pessoaId.Value);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao atualizar Pessoas/me para usuario {UsuarioId} e pessoa {PessoaId}.", _currentUser.UserId, pessoaId.Value);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove uma pessoa
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem excluir pessoas.");
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

    private async Task<int?> GetCurrentPessoaIdAsync()
    {
        if (!_currentUser.UserId.HasValue) return null;
        var usuario = await _usuarioRepository.GetByIdAsync(_currentUser.UserId.Value);
        return usuario?.PessoaId;
    }
}
