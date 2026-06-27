namespace WhatsFlow.Application.DTOs.Auditoria;

public class AuditLogPagedQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
    public string? EntityName { get; init; }
    public string? EntityId { get; init; }
    public string? Action { get; init; }
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }

    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
