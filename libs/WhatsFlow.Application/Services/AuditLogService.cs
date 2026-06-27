using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Auditoria;

namespace WhatsFlow.Application.Services;

public interface IAuditLogService
{
    Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogPagedQueryDto query);
    Task<AuditLogMetricsDto> GetMetricsAsync(AuditLogPagedQueryDto query);
    Task RecordAsync(string entityName, string entityId, string action, object? changes = null);
}
