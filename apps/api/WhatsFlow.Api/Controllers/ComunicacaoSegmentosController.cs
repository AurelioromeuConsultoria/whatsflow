using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComunicacaoSegmentosController : ControllerBase
{
    private readonly IComunicacaoSegmentoService _service;

    public ComunicacaoSegmentosController(IComunicacaoSegmentoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComunicacaoSegmentoResumoDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComunicacaoSegmentoDetalheDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("estimativa")]
    public async Task<ActionResult<ComunicacaoEstimativaAudienciaDto>> GetEstimativa([FromQuery] string? publicoAlvo = null, [FromQuery] int? segmentoId = null)
    {
        return Ok(await _service.GetEstimativaAsync(publicoAlvo, segmentoId));
    }

    [HttpPost]
    public async Task<ActionResult<ComunicacaoSegmentoDetalheDto>> Create([FromBody] CriarComunicacaoSegmentoDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ComunicacaoSegmentoDetalheDto>> Update(int id, [FromBody] AtualizarComunicacaoSegmentoDto dto)
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
