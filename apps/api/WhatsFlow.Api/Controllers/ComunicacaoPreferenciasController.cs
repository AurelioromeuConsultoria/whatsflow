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

    [HttpGet("pessoa/{pessoaId:int}")]
    public async Task<ActionResult<IReadOnlyList<ComunicacaoPreferenciaResumoDto>>> GetByPessoaId(int pessoaId)
    {
        return Ok(await _service.GetByPessoaIdAsync(pessoaId));
    }

    [HttpPut("pessoa/{pessoaId:int}/canal/{canal}")]
    public async Task<ActionResult<ComunicacaoPreferenciaResumoDto>> Upsert(int pessoaId, CanalComunicacao canal, [FromBody] AtualizarComunicacaoPreferenciaDto dto)
    {
        return Ok(await _service.UpsertAsync(pessoaId, canal, dto));
    }
}
