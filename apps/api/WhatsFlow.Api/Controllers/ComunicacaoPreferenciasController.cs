using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComunicacaoPreferenciasController : ControllerBase
{
    private readonly IComunicacaoPreferenciaService _service;

    public ComunicacaoPreferenciasController(IComunicacaoPreferenciaService service)
    {
        _service = service;
    }

    [HttpGet("contato/{contatoId:int}")]
    public async Task<ActionResult<IReadOnlyList<ComunicacaoPreferenciaResumoDto>>> GetByContatoId(int contatoId)
    {
        return Ok(await _service.GetByContatoIdAsync(contatoId));
    }

    [HttpPut("contato/{contatoId:int}/canal/{canal}")]
    public async Task<ActionResult<ComunicacaoPreferenciaResumoDto>> Upsert(int contatoId, CanalComunicacao canal, [FromBody] AtualizarComunicacaoPreferenciaDto dto)
    {
        return Ok(await _service.UpsertAsync(contatoId, canal, dto));
    }
}
