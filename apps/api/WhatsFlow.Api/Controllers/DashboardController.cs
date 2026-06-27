using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet("estatisticas")]
    public async Task<ActionResult<DashboardDto>> GetEstatisticas()
    {
        var estatisticas = await _service.GetEstatisticasAsync();
        return Ok(estatisticas);
    }

    [HttpGet("series")]
    public async Task<ActionResult<List<DashboardSeriePontoDto>>> GetSerie([FromQuery] int meses = 6)
    {
        var serie = await _service.GetSerieAsync(meses);
        return Ok(serie);
    }
}
