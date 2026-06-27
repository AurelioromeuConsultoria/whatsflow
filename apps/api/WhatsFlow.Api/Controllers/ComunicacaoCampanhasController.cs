using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComunicacaoCampanhasController : ControllerBase
{
    private readonly IComunicacaoCampanhaService _service;
    private readonly IComunicacaoEntregaService _entregaService;

    public ComunicacaoCampanhasController(IComunicacaoCampanhaService service, IComunicacaoEntregaService entregaService)
    {
        _service = service;
        _entregaService = entregaService;
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDto<ComunicacaoCampanhaResumoDto>>> GetPaged([FromQuery] ComunicacaoCampanhaPagedQueryDto query)
    {
        return Ok(await _service.GetPagedAsync(query));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ComunicacaoStatsDto>> GetStats()
    {
        return Ok(await _service.GetStatsAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComunicacaoCampanhaDetalheDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("{id}/entregas")]
    public async Task<ActionResult<IReadOnlyList<ComunicacaoEntregaResumoDto>>> GetEntregas(int id)
    {
        return Ok(await _entregaService.GetByCampanhaIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<ComunicacaoCampanhaDetalheDto>> Create([FromBody] CriarComunicacaoCampanhaDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ComunicacaoCampanhaDetalheDto>> Update(int id, [FromBody] AtualizarComunicacaoCampanhaDto dto)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, dto));
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }
}
