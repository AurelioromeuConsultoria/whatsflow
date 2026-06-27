using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.API.Controllers;

/// <summary>
/// Onboarding self-service: cadastro público de nova organização + confirmação de e-mail.
/// Anônimo (logo, isento dos middlewares de gating/permissão, que só agem em requisições autenticadas).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SignupController : ControllerBase
{
    private readonly ISignupService _service;

    public SignupController(ISignupService service)
    {
        _service = service;
    }

    [HttpPost]
    [EnableRateLimiting("signup")]
    public async Task<ActionResult<SignupResultDto>> Signup([FromBody] SignupDto dto)
    {
        try
        {
            var resultado = await _service.SignupAsync(dto);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("confirmar")]
    public async Task<IActionResult> Confirmar([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Token ausente." });
        }

        var resultado = await _service.ConfirmarAsync(token);
        var cor = resultado.Confirmado ? "#1a5f7a" : "#e53e3e";
        var html = $"<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"UTF-8\"><title>Confirmação de e-mail</title></head>" +
                   $"<body style=\"font-family:sans-serif;text-align:center;padding:48px;color:#2d3748\">" +
                   $"<h1 style=\"color:{cor}\">{(resultado.Confirmado ? "Tudo certo!" : "Ops")}</h1>" +
                   $"<p>{resultado.Mensagem}</p></body></html>";
        return Content(html, "text/html");
    }
}
