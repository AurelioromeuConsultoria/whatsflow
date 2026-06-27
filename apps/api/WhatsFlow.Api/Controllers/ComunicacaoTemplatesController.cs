using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComunicacaoTemplatesController : ControllerBase
{
    private readonly IComunicacaoTemplateService _service;

    public ComunicacaoTemplatesController(IComunicacaoTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComunicacaoTemplateResumoDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComunicacaoTemplateDetalheDto>> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ComunicacaoTemplateDetalheDto>> Create([FromBody] CriarComunicacaoTemplateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ComunicacaoTemplateDetalheDto>> Update(int id, [FromBody] AtualizarComunicacaoTemplateDto dto)
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
