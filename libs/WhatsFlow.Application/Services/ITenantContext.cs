namespace WhatsFlow.Application.Services;

public interface ITenantContext
{
    int? TenantId { get; }
    string? TenantSlug { get; }
    bool IsResolved { get; }
}
