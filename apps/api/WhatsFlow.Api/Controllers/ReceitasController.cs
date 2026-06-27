using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceitasController : ControllerBase
{
    private readonly IReceitaService _service;

    public ReceitasController(IReceitaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReceitaDto>>> GetAll([FromQuery] int? pessoaId = null)
    {
        var items = await _service.GetAllAsync(pessoaId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReceitaDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ReceitaDto>> Create(CriarReceitaDto dto)
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

    [HttpPost("lote")]
    public async Task<ActionResult<IEnumerable<ReceitaDto>>> LancarLote(LancarContribuicoesLoteDto dto)
    {
        try
        {
            var criadas = await _service.LancarContribuicoesEmLoteAsync(dto);
            return Ok(criadas);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("relatorio/contribuicoes")]
    public async Task<ActionResult<RelatorioContribuicoesDto>> GetRelatorioContribuicoes(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim,
        [FromQuery] int? categoriaId = null)
    {
        try
        {
            var relatorio = await _service.GetRelatorioContribuicoesAsync(dataInicio, dataFim, categoriaId);
            return Ok(relatorio);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("informe/{pessoaId}")]
    public async Task<ActionResult<InformeContribuicoesDto>> GetInformeAnual(int pessoaId, [FromQuery] int ano)
    {
        try
        {
            var informe = await _service.GetInformeAnualAsync(pessoaId, ano);
            return Ok(informe);
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

    [HttpPut("{id}")]
    public async Task<ActionResult<ReceitaDto>> Update(int id, AtualizarReceitaDto dto)
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/gerar-proxima")]
    public async Task<ActionResult<ReceitaDto>> GerarProxima(int id)
    {
        try
        {
            var result = await _service.GerarProximaRecorrenciaAsync(id);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
