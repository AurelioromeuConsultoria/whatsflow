using System.Security.Claims;
using WhatsFlow.Application.Services;

namespace WhatsFlow.API.Permissions;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IPermissionService permissionService)
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

        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (!path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        if (path.StartsWith("/api/auth") || path.StartsWith("/api/upload"))
        {
            await _next(context);
            return;
        }

        var recurso = PermissionResourceMap.GetResourceFromPath(path);
        if (string.IsNullOrWhiteSpace(recurso))
        {
            await _next(context);
            return;
        }

        var acao = PermissionResourceMap.GetActionFromMethod(context.Request.Method);
        if (acao == null)
        {
            await _next(context);
            return;
        }

        var isPlatformAdmin = string.Equals(
            context.User.FindFirstValue("IsPlatformAdmin"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (isPlatformAdmin)
        {
            await _next(context);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        if (!int.TryParse(userIdValue, out var userId) || userId <= 0)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var allowed = await permissionService.HasPermissionAsync(userId, recurso, acao);
        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
