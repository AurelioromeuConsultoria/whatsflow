using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacoesController : ControllerBase
{
    private readonly INotificacaoUsuarioService _service;
    private readonly ICurrentUserContext _currentUser;

    public NotificacoesController(INotificacaoUsuarioService service, ICurrentUserContext currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificacaoUsuarioDto>>> GetMinhas([FromQuery] bool somenteNaoLidas = false, [FromQuery] int? limit = null)
    {
        var usuarioId = _currentUser.UserId;
        if (!usuarioId.HasValue) return Unauthorized();

        var items = await _service.GetMinhasAsync(usuarioId.Value, somenteNaoLidas, limit);
        return Ok(items);
    }

    [HttpGet("nao-lidas/count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var usuarioId = _currentUser.UserId;
        if (!usuarioId.HasValue) return Unauthorized();

        var count = await _service.GetUnreadCountAsync(usuarioId.Value);
        return Ok(new { count });
    }

    [HttpPost("{id}/marcar-lida")]
    public async Task<ActionResult<NotificacaoUsuarioDto>> MarcarComoLida(int id)
    {
        var usuarioId = _currentUser.UserId;
        if (!usuarioId.HasValue) return Unauthorized();

        try
        {
            var item = await _service.MarcarComoLidaAsync(id, usuarioId.Value);
            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("marcar-todas-lidas")]
    public async Task<ActionResult<object>> MarcarTodasComoLidas()
    {
        var usuarioId = _currentUser.UserId;
        if (!usuarioId.HasValue) return Unauthorized();

        var total = await _service.MarcarTodasComoLidasAsync(usuarioId.Value);
        return Ok(new { total });
    }
}
