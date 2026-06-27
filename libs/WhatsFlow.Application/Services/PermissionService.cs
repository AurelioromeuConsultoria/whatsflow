using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int usuarioId, string recurso, string acao);
}

public class PermissionService : IPermissionService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public PermissionService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<bool> HasPermissionAsync(int usuarioId, string recurso, string acao)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario == null || !usuario.Ativo)
            return false;

        var permissoes = usuario.PerfilAcesso?.Permissoes;
        if (permissoes == null)
        {
            return usuario.TipoUsuario == TipoUsuario.Admin;
        }

        var perm = permissoes.FirstOrDefault(p =>
            p.TenantId == usuario.TenantId &&
            p.Recurso.ToLower() == recurso.ToLower());
        if (perm == null) return false;

        return acao switch
        {
            "view" => perm.PodeVer,
            "edit" => perm.PodeEditar,
            "delete" => perm.PodeExcluir,
            _ => false
        };
    }
}
