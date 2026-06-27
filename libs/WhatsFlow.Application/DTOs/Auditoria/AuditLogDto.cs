namespace WhatsFlow.Application.DTOs.Auditoria;

public class AuditLogDto
{
    public int Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public string? IpAddress { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ChangesJson { get; init; }
}

