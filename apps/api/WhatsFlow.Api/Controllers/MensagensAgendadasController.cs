using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.MensagensAgendadas;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MensagensAgendadasController : ControllerBase
{
    private readonly IMensagemAgendadaService _mensagemService;

    public MensagensAgendadasController(IMensagemAgendadaService mensagemService)
    {
        _mensagemService = mensagemService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MensagemAgendadaDto>>> GetAll()
    {
        var mensagens = await _mensagemService.GetAllAsync();
        return Ok(mensagens);
    }

    /// <summary>
    /// Lista mensagens com paginação e filtros (server-side).
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDto<MensagemAgendadaDto>>> GetPaged([FromQuery] MensagemAgendadaPagedQueryDto query)
    {
        var result = await _mensagemService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<MensagemAgendadaStatsDto>> GetStats()
    {
        var result = await _mensagemService.GetStatsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MensagemAgendadaDto>> GetById(int id)
    {
        var mensagem = await _mensagemService.GetByIdAsync(id);
        if (mensagem == null)
            return NotFound();

        return Ok(mensagem);
    }

    [HttpGet("prontas-para-envio")]
    public async Task<ActionResult<IEnumerable<MensagemAgendadaDto>>> GetProntasParaEnvio()
    {
        var mensagens = await _mensagemService.GetMensagensProntasParaEnvioAsync();
        return Ok(mensagens);
    }

    [HttpGet("contato/{contatoId}")]
    public async Task<ActionResult<IEnumerable<MensagemAgendadaDto>>> GetPorContato(int contatoId)
    {
        var mensagens = await _mensagemService.GetMensagensPorContatoAsync(contatoId);
        return Ok(mensagens);
    }

    [HttpPost("{id}/marcar-pronta")]
    public async Task<IActionResult> MarcarComoPronta(int id)
    {
        try
        {
            await _mensagemService.MarcarComoProntaParaEnvioAsync(id);
            return Ok();
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

    [HttpPost("{id}/marcar-enviada")]
    public async Task<IActionResult> MarcarComoEnviada(int id)
    {
        try
        {
            await _mensagemService.MarcarComoEnviadaAsync(id);
            return Ok();
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

    [HttpPost("{id}/marcar-erro")]
    public async Task<IActionResult> MarcarComoErro(int id, [FromBody] string erro)
    {
        try
        {
            await _mensagemService.MarcarComoErroAsync(id, erro);
            return Ok();
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
}

