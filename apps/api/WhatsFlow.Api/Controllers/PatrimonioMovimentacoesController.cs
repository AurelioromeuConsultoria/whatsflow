using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/patrimonio/{patrimonioItemId:int}/movimentacoes")]
[Authorize]
public class PatrimonioMovimentacoesController : ControllerBase
{
    private readonly IPatrimonioMovimentacaoService _service;

    public PatrimonioMovimentacoesController(IPatrimonioMovimentacaoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatrimonioMovimentacaoDto>>> GetAll(int patrimonioItemId)
    {
        var items = await _service.GetByPatrimonioIdAsync(patrimonioItemId);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<PatrimonioMovimentacaoDto>> Create(int patrimonioItemId, CriarPatrimonioMovimentacaoDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(patrimonioItemId, dto);
            return Ok(created);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
