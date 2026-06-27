using System.Collections.Concurrent;

namespace WhatsFlow.Application.Services;

public interface ISchedulerExecutionMonitor
{
    void RecordSuccess(string schedulerName, DateTime startedAtUtc, DateTime finishedAtUtc, string? details = null);
    void RecordFailure(string schedulerName, DateTime startedAtUtc, DateTime finishedAtUtc, string error, string? details = null);
    IReadOnlyCollection<SchedulerExecutionStatusDto> GetAll();
}

public class SchedulerExecutionMonitor : ISchedulerExecutionMonitor
{
    private readonly ConcurrentDictionary<string, SchedulerExecutionStatusDto> _statuses = new(StringComparer.OrdinalIgnoreCase);

    public void RecordSuccess(string schedulerName, DateTime startedAtUtc, DateTime finishedAtUtc, string? details = null)
    {
        _statuses[schedulerName] = new SchedulerExecutionStatusDto
        {
            SchedulerName = schedulerName,
            Status = "Healthy",
            LastStartedAtUtc = startedAtUtc,
            LastFinishedAtUtc = finishedAtUtc,
            LastDurationMs = (finishedAtUtc - startedAtUtc).TotalMilliseconds,
            Details = details
        };
    }

    public void RecordFailure(string schedulerName, DateTime startedAtUtc, DateTime finishedAtUtc, string error, string? details = null)
    {
        _statuses[schedulerName] = new SchedulerExecutionStatusDto
        {
            SchedulerName = schedulerName,
            Status = "Unhealthy",
            LastStartedAtUtc = startedAtUtc,
            LastFinishedAtUtc = finishedAtUtc,
            LastDurationMs = (finishedAtUtc - startedAtUtc).TotalMilliseconds,
            Error = error,
            Details = details
        };
    }

    public IReadOnlyCollection<SchedulerExecutionStatusDto> GetAll()
    {
        return _statuses.Values
            .OrderBy(x => x.SchedulerName)
            .ToList();
    }
}

public class SchedulerExecutionStatusDto
{
    public string SchedulerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public DateTime? LastStartedAtUtc { get; set; }
    public DateTime? LastFinishedAtUtc { get; set; }
    public double? LastDurationMs { get; set; }
    public string? Error { get; set; }
    public string? Details { get; set; }
}
