using WhatsFlow.Application.Services;

namespace WhatsFlow.BackgroundWorker;

/// <summary>
/// ICurrentUserContext para o Worker: não existe usuário HTTP autenticado.
/// O tenant vem do <see cref="TenantScopeOverride"/> que cada job define antes
/// de processar. Ações de escrita ficam atribuídas ao "sistema" (UserId nulo)
/// na auditoria.
/// </summary>
public sealed class WorkerCurrentUserContext : ICurrentUserContext
{
    private readonly TenantScopeOverride _tenantScope;

    public WorkerCurrentUserContext(TenantScopeOverride tenantScope)
    {
        _tenantScope = tenantScope;
    }

    public int? UserId => null;
    public int? TenantId => _tenantScope.TenantId;
    public string? TenantSlug => _tenantScope.TenantSlug;
    public string? UserName => "sistema";
    public string? UserEmail => null;
    public string? IpAddress => null;
}
