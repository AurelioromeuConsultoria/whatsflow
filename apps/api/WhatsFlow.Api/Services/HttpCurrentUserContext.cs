using System.Security.Claims;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Services;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _http;
    private readonly TenantScopeOverride _tenantScopeOverride;

    public HttpCurrentUserContext(IHttpContextAccessor http, TenantScopeOverride tenantScopeOverride)
    {
        _http = http;
        _tenantScopeOverride = tenantScopeOverride;
    }

    public int? UserId
    {
        get
        {
            var ctx = _http.HttpContext;
            var raw = ctx?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) && id > 0 ? id : null;
        }
    }

    public int? TenantId
    {
        get
        {
            if (_tenantScopeOverride.TenantId.HasValue)
            {
                return _tenantScopeOverride.TenantId;
            }

            var overrideTenantId = GetTenantOverrideId();
            if (overrideTenantId.HasValue)
            {
                return overrideTenantId;
            }

            var raw = _http.HttpContext?.User?.FindFirstValue("TenantId");
            return int.TryParse(raw, out var id) && id > 0 ? id : null;
        }
    }

    public string? TenantSlug
    {
        get
        {
            if (_tenantScopeOverride.TenantSlug is not null)
            {
                return _tenantScopeOverride.TenantSlug;
            }

            var overrideTenantSlug = GetTenantOverrideSlug();
            if (!string.IsNullOrWhiteSpace(overrideTenantSlug))
            {
                return overrideTenantSlug;
            }

            return _http.HttpContext?.User?.FindFirstValue("TenantSlug");
        }
    }
    public string? UserName => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
    public string? UserEmail => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
    public string? IpAddress => _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    private int? GetTenantOverrideId()
    {
        var httpContext = _http.HttpContext;
        if (httpContext is null || !IsPlatformAdminRequest(httpContext.User))
        {
            return null;
        }

        var raw = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return int.TryParse(raw, out var id) && id > 0 ? id : null;
    }

    private string? GetTenantOverrideSlug()
    {
        var httpContext = _http.HttpContext;
        if (httpContext is null || !IsPlatformAdminRequest(httpContext.User))
        {
            return null;
        }

        return httpContext.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
    }

    private static bool IsPlatformAdminRequest(ClaimsPrincipal user)
    {
        return string.Equals(
            user.FindFirstValue("IsPlatformAdmin"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
