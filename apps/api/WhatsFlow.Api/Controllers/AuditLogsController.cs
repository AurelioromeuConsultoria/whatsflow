using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Auditoria;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service)
    {
        _service = service;
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetPaged([FromQuery] AuditLogPagedQueryDto query)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem visualizar auditoria.");
        }

        var result = await _service.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<AuditLogMetricsDto>> GetMetrics([FromQuery] AuditLogPagedQueryDto query)
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem visualizar auditoria.");
        }

        var result = await _service.GetMetricsAsync(query);
        return Ok(result);
    }

    private bool IsAdminUser()
    {
        var tipoUsuarioId = User.FindFirstValue("TipoUsuarioId");
        return tipoUsuarioId == ((int)TipoUsuario.Admin).ToString() ||
               tipoUsuarioId == ((int)TipoUsuario.Ambos).ToString();
    }
}
