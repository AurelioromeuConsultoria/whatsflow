using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracoesMensagensController : ControllerBase
{
    private readonly IConfiguracaoMensagemService _configuracaoService;

    public ConfiguracoesMensagensController(IConfiguracaoMensagemService configuracaoService)
    {
        _configuracaoService = configuracaoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConfiguracaoMensagemDto>>> GetAll()
    {
        var configuracoes = await _configuracaoService.GetAllAsync();
        return Ok(configuracoes);
    }

    [HttpGet("ativas")]
    public async Task<ActionResult<IEnumerable<ConfiguracaoMensagemDto>>> GetAtivas()
    {
        var configuracoes = await _configuracaoService.GetAtivasAsync();
        return Ok(configuracoes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConfiguracaoMensagemDto>> GetById(int id)
    {
        var configuracao = await _configuracaoService.GetByIdAsync(id);
        if (configuracao == null)
            return NotFound();

        return Ok(configuracao);
    }

    [HttpPost]
    public async Task<ActionResult<ConfiguracaoMensagemDto>> Create(CriarConfiguracaoMensagemDto dto)
    {
        try
        {
            var configuracao = await _configuracaoService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = configuracao.Id }, configuracao);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ConfiguracaoMensagemDto>> Update(int id, AtualizarConfiguracaoMensagemDto dto)
    {
        try
        {
            var configuracao = await _configuracaoService.UpdateAsync(id, dto);
            return Ok(configuracao);
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
            await _configuracaoService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

