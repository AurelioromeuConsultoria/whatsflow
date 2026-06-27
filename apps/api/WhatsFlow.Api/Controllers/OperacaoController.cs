using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Security.Claims;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/operacao")]
[Authorize]
public class OperacaoController : ControllerBase
{
    private readonly ISchedulerExecutionMonitor _schedulerExecutionMonitor;

    public OperacaoController(ISchedulerExecutionMonitor schedulerExecutionMonitor)
    {
        _schedulerExecutionMonitor = schedulerExecutionMonitor;
    }

    [HttpGet("schedulers")]
    public ActionResult<IEnumerable<SchedulerExecutionStatusDto>> GetSchedulers()
    {
        if (!IsAdminUser())
        {
            return StatusCode(403, "Apenas administradores podem visualizar status operacional.");
        }

        return Ok(_schedulerExecutionMonitor.GetAll());
    }

    private bool IsAdminUser()
    {
        var tipoUsuarioId = User.FindFirstValue("TipoUsuarioId");
        return tipoUsuarioId == ((int)TipoUsuario.Admin).ToString() ||
               tipoUsuarioId == ((int)TipoUsuario.Ambos).ToString();
    }
}
