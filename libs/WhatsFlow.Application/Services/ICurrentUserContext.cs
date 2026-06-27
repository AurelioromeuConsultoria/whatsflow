namespace WhatsFlow.Application.Services;

public interface ICurrentUserContext
{
    int? UserId { get; }
    int? TenantId { get; }
    string? TenantSlug { get; }
    string? UserName { get; }
    string? UserEmail { get; }
    string? IpAddress { get; }
}
