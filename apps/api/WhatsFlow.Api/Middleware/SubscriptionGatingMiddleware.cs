using System.Security.Claims;
using System.Text.Json;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Middleware;

/// <summary>
/// Bloqueia requisições de tenants com assinatura suspensa/cancelada (HTTP 402),
/// para forçar a regularização. Roda após autenticação (tenant já resolvido) e antes
/// do PermissionMiddleware. Fail-open: tenant sem assinatura não é bloqueado.
/// </summary>
public class SubscriptionGatingMiddleware
{
    private readonly RequestDelegate _next;

    // Rotas que precisam funcionar mesmo com assinatura bloqueada.
    private static readonly string[] RotasIsentas =
    {
        "/api/auth",
        "/api/upload",
        "/api/webhooks",
        "/api/billing"
    };

    public SubscriptionGatingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ITenantContext tenantContext, IBillingService billingService)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (!path.StartsWith("/api/") || RotasIsentas.Any(r => path.StartsWith(r)))
        {
            await _next(context);
            return;
        }

        // Platform admin nunca é bloqueado.
        if (string.Equals(context.User.FindFirstValue("IsPlatformAdmin"), "true", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var tenantId = tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            await _next(context);
            return;
        }

        if (await billingService.TenantBloqueadoAsync(tenantId.Value))
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "assinatura_suspensa",
                message = "A assinatura desta organização está suspensa. Regularize o pagamento para continuar."
            }));
            return;
        }

        await _next(context);
    }
}
