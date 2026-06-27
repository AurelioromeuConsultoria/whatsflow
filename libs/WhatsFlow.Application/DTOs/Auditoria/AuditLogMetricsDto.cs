namespace WhatsFlow.Application.DTOs.Auditoria;

public class AuditLogMetricsDto
{
    public int TotalLogs { get; init; }
    public int CriticalActions { get; init; }
    public int FailureActions { get; init; }
    public int DistinctUsers { get; init; }
    public string? TopUserLabel { get; init; }
    public int TopUserCount { get; init; }
    public string? TopEntityName { get; init; }
    public int TopEntityCount { get; init; }
    public string? TopActionName { get; init; }
    public int TopActionCount { get; init; }
}
