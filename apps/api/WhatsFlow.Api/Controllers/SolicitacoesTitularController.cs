using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Controllers;

/// <summary>
/// Requisições de titulares de dados (LGPD, Art. 18). Permissões herdam do recurso
/// "pessoas" via PermissionResourceMap (GET=view, POST/PUT=edit).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SolicitacoesTitularController : ControllerBase
{
    private readonly ISolicitacaoTitularService _service;

    public SolicitacoesTitularController(ISolicitacaoTitularService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SolicitacaoTitularDto>>> Listar([FromQuery] StatusSolicitacaoTitular? status)
    {
        var itens = await _service.ListarAsync(status);
        return Ok(itens);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SolicitacaoTitularDto>> Obter(int id)
    {
        var item = await _service.ObterAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<SolicitacaoTitularDto>> Criar([FromBody] CriarSolicitacaoTitularDto dto)
    {
        var criada = await _service.CriarAsync(dto);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    [HttpPut("{id}/atender")]
    public async Task<ActionResult<SolicitacaoTitularDto>> Atender(int id)
    {
        var item = await _service.AtenderAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPut("{id}/concluir")]
    public async Task<ActionResult<SolicitacaoTitularDto>> Concluir(int id, [FromBody] ConcluirSolicitacaoTitularDto dto)
    {
        var item = await _service.ConcluirAsync(id, dto.Observacao);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPut("{id}/recusar")]
    public async Task<ActionResult<SolicitacaoTitularDto>> Recusar(int id, [FromBody] RecusarSolicitacaoTitularDto dto)
    {
        var item = await _service.RecusarAsync(id, dto.Motivo);
        return item == null ? NotFound() : Ok(item);
    }
}
