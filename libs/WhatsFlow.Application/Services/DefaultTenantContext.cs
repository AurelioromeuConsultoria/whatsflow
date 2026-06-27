namespace WhatsFlow.Application.Services;

public sealed class DefaultTenantContext : ITenantContext
{
    public int? TenantId => Domain.Entities.Tenant.InitialTenantId;
    public string? TenantSlug => Domain.Entities.Tenant.InitialTenantSlug;
    public bool IsResolved => true;
}
