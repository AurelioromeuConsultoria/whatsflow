using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

public interface ITenantManagementService
{
    Task<IEnumerable<TenantDto>> GetAllAsync();
    Task<TenantDto?> GetByIdAsync(int id);
    Task<ProvisionTenantResultDto> ProvisionAsync(ProvisionTenantDto dto);
    Task<TenantDto> UpdateAsync(int id, AtualizarTenantDto dto);
    Task<TenantDto> UpdateStatusAsync(int id, AtualizarTenantStatusDto dto);
    Task DeleteAsync(int id);
}
