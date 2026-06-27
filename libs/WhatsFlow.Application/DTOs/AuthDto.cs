using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string? TenantSlug { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UsuarioDto Usuario { get; set; } = null!;
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AlterarSenhaDto
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
}

public class RegistrarResponsavelDto
{
    public string TenantSlug { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantSlug { get; set; } = string.Empty;
    public string TenantNome { get; set; } = string.Empty;
    public string? TenantNomeExibicao { get; set; }
    public string? TenantLogoUrl { get; set; }
    public string? TenantFaviconUrl { get; set; }
    public string? TenantCorPrimaria { get; set; }
    public string? TenantCorSecundaria { get; set; }
    public bool IsRootTenant { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmailLogin { get; set; } = string.Empty;
    public TipoUsuario TipoUsuario { get; set; }
    public string TipoUsuarioDescricao { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? UltimoAcesso { get; set; }
    public int? PerfilAcessoId { get; set; }
    public string? PerfilAcessoNome { get; set; }
    public List<PermissaoPerfilDto> Permissoes { get; set; } = new();
}

public class CriarUsuarioDto
{
    public string? TenantSlug { get; set; }
    public int? PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string EmailLogin { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public TipoUsuario TipoUsuario { get; set; }
    public int? PerfilAcessoId { get; set; }
}

public class AtualizarUsuarioDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string EmailLogin { get; set; } = string.Empty;
    public TipoUsuario TipoUsuario { get; set; }
    public bool Ativo { get; set; }
    public int? PerfilAcessoId { get; set; }
}

public class TenantDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? NomeExibicao { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CorPrimaria { get; set; }
    public string? CorSecundaria { get; set; }
    public bool IsRootTenant { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDeactivate { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? DominioPrimario { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalAdministradores { get; set; }
    public int TotalPessoas { get; set; }
    public DateTime? UltimaAtividadeEm { get; set; }
    public string StatusOperacional { get; set; } = "Rascunho";
    public string StatusOperacionalChave { get; set; } = "rascunho";
    public string StatusOperacionalTom { get; set; } = "secondary";
    public int OnboardingPercentual { get; set; }
    public int OnboardingConcluidos { get; set; }
    public int OnboardingTotal { get; set; }
    public bool OnboardingIdentidadeOk { get; set; }
    public bool OnboardingBrandingOk { get; set; }
    public bool OnboardingDominioOk { get; set; }
    public bool OnboardingAdminOk { get; set; }
    public bool OnboardingBaseOperacionalOk { get; set; }
}

public class ProvisionTenantDto
{
    public string Nome { get; set; } = string.Empty;
    public string? NomeExibicao { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? DominioPrimario { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CorPrimaria { get; set; }
    public string? CorSecundaria { get; set; }
    public string AdminNome { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string? AdminTelefone { get; set; }
    public string? AdminWhatsApp { get; set; }
    public string AdminEmailLogin { get; set; } = string.Empty;
    public string AdminSenha { get; set; } = string.Empty;

    /// <summary>
    /// Quando false (signup self-service), tenant e usuário admin nascem inativos até a
    /// verificação de e-mail. Default true (provisionamento por platform admin).
    /// </summary>
    public bool AtivarImediatamente { get; set; } = true;
}

public class ProvisionTenantResultDto
{
    public TenantDto Tenant { get; set; } = null!;
    public int PerfilAcessoId { get; set; }
    public int PessoaId { get; set; }
    public int UsuarioId { get; set; }
}

public class AtualizarTenantStatusDto
{
    public bool Ativo { get; set; }
}

public class AtualizarTenantDto
{
    public string Nome { get; set; } = string.Empty;
    public string? NomeExibicao { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? DominioPrimario { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CorPrimaria { get; set; }
    public string? CorSecundaria { get; set; }
}

public class RegistrarContextoOperacionalTenantDto
{
    public int TenantOrigemId { get; set; }
    public string? TenantOrigemSlug { get; set; }
    public int TenantDestinoId { get; set; }
    public string? TenantDestinoSlug { get; set; }
    public string Acao { get; set; } = "TrocarTenantOperacional";
}
