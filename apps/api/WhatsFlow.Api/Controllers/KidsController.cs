using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/kids")]
[Authorize]
public class KidsController : ControllerBase
{
    private readonly IKidsService _service;
    private readonly IKidsPreCheckinService _preCheckinService;
    private readonly IKidsConteudoAulaService _conteudoAulaService;
    private readonly IKidsNotificacaoService _notificacaoService;
    private readonly IKidsRetiradaService _retiradaService;
    private readonly IKidsPainelService _painelService;
    private readonly IKidsOcorrenciaService _ocorrenciaService;
    private readonly IKidsEstruturaService _estruturaService;
    private readonly IKidsIndicadoresService _indicadoresService;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IKidsDeviceTokenRepository _deviceTokenRepository;

    public KidsController(
        IKidsService service,
        IKidsPreCheckinService preCheckinService,
        IKidsConteudoAulaService conteudoAulaService,
        IKidsNotificacaoService notificacaoService,
        IKidsRetiradaService retiradaService,
        IKidsPainelService painelService,
        IKidsOcorrenciaService ocorrenciaService,
        IKidsEstruturaService estruturaService,
        IKidsIndicadoresService indicadoresService,
        IUsuarioRepository usuarioRepository,
        IKidsDeviceTokenRepository deviceTokenRepository)
    {
        _service = service;
        _preCheckinService = preCheckinService;
        _conteudoAulaService = conteudoAulaService;
        _notificacaoService = notificacaoService;
        _retiradaService = retiradaService;
        _painelService = painelService;
        _ocorrenciaService = ocorrenciaService;
        _estruturaService = estruturaService;
        _indicadoresService = indicadoresService;
        _usuarioRepository = usuarioRepository;
        _deviceTokenRepository = deviceTokenRepository;
    }

    /// <summary>
    /// Retorna indicadores operacionais consolidados do Kids.
    /// </summary>
    [HttpGet("indicadores")]
    public async Task<ActionResult<KidsIndicadoresDto>> GetIndicadores([FromQuery] int dias = 30)
    {
        try
        {
            var indicadores = await _indicadoresService.GetIndicadoresAsync(dias);
            return Ok(indicadores);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar indicadores de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista a estrutura de salas do Kids.
    /// </summary>
    [HttpGet("salas")]
    public async Task<ActionResult<IEnumerable<KidsSalaDto>>> GetSalas([FromQuery] bool incluirInativas = false)
    {
        try
        {
            var items = await _estruturaService.GetSalasAsync(incluirInativas);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar salas de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma nova sala do Kids.
    /// </summary>
    [HttpPost("salas")]
    public async Task<ActionResult<KidsSalaDto>> CreateSala([FromBody] CreateKidsSalaRequest request)
    {
        try
        {
            var created = await _estruturaService.CreateSalaAsync(request);
            return Ok(created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao criar sala de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma sala do Kids.
    /// </summary>
    [HttpPut("salas/{id}")]
    public async Task<ActionResult<KidsSalaDto>> UpdateSala(string id, [FromBody] UpdateKidsSalaRequest request)
    {
        try
        {
            var updated = await _estruturaService.UpdateSalaAsync(id, request);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao atualizar sala de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista as turmas do Kids.
    /// </summary>
    [HttpGet("turmas")]
    public async Task<ActionResult<IEnumerable<KidsTurmaDto>>> GetTurmas([FromQuery] string? salaId = null, [FromQuery] bool incluirInativas = false)
    {
        try
        {
            var items = await _estruturaService.GetTurmasAsync(salaId, incluirInativas);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar turmas de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma nova turma do Kids.
    /// </summary>
    [HttpPost("turmas")]
    public async Task<ActionResult<KidsTurmaDto>> CreateTurma([FromBody] CreateKidsTurmaRequest request)
    {
        try
        {
            var created = await _estruturaService.CreateTurmaAsync(request);
            return Ok(created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao criar turma de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma turma do Kids.
    /// </summary>
    [HttpPut("turmas/{id}")]
    public async Task<ActionResult<KidsTurmaDto>> UpdateTurma(string id, [FromBody] UpdateKidsTurmaRequest request)
    {
        try
        {
            var updated = await _estruturaService.UpdateTurmaAsync(id, request);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao atualizar turma de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista todas as crianças cadastradas
    /// </summary>
    [HttpGet("criancas")]
    public async Task<ActionResult<IEnumerable<CriancaDto>>> GetCriancas()
    {
        try
        {
            var criancas = await _service.GetCriancasAsync();
            return Ok(criancas);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar crianças", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista as crianças vinculadas ao responsável autenticado
    /// </summary>
    [HttpGet("me/criancas")]
    public async Task<ActionResult<IEnumerable<MinhaCriancaResumoDto>>> GetMinhasCriancas()
    {
        try
        {
            var criancas = await _service.GetMinhasCriancasAsync();
            return Ok(criancas);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar minhas crianças", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém detalhe resumido de uma criança vinculada ao responsável autenticado
    /// </summary>
    [HttpGet("me/criancas/{criancaPessoaId}")]
    public async Task<ActionResult<MinhaCriancaDetalheDto>> GetMinhaCriancaById(int criancaPessoaId)
    {
        try
        {
            var crianca = await _service.GetMinhaCriancaByIdAsync(criancaPessoaId);
            if (crianca == null)
                return NotFound(new { message = "Criança não encontrada" });

            return Ok(crianca);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar minha criança", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista os check-ins permitidos do responsável autenticado
    /// </summary>
    [HttpGet("me/checkins")]
    public async Task<ActionResult<IEnumerable<MeuCheckinResumoDto>>> GetMeusCheckins()
    {
        try
        {
            var checkins = await _service.GetMeusCheckinsAsync();
            return Ok(checkins);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar meus check-ins", error = ex.Message });
        }
    }

    /// <summary>
    /// Histórico paginado de check-ins do responsável autenticado.
    /// </summary>
    [HttpGet("me/historico")]
    public async Task<ActionResult<MeuHistoricoPagedDto>> GetMeuHistorico(
        [FromQuery] int? criancaPessoaId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var resultado = await _service.GetMeuHistoricoAsync(criancaPessoaId, page, pageSize);
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar histórico", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista os conteúdos publicados aplicáveis a uma criança vinculada ao responsável autenticado.
    /// </summary>
    [HttpGet("me/criancas/{criancaPessoaId}/conteudos-aula")]
    public async Task<ActionResult<IEnumerable<MeuConteudoAulaDto>>> GetMeuConteudoPorCrianca(int criancaPessoaId, [FromQuery] int? limit = null)
    {
        try
        {
            var items = await _conteudoAulaService.GetMeuConteudoPorCriancaAsync(criancaPessoaId, limit);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar conteúdos da aula", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria um pré-check-in para uma criança vinculada ao responsável autenticado.
    /// </summary>
    [HttpPost("me/precheckins")]
    public async Task<ActionResult<KidsPreCheckinDto>> CreateMeuPreCheckin([FromBody] CreateKidsPreCheckinRequest request)
    {
        try
        {
            var created = await _preCheckinService.CriarMeuPreCheckinAsync(request);
            return Ok(created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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
            return StatusCode(500, new { message = "Erro ao criar meu pré-check-in", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista os pré-check-ins do responsável autenticado.
    /// </summary>
    [HttpGet("me/precheckins")]
    public async Task<ActionResult<IEnumerable<KidsPreCheckinDto>>> GetMeusPreCheckins([FromQuery] string? status = null, [FromQuery] bool somenteAtivos = false)
    {
        try
        {
            var items = await _preCheckinService.GetMeusPreCheckinsAsync(status, somenteAtivos);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar meus pré-check-ins", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela um pré-check-in do responsável autenticado.
    /// </summary>
    [HttpPost("me/precheckins/{id}/cancelar")]
    public async Task<ActionResult<KidsPreCheckinDto>> CancelarMeuPreCheckin(int id, [FromBody] CancelKidsPreCheckinRequest request)
    {
        try
        {
            var item = await _preCheckinService.CancelarMeuPreCheckinAsync(id, request);
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao cancelar meu pré-check-in", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista os pré-check-ins pendentes para conferência operacional.
    /// </summary>
    [HttpGet("precheckins/pendentes")]
    public async Task<ActionResult<IEnumerable<KidsPreCheckinDto>>> GetPreCheckinsPendentes([FromQuery] int? eventoOcorrenciaId = null, [FromQuery] string? salaId = null, [FromQuery] string? turmaId = null)
    {
        try
        {
            var items = await _preCheckinService.GetPendentesAsync(eventoOcorrenciaId, salaId, turmaId);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar pré-check-ins pendentes", error = ex.Message });
        }
    }

    /// <summary>
    /// Valida um pré-check-in por token ou código curto.
    /// </summary>
    [HttpPost("precheckins/validar")]
    public async Task<ActionResult<KidsPreCheckinDto>> ValidarPreCheckin([FromBody] ValidarKidsPreCheckinRequest request)
    {
        try
        {
            var item = await _preCheckinService.ValidarAsync(request);
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao validar pré-check-in", error = ex.Message });
        }
    }

    /// <summary>
    /// Confirma um pré-check-in e realiza o check-in operacional.
    /// </summary>
    [HttpPost("precheckins/{id}/confirmar")]
    public async Task<ActionResult<KidsPreCheckinDto>> ConfirmarPreCheckin(int id, [FromBody] ConfirmKidsPreCheckinRequest request)
    {
        try
        {
            var item = await _preCheckinService.ConfirmarAsync(id, request);
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao confirmar pré-check-in", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela um pré-check-in pendente.
    /// </summary>
    [HttpPost("precheckins/{id}/cancelar")]
    public async Task<ActionResult<KidsPreCheckinDto>> CancelarPreCheckin(int id, [FromBody] CancelKidsPreCheckinRequest request)
    {
        try
        {
            var item = await _preCheckinService.CancelarAsync(id, request);
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao cancelar pré-check-in", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista os avisos do responsável autenticado
    /// </summary>
    [HttpGet("me/avisos")]
    public async Task<ActionResult<IEnumerable<MeuAvisoKidsDto>>> GetMeusAvisos([FromQuery] bool naoLidos = false, [FromQuery] string? tipo = null, [FromQuery] int? criancaPessoaId = null, [FromQuery] int? limit = null)
    {
        try
        {
            var avisos = await _notificacaoService.GetMeusAvisosAsync(naoLidos, tipo, criancaPessoaId, limit);
            return Ok(avisos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar meus avisos", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista os conteúdos administrativos de aula do Kids.
    /// </summary>
    [HttpGet("conteudos-aula")]
    public async Task<ActionResult<IEnumerable<KidsConteudoAulaAdminDto>>> GetConteudosAula([FromQuery] string? status = null, [FromQuery] string? salaId = null, [FromQuery] string? turmaId = null, [FromQuery] DateTime? dataReferencia = null, [FromQuery] int? limit = null)
    {
        try
        {
            var items = await _conteudoAulaService.GetAsync(status, salaId, turmaId, dataReferencia, limit);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar conteúdos da aula", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém um conteúdo de aula do Kids por identificador.
    /// </summary>
    [HttpGet("conteudos-aula/{id}")]
    public async Task<ActionResult<KidsConteudoAulaAdminDto>> GetConteudoAulaById(int id)
    {
        try
        {
            var item = await _conteudoAulaService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { message = "Conteúdo da aula não encontrado" });
            }

            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar conteúdo da aula", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria um novo conteúdo de aula do Kids.
    /// </summary>
    [HttpPost("conteudos-aula")]
    public async Task<ActionResult<KidsConteudoAulaAdminDto>> CreateConteudoAula([FromBody] CreateKidsConteudoAulaRequest request)
    {
        try
        {
            var created = await _conteudoAulaService.CreateAsync(request);
            return Ok(created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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
            return StatusCode(500, new { message = "Erro ao criar conteúdo da aula", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um conteúdo de aula do Kids.
    /// </summary>
    [HttpPut("conteudos-aula/{id}")]
    public async Task<ActionResult<KidsConteudoAulaAdminDto>> UpdateConteudoAula(int id, [FromBody] UpdateKidsConteudoAulaRequest request)
    {
        try
        {
            var updated = await _conteudoAulaService.UpdateAsync(id, request);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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
            return StatusCode(500, new { message = "Erro ao atualizar conteúdo da aula", error = ex.Message });
        }
    }

    /// <summary>
    /// Publica um conteúdo de aula do Kids.
    /// </summary>
    [HttpPost("conteudos-aula/{id}/publicar")]
    public async Task<ActionResult<KidsConteudoAulaAdminDto>> PublicarConteudoAula(int id)
    {
        try
        {
            var updated = await _conteudoAulaService.PublicarAsync(id);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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
            return StatusCode(500, new { message = "Erro ao publicar conteúdo da aula", error = ex.Message });
        }
    }

    /// <summary>
    /// Arquiva um conteúdo de aula do Kids.
    /// </summary>
    [HttpPost("conteudos-aula/{id}/arquivar")]
    public async Task<ActionResult<KidsConteudoAulaAdminDto>> ArquivarConteudoAula(int id)
    {
        try
        {
            var updated = await _conteudoAulaService.ArquivarAsync(id);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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
            return StatusCode(500, new { message = "Erro ao arquivar conteúdo da aula", error = ex.Message });
        }
    }

    /// <summary>
    /// Marca um aviso como lido para o responsável autenticado
    /// </summary>
    [HttpPatch("me/avisos/{id}/lido")]
    public async Task<ActionResult<MeuAvisoKidsDto>> MarcarMeuAvisoComoLido(int id)
    {
        try
        {
            var aviso = await _notificacaoService.MarcarComoLidoAsync(id);
            return Ok(aviso);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao marcar aviso como lido", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém detalhes de uma criança específica
    /// </summary>
    [HttpGet("criancas/{criancaPessoaId}")]
    public async Task<ActionResult<CriancaDto>> GetCriancaById(int criancaPessoaId)
    {
        try
        {
            var crianca = await _service.GetCriancaByIdAsync(criancaPessoaId);
            if (crianca == null)
                return NotFound(new { message = "Criança não encontrada" });

            return Ok(crianca);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar criança", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma nova criança
    /// </summary>
    [HttpPost("criancas")]
    public async Task<ActionResult<CriancaDto>> CreateCrianca([FromBody] CreateCriancaRequest request)
    {
        try
        {
            var ipOrigem = HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var crianca = await _service.CreateCriancaAsync(request, ipOrigem);
            return CreatedAtAction(nameof(GetCriancaById), new { criancaPessoaId = crianca.PessoaId }, crianca);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Incluir inner exception para debug
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }
            return StatusCode(500, new { message = "Erro ao criar criança", error = errorMessage, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Atualiza dados de uma criança
    /// </summary>
    [HttpPut("criancas/{criancaPessoaId}")]
    public async Task<ActionResult<CriancaDto>> UpdateCrianca(int criancaPessoaId, [FromBody] UpdateCriancaRequest request)
    {
        try
        {
            var crianca = await _service.UpdateCriancaAsync(criancaPessoaId, request);
            return Ok(crianca);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Desativa uma criança (soft delete)
    /// </summary>
    [HttpDelete("criancas/{criancaPessoaId}")]
    public async Task<IActionResult> DeleteCrianca(int criancaPessoaId)
    {
        try
        {
            await _service.DeleteCriancaAsync(criancaPessoaId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Vincula um responsável a uma criança
    /// </summary>
    [HttpPost("criancas/{criancaPessoaId}/responsaveis")]
    public async Task<ActionResult<ResponsavelCriancaDto>> VincularResponsavel(
        int criancaPessoaId,
        [FromBody] CreateResponsavelRequest request)
    {
        try
        {
            var responsavel = await _service.VincularResponsavelAsync(criancaPessoaId, request);
            return CreatedAtAction(
                nameof(GetCriancaById),
                new { criancaPessoaId },
                responsavel);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao vincular responsável", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza vínculo de responsável
    /// </summary>
    [HttpPut("responsaveis/{responsavelId}")]
    public async Task<ActionResult<ResponsavelCriancaDto>> UpdateResponsavel(
        int responsavelId,
        [FromBody] UpdateResponsavelRequest request)
    {
        try
        {
            var responsavel = await _service.UpdateResponsavelAsync(responsavelId, request);
            return Ok(responsavel);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Desvincula um responsável de uma criança
    /// </summary>
    [HttpDelete("responsaveis/{responsavelId}")]
    public async Task<IActionResult> DesvincularResponsavel(int responsavelId)
    {
        try
        {
            await _service.DesvincularResponsavelAsync(responsavelId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Realiza check-in de uma criança
    /// </summary>
    [HttpPost("checkin")]
    public async Task<ActionResult<CheckinResponse>> Checkin([FromBody] CheckinRequest request)
    {
        try
        {
            var response = await _service.CheckinAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao realizar check-in", error = ex.Message });
        }
    }

    /// <summary>
    /// Realiza check-out de uma criança
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        try
        {
            await _service.CheckoutAsync(request);
            return Ok(new { message = "Check-out realizado com sucesso" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao realizar check-out", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista histórico de check-ins/check-outs
    /// </summary>
    [HttpGet("checkins")]
    public async Task<ActionResult<IEnumerable<KidsCheckinDto>>> GetHistoricoCheckins(
        [FromQuery] int? criancaPessoaId = null)
    {
        try
        {
            var historico = await _service.GetHistoricoCheckinsAsync(criancaPessoaId);
            return Ok(historico);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar histórico", error = ex.Message });
        }
    }

    /// <summary>
    /// Retorna o painel operacional do culto atual do Kids.
    /// </summary>
    [HttpGet("painel-operacional")]
    public async Task<ActionResult<KidsPainelOperacionalDto>> GetPainelOperacional([FromQuery] DateTime? data = null, [FromQuery] string? salaId = null)
    {
        try
        {
            var painel = await _painelService.GetPainelOperacionalAsync(data, salaId);
            return Ok(painel);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao carregar painel operacional do Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Registra uma ocorrência de Kids para uma criança.
    /// </summary>
    [HttpPost("ocorrencias")]
    public async Task<ActionResult<KidsOcorrenciaDto>> CreateOcorrencia([FromBody] CriarKidsOcorrenciaRequest request)
    {
        try
        {
            var created = await _ocorrenciaService.CriarAsync(request);
            return Ok(created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao criar ocorrência de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista ocorrências de uma criança.
    /// </summary>
    [HttpGet("criancas/{criancaPessoaId}/ocorrencias")]
    public async Task<ActionResult<IEnumerable<KidsOcorrenciaDto>>> GetOcorrenciasByCrianca(int criancaPessoaId)
    {
        try
        {
            var items = await _ocorrenciaService.GetByCriancaAsync(criancaPessoaId);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar ocorrências da criança", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma ocorrência de Kids.
    /// </summary>
    [HttpPatch("ocorrencias/{id}")]
    public async Task<ActionResult<KidsOcorrenciaDto>> UpdateOcorrencia(int id, [FromBody] AtualizarKidsOcorrenciaRequest request)
    {
        try
        {
            var updated = await _ocorrenciaService.AtualizarAsync(id, request);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao atualizar ocorrência de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista ocorrências abertas para operação.
    /// </summary>
    [HttpGet("ocorrencias/abertas")]
    public async Task<ActionResult<IEnumerable<KidsOcorrenciaResumoDto>>> GetOcorrenciasAbertas()
    {
        try
        {
            var items = await _ocorrenciaService.GetAbertasAsync();
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar ocorrências abertas de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Valida token ou PIN de retirada e carrega o contexto da sessão.
    /// </summary>
    [HttpPost("retirada/validar")]
    public async Task<ActionResult<RetiradaValidacaoDto>> ValidarRetirada([FromBody] ValidarRetiradaRequest request)
    {
        try
        {
            var result = await _retiradaService.ValidarAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao validar retirada", error = ex.Message });
        }
    }

    /// <summary>
    /// Confirma a retirada segura da criança.
    /// </summary>
    [HttpPost("retirada/confirmar")]
    public async Task<IActionResult> ConfirmarRetirada([FromBody] ConfirmarRetiradaRequest request)
    {
        try
        {
            await _retiradaService.ConfirmarAsync(request);
            return Ok(new { message = "Retirada confirmada com sucesso" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao confirmar retirada", error = ex.Message });
        }
    }

    /// <summary>
    /// Registra retirada em modo de exceção com trilha auditável.
    /// </summary>
    [HttpPost("retirada/excecao")]
    public async Task<IActionResult> RegistrarRetiradaExcecao([FromBody] RetiradaExcecaoRequest request)
    {
        try
        {
            await _retiradaService.RegistrarExcecaoAsync(request);
            return Ok(new { message = "Retirada em exceção registrada com sucesso" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao registrar retirada em exceção", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista avisos manuais emitidos no módulo Kids
    /// </summary>
    [HttpGet("avisos")]
    public async Task<ActionResult<IEnumerable<KidsNotificacaoDto>>> GetAvisos([FromQuery] string? tipo = null, [FromQuery] int? responsavelPessoaId = null, [FromQuery] int? criancaPessoaId = null, [FromQuery] int? limit = null)
    {
        try
        {
            var avisos = await _notificacaoService.GetAvisosAsync(tipo, responsavelPessoaId, criancaPessoaId, limit);
            return Ok(avisos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar avisos de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria um aviso manual para responsáveis do Kids
    /// </summary>
    [HttpPost("avisos")]
    public async Task<ActionResult<KidsNotificacaoDto>> CreateAviso([FromBody] CreateKidsAvisoRequest request)
    {
        try
        {
            var aviso = await _notificacaoService.CriarAvisoAsync(request);
            return Ok(aviso);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao criar aviso de Kids", error = ex.Message });
        }
    }

    /// <summary>
    /// Registra o token FCM do dispositivo do usuário logado (para receber push de check-in/check-out e avisos).
    /// </summary>
    [HttpPost("me/device-token")]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request)
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var usuario = await _usuarioRepository.GetByIdAsync(userId);
            if (usuario == null) return Unauthorized();

            await _deviceTokenRepository.UpsertAsync(usuario.PessoaId, request.Token.Trim(), request.Platform ?? "Android");
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao registrar token", error = ex.Message });
        }
    }

    /// <summary>
    /// Remove o token FCM do dispositivo ao fazer logout, evitando push após desconexão.
    /// </summary>
    [HttpDelete("me/device-token")]
    public async Task<IActionResult> UnregisterDeviceToken([FromBody] UnregisterDeviceTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { message = "Token é obrigatório" });

            await _deviceTokenRepository.DeleteByTokenAsync(request.Token.Trim());
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao remover token", error = ex.Message });
        }
    }
}
