using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrcamentoCategoriasController : ControllerBase
{
    private readonly IOrcamentoCategoriaService _service;

    public OrcamentoCategoriasController(IOrcamentoCategoriaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrcamentoCategoriaDto>>> GetByAno([FromQuery] int ano)
    {
        if (ano == 0) ano = DateTime.Now.Year;
        var items = await _service.GetByAnoAsync(ano);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<OrcamentoCategoriaDto>> Save(SalvarOrcamentoCategoriaDto dto)
    {
        try
        {
            var result = await _service.SaveAsync(dto);
            return Ok(result);
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

    [HttpGet("comparacao")]
    public async Task<ActionResult<OrcamentoComparacaoDto>> GetComparacao([FromQuery] int ano)
    {
        if (ano == 0) ano = DateTime.Now.Year;
        var result = await _service.GetComparacaoAsync(ano);
        return Ok(result);
    }
}
