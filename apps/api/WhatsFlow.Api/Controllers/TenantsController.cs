using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Auditoria;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantManagementService _tenantManagementService;
    private readonly IAuditLogService _auditLogService;

    public TenantsController(ITenantManagementService tenantManagementService, IAuditLogService auditLogService)
    {
        _tenantManagementService = tenantManagementService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAll()
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem listar tenants.");
        }

        var tenants = await _tenantManagementService.GetAllAsync();
        return Ok(tenants);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TenantDto>> GetById(int id)
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem visualizar tenants.");
        }

        var tenant = await _tenantManagementService.GetByIdAsync(id);
        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpPost]
    public async Task<ActionResult<ProvisionTenantResultDto>> Provision(ProvisionTenantDto dto)
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem provisionar tenants.");
        }

        try
        {
            var result = await _tenantManagementService.ProvisionAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Tenant.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TenantDto>> Update(int id, AtualizarTenantDto dto)
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem editar tenants.");
        }

        try
        {
            var result = await _tenantManagementService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TenantDto>> UpdateStatus(int id, AtualizarTenantStatusDto dto)
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem alterar o status de tenants.");
        }

        try
        {
            var result = await _tenantManagementService.UpdateStatusAsync(id, dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem excluir tenants.");
        }

        try
        {
            await _tenantManagementService.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("contexto-operacional")]
    public async Task<IActionResult> RegistrarContextoOperacional(RegistrarContextoOperacionalTenantDto dto)
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem trocar o contexto operacional.");
        }

        if (dto.TenantOrigemId <= 0 || dto.TenantDestinoId <= 0)
        {
            return BadRequest("Tenant de origem e destino são obrigatórios.");
        }

        var tenantDestino = await _tenantManagementService.GetByIdAsync(dto.TenantDestinoId);
        if (tenantDestino is null)
        {
            return NotFound("Tenant de destino não encontrado.");
        }

        await _auditLogService.RecordAsync(
            "Tenant",
            dto.TenantDestinoId.ToString(),
            dto.Acao,
            new
            {
                dto.TenantOrigemId,
                dto.TenantOrigemSlug,
                dto.TenantDestinoId,
                TenantDestinoSlug = dto.TenantDestinoSlug ?? tenantDestino.Slug,
                TenantDestinoNome = tenantDestino.NomeExibicao ?? tenantDestino.Nome,
                UsuarioPlatformAdmin = User.Identity?.Name
            });

        return Accepted();
    }

    [HttpGet("{id:int}/auditoria-administrativa")]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetAuditoriaAdministrativa(int id)
    {
        if (!IsPlatformAdminUser())
        {
            return StatusCode(403, "Apenas administradores da plataforma podem visualizar a trilha administrativa.");
        }

        var tenant = await _tenantManagementService.GetByIdAsync(id);
        if (tenant is null)
        {
            return NotFound();
        }

        var page = await _auditLogService.GetPagedAsync(new AuditLogPagedQueryDto
        {
            EntityName = "Tenant",
            EntityId = id.ToString(),
            Page = 1,
            PageSize = 10
        });

        return Ok(page);
    }

    private bool IsPlatformAdminUser()
    {
        return string.Equals(
            User.FindFirst("IsPlatformAdmin")?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
