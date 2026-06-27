using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Application.Utils;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EscalasController : ControllerBase
{
    private sealed record DestinatarioWhatsApp(string Nome, string WhatsApp);

    private readonly IEscalaService _service;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEvolutionApiService _evolutionApiService;
    private readonly PublicAppUrlSettings _publicAppUrlSettings;

    public EscalasController(
        IEscalaService service,
        IUsuarioRepository usuarioRepository,
        IEvolutionApiService evolutionApiService,
        IOptions<PublicAppUrlSettings> publicAppUrlSettings)
    {
        _service = service;
        _usuarioRepository = usuarioRepository;
        _evolutionApiService = evolutionApiService;
        _publicAppUrlSettings = publicAppUrlSettings.Value;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EscalaDto>> GetById(int id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, GetUsuarioId(), IsAdminUser());
            if (item == null) return NotFound();
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }

    [HttpGet("ocorrencia/{eventoOcorrenciaId}")]
    public async Task<ActionResult<EscalaDto>> GetByEventoOcorrencia(int eventoOcorrenciaId)
    {
        try
        {
            var item = await _service.GetByEventoOcorrenciaAsync(eventoOcorrenciaId, GetUsuarioId(), IsAdminUser());
            if (item == null) return NotFound();
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }

    [HttpGet("ocorrencia/{eventoOcorrenciaId}/escalas")]
    public async Task<ActionResult<IEnumerable<EscalaDto>>> GetAllByEventoOcorrencia(int eventoOcorrenciaId)
    {
        var items = await _service.GetAllByEventoOcorrenciaAsync(eventoOcorrenciaId, GetUsuarioId(), IsAdminUser());
        return Ok(items);
    }

    [HttpGet("ocorrencia/{eventoOcorrenciaId}/equipe/{equipeId}")]
    public async Task<ActionResult<EscalaDto>> GetByEventoOcorrenciaAndEquipe(int eventoOcorrenciaId, int equipeId)
    {
        try
        {
            var item = await _service.GetByEventoOcorrenciaAndEquipeAsync(eventoOcorrenciaId, equipeId, GetUsuarioId(), IsAdminUser());
            if (item == null) return NotFound();
            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }

    [HttpGet("minhas")]
    public async Task<ActionResult<IEnumerable<EscalaDto>>> GetMinhas([FromQuery] bool somenteFuturas = true)
    {
        var usuarioId = GetUsuarioId();
        var usuarioPessoaId = await GetUsuarioPessoaIdAsync(usuarioId);
        if (!usuarioPessoaId.HasValue)
        {
            return Unauthorized();
        }

        var items = await _service.GetMinhasEscalasAsync(usuarioPessoaId.Value, somenteFuturas);
        return Ok(items);
    }

    [HttpGet("historico-voluntarios")]
    public async Task<ActionResult<IEnumerable<HistoricoVoluntarioDto>>> GetHistoricoVoluntarios(
        [FromQuery] int? equipeId = null,
        [FromQuery] int? eventoId = null,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        var items = await _service.GetHistoricoVoluntariosAsync(GetUsuarioId(), IsAdminUser(), equipeId, eventoId, dataInicio, dataFim);
        return Ok(items);
    }

    [HttpGet("planejamento-mensal")]
    public async Task<ActionResult<PlanejamentoMensalEscalaDto>> GetPlanejamentoMensal(
        [FromQuery] int ano,
        [FromQuery] int mes,
        [FromQuery] int? equipeId = null,
        [FromQuery] int? eventoId = null)
    {
        try
        {
            var item = await _service.GetPlanejamentoMensalAsync(GetUsuarioId(), IsAdminUser(), ano, mes, equipeId, eventoId);
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

    [HttpPost("planejamento-mensal/gerar-automatico")]
    public async Task<ActionResult<GerarPlanejamentoMensalResultadoDto>> GerarPlanejamentoMensalAutomatico(GerarPlanejamentoMensalDto dto)
    {
        try
        {
            var item = await _service.GerarPlanejamentoMensalAutomaticoAsync(dto, GetUsuarioId(), IsAdminUser());
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

    [HttpPost("planejamento-mensal/alocacoes")]
    public async Task<ActionResult<EscalaItemDto>> CriarAlocacaoPlanejamentoMensal(CriarAlocacaoPlanejamentoMensalDto dto)
    {
        try
        {
            var item = await _service.CriarAlocacaoPlanejamentoMensalAsync(dto, GetUsuarioId(), IsAdminUser());
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

    [HttpPost("planejamento-mensal/disparar-whatsapp")]
    public async Task<ActionResult<DispararPlanejamentoMensalWhatsAppResultadoDto>> DispararPlanejamentoMensalWhatsApp(
        DispararPlanejamentoMensalWhatsAppDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (dto.EquipeId <= 0) return BadRequest("Equipe é obrigatória para disparar a escala mensal.");
            if (string.IsNullOrWhiteSpace(dto.ImagemUrl)) return BadRequest("Imagem da escala é obrigatória.");
            var planejamento = await _service.GetPlanejamentoMensalAsync(
                GetUsuarioId(),
                IsAdminUser(),
                dto.Ano,
                dto.Mes,
                dto.EquipeId,
                dto.EventoId);

            var destinatarios = planejamento.Voluntarios
                .Where(v => !string.IsNullOrWhiteSpace(v.WhatsApp))
                .GroupBy(v => v.PessoaId)
                .Select(g => g.First())
                .OrderBy(v => v.Nome)
                .Select(v => new DestinatarioWhatsApp(v.Nome, v.WhatsApp!))
                .ToList();

            var whatsappTesteNormalizado = TelefoneUtils.NormalizarTelefone(dto.WhatsAppTeste);
            if (!string.IsNullOrWhiteSpace(whatsappTesteNormalizado))
            {
                destinatarios = new List<DestinatarioWhatsApp>
                {
                    new("Destino de teste", dto.WhatsAppTeste!.Trim())
                };
            }

            var resultado = new DispararPlanejamentoMensalWhatsAppResultadoDto
            {
                TotalDestinatarios = destinatarios.Count
            };

            if (destinatarios.Count == 0)
            {
                return Ok(resultado);
            }

            var mensagem = string.IsNullOrWhiteSpace(dto.Mensagem)
                ? BuildMensagemPlanejamentoMensal(planejamento)
                : dto.Mensagem.Trim();
            var imagemUrl = ResolverImagemUrl(dto.ImagemUrl);

            foreach (var voluntario in destinatarios)
            {
                var response = await _evolutionApiService.EnviarMensagemImagemAsync(
                    voluntario.WhatsApp!,
                    imagemUrl,
                    mensagem,
                    cancellationToken);

                if (response.Sucesso)
                {
                    resultado.TotalEnviados++;
                    continue;
                }

                var fallbackResponse = await _evolutionApiService.EnviarMensagemTextoAsync(
                    voluntario.WhatsApp!,
                    mensagem,
                    cancellationToken);

                if (fallbackResponse.Sucesso)
                {
                    resultado.TotalEnviados++;
                    resultado.Falhas.Add($"{voluntario.Nome}: mídia falhou, mas texto foi entregue.");
                    continue;
                }

                resultado.TotalFalhas++;
                resultado.Falhas.Add($"{voluntario.Nome}: {fallbackResponse.MensagemErro ?? response.MensagemErro ?? "falha no envio"}");
            }

            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{escalaId}/sugestoes")]
    public async Task<ActionResult<IEnumerable<SugestaoEscalaVoluntarioDto>>> GetSugestoes(int escalaId, [FromQuery] int equipeId)
    {
        try
        {
            var itens = await _service.GetSugestoesAsync(escalaId, equipeId, GetUsuarioId(), IsAdminUser());
            return Ok(itens);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("não encontrada"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<EscalaDto>> Create(CriarEscalaDto dto)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var created = await _service.CreateAsync(dto, usuarioId, IsAdminUser());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EscalaDto>> Update(int id, AtualizarEscalaDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto, GetUsuarioId(), IsAdminUser());
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("não encontrada"))
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
            await _service.DeleteAsync(id, GetUsuarioId(), IsAdminUser());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{escalaId}/itens")]
    public async Task<ActionResult<EscalaItemDto>> AddItem(int escalaId, CriarEscalaItemDto dto)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var isAdmin = IsAdminUser();
            var created = await _service.AddItemAsync(escalaId, dto, usuarioId, isAdmin);
            return Ok(created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{escalaId}/itens/{escalaItemId}")]
    public async Task<ActionResult<EscalaItemDto>> UpdateItem(int escalaId, int escalaItemId, AtualizarEscalaItemDto dto)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var isAdmin = IsAdminUser();
            var updated = await _service.UpdateItemAsync(escalaId, escalaItemId, dto, usuarioId, isAdmin);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{escalaId}/itens/{escalaItemId}")]
    public async Task<IActionResult> DeleteItem(int escalaId, int escalaItemId)
    {
        try
        {
            await _service.DeleteItemAsync(escalaId, escalaItemId, GetUsuarioId(), IsAdminUser());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{escalaId}/publicar")]
    public async Task<ActionResult<EscalaDto>> Publicar(int escalaId)
    {
        try
        {
            var updated = await _service.PublicarAsync(escalaId, GetUsuarioId(), IsAdminUser());
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("ocorrencia/{eventoOcorrenciaId}/equipe/{equipeId}/gerar-automatico")]
    public async Task<ActionResult<EscalaDto>> GerarAutomatico(int eventoOcorrenciaId, int equipeId)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var escala = await _service.GerarAutomaticoAsync(eventoOcorrenciaId, equipeId, usuarioId, IsAdminUser());
            return Ok(escala);
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

    [HttpPost("{escalaId}/itens/{escalaItemId}/confirmar")]
    public async Task<ActionResult<EscalaItemDto>> ConfirmarItem(int escalaId, int escalaItemId)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var usuarioPessoaId = await GetUsuarioPessoaIdAsync(usuarioId);
            var item = await _service.ConfirmarItemAsync(escalaId, escalaItemId, usuarioId, IsAdminUser(), usuarioPessoaId);
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

    [HttpPost("{escalaId}/itens/{escalaItemId}/recusar")]
    public async Task<ActionResult<EscalaItemDto>> RecusarItem(int escalaId, int escalaItemId, [FromBody] RecusarEscalaItemDto dto)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var usuarioPessoaId = await GetUsuarioPessoaIdAsync(usuarioId);
            var item = await _service.RecusarItemAsync(
                escalaId,
                escalaItemId,
                dto?.MotivoRecusa,
                usuarioId,
                IsAdminUser(),
                usuarioPessoaId);
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

    [HttpPost("{escalaId}/itens/{escalaItemId}/presenca")]
    public async Task<ActionResult<EscalaItemDto>> RegistrarPresenca(int escalaId, int escalaItemId, [FromBody] RegistrarPresencaEscalaItemDto dto)
    {
        try
        {
            var item = await _service.RegistrarPresencaAsync(
                escalaId,
                escalaItemId,
                dto.Compareceu,
                dto.ObservacaoOperacional,
                GetUsuarioId(),
                IsAdminUser());
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

    [HttpPost("lembretes/processar")]
    public async Task<ActionResult<object>> ProcessarLembretes()
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem processar lembretes manualmente.");
        }

        var total = await _service.EnviarLembretesPendentesAsync();
        return Ok(new { totalEnviados = total });
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

    private async Task<int?> GetUsuarioPessoaIdAsync(int usuarioId)
    {
        if (usuarioId <= 0)
        {
            return null;
        }

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        return usuario?.PessoaId;
    }

    private string ResolverImagemUrl(string imagemUrl)
    {
        var url = imagemUrl.Trim();
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (string.IsNullOrWhiteSpace(_publicAppUrlSettings.ApiBaseUrl))
        {
            throw new InvalidOperationException("PublicAppUrl:ApiBaseUrl não configurado para expor a imagem da escala.");
        }

        return $"{_publicAppUrlSettings.ApiBaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    private static string BuildMensagemPlanejamentoMensal(PlanejamentoMensalEscalaDto planejamento)
    {
        var mes = planejamento.DataInicio.ToString("MM/yyyy");
        var equipe = planejamento.Voluntarios
            .SelectMany(v => v.Equipes)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "equipe";

        return $"Olá! Segue a escala mensal de {equipe} - {mes}. Qualquer ajuste, fale com a liderança.";
    }
}
