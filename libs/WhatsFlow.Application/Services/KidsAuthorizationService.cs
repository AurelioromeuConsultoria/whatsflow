using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsAuthorizationService
{
    Task EnsureOperadorAsync();
    Task EnsureLiderAsync();
    Task<KidsAuthorizationContext> GetCurrentContextAsync();
}

public sealed class KidsAuthorizationContext
{
    public required int UsuarioId { get; init; }
    public required int PessoaId { get; init; }
    public required int TenantId { get; init; }
    public required TipoUsuario TipoUsuario { get; init; }
    public required IReadOnlyCollection<PerfilPessoa> PerfisAtivos { get; init; }

    public bool IsAdministrativo => TipoUsuario is TipoUsuario.Admin or TipoUsuario.Ambos;
    public bool IsLiderKids => IsAdministrativo || PerfisAtivos.Contains(PerfilPessoa.Admin) || PerfisAtivos.Contains(PerfilPessoa.Lider);
    public bool IsOperadorKids => IsLiderKids || PerfisAtivos.Contains(PerfilPessoa.Kids);
}

public class KidsAuthorizationService : IKidsAuthorizationService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPessoaPerfilRepository _pessoaPerfilRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IPermissionService _permissionService;

    public KidsAuthorizationService(
        IUsuarioRepository usuarioRepository,
        IPessoaPerfilRepository pessoaPerfilRepository,
        ICurrentUserContext currentUserContext,
        IPermissionService permissionService)
    {
        _usuarioRepository = usuarioRepository;
        _pessoaPerfilRepository = pessoaPerfilRepository;
        _currentUserContext = currentUserContext;
        _permissionService = permissionService;
    }

    public async Task EnsureOperadorAsync()
    {
        var context = await GetCurrentContextAsync();
        var hasViewPermission = await _permissionService.HasPermissionAsync(context.UsuarioId, "kids", "view");
        if (!context.IsOperadorKids && !hasViewPermission)
        {
            throw new UnauthorizedAccessException("Esta operação de Kids exige perfil de operador, líder ou administrativo.");
        }
    }

    public async Task EnsureLiderAsync()
    {
        var context = await GetCurrentContextAsync();
        var hasEditPermission = await _permissionService.HasPermissionAsync(context.UsuarioId, "kids", "edit");
        if (!context.IsLiderKids && !hasEditPermission)
        {
            throw new UnauthorizedAccessException("Esta operação de Kids exige perfil de liderança ou administrativo.");
        }
    }

    public async Task<KidsAuthorizationContext> GetCurrentContextAsync()
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuário atual não identificado.");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(_currentUserContext.UserId.Value);
        if (usuario == null || !usuario.Ativo)
        {
            throw new UnauthorizedAccessException("Usuário atual não identificado.");
        }

        var tenantId = _currentUserContext.TenantId ?? usuario.TenantId;
        if (tenantId <= 0 || usuario.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Contexto do tenant atual é inválido para operações de Kids.");
        }

        var agora = DateTime.UtcNow;
        var perfisAtivos = (await _pessoaPerfilRepository.GetPerfisPorPessoaAsync(usuario.PessoaId))
            .Where(p => p.DataInicio <= agora && (!p.DataFim.HasValue || p.DataFim.Value >= agora))
            .Select(p => p.Perfil)
            .Distinct()
            .ToList();

        return new KidsAuthorizationContext
        {
            UsuarioId = usuario.Id,
            PessoaId = usuario.PessoaId,
            TenantId = tenantId,
            TipoUsuario = usuario.TipoUsuario,
            PerfisAtivos = perfisAtivos
        };
    }
}
