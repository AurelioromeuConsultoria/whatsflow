using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardFinanceiroController : ControllerBase
{
    private readonly IDashboardFinanceiroService _service;

    public DashboardFinanceiroController(IDashboardFinanceiroService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardFinanceiroDto>> GetDashboard()
    {
        var dashboard = await _service.GetDashboardAsync();
        return Ok(dashboard);
    }
}
