using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/pessoas/aniversarios-campanha")]
[Authorize]
public class PessoasAniversariosCampanhaController : ControllerBase
{
    private readonly ICampanhaAniversarioService _service;

    public PessoasAniversariosCampanhaController(ICampanhaAniversarioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<CampanhaAniversarioConfiguracaoDto>> Get([FromQuery] CampanhaAniversarioHistoricoFiltroDto filtros)
    {
        var configuracao = await _service.GetAsync(filtros);
        return Ok(configuracao);
    }

    [HttpPut]
    public async Task<ActionResult<CampanhaAniversarioConfiguracaoDto>> Update([FromBody] AtualizarCampanhaAniversarioDto dto)
    {
        var configuracao = await _service.UpdateAsync(dto);
        return Ok(configuracao);
    }

    [HttpPost("teste")]
    public async Task<ActionResult<CampanhaAniversarioEnvioTesteResultadoDto>> EnviarTeste(
        [FromBody] EnviarTesteCampanhaAniversarioDto dto,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.EnviarTesteAsync(dto, cancellationToken);
        if (!resultado.Sucesso)
        {
            return BadRequest(resultado);
        }

        return Ok(resultado);
    }

    [HttpPost("historico/{envioId:int}/reenviar")]
    public async Task<ActionResult<CampanhaAniversarioReenvioResultadoDto>> Reenviar(
        int envioId,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.ReenviarAsync(envioId, cancellationToken);
        if (!resultado.Sucesso)
        {
            return BadRequest(resultado);
        }

        return Ok(resultado);
    }
}
