using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelatoriosFinanceirosController : ControllerBase
{
    private readonly IRelatorioFinanceiroService _service;

    public RelatoriosFinanceirosController(IRelatorioFinanceiroService service)
    {
        _service = service;
    }

    [HttpGet("fluxo-caixa")]
    public async Task<ActionResult<RelatorioFluxoCaixaDto>> GetFluxoCaixa(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim)
    {
        var relatorio = await _service.GetFluxoCaixaAsync(dataInicio, dataFim);
        return Ok(relatorio);
    }

    [HttpGet("por-categoria")]
    public async Task<ActionResult<RelatorioPorCategoriaCompletoDto>> GetRelatorioPorCategoria(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim)
    {
        var relatorio = await _service.GetRelatorioPorCategoriaAsync(dataInicio, dataFim);
        return Ok(relatorio);
    }

    [HttpGet("por-centro-custo")]
    public async Task<ActionResult<IEnumerable<RelatorioPorCentroCustoDto>>> GetRelatorioPorCentroCusto(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim)
    {
        var relatorio = await _service.GetRelatorioPorCentroCustoAsync(dataInicio, dataFim);
        return Ok(relatorio);
    }

    [HttpGet("por-projeto")]
    public async Task<ActionResult<IEnumerable<RelatorioPorProjetoDto>>> GetRelatorioPorProjeto(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim)
    {
        var relatorio = await _service.GetRelatorioPorProjetoAsync(dataInicio, dataFim);
        return Ok(relatorio);
    }

    [HttpGet("dre")]
    public async Task<ActionResult<DreDto>> GetDre([FromQuery] int ano)
    {
        if (ano == 0) ano = DateTime.Now.Year;
        var result = await _service.GetDreAsync(ano);
        return Ok(result);
    }
}
