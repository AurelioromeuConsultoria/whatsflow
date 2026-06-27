using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken(RefreshTokenDto dto)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioDto>> GetMe()
    {
        try
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var usuario = await _authService.GetUsuarioLogadoAsync(usuarioId);
            return Ok(usuario);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("alterar-senha")]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaDto dto)
    {
        try
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            await _authService.AlterarSenhaAsync(usuarioId, dto);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
}






