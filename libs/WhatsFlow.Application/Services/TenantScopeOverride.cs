namespace WhatsFlow.Application.Services;

public sealed class TenantScopeOverride : ITenantContext
{
    public int? TenantId { get; private set; }
    public string? TenantSlug { get; private set; }
    public bool IsResolved => TenantId.HasValue;

    public void SetTenant(int tenantId, string? tenantSlug = null)
    {
        TenantId = tenantId > 0 ? tenantId : null;
        TenantSlug = tenantSlug;
    }

    public void Clear()
    {
        TenantId = null;
        TenantSlug = null;
    }
}
