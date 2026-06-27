using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Usuario : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string EmailLogin { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string SenhaHash { get; set; } = string.Empty;

    [Required]
    public TipoUsuario TipoUsuario { get; set; } = TipoUsuario.Portal;

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    public bool IsPlatformAdmin { get; set; }

    public int? PerfilAcessoId { get; set; }
    public virtual PerfilAcesso? PerfilAcesso { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? UltimoAcesso { get; set; }

    /// <summary>Tentativas de login malsucedidas consecutivas (lockout anti força-bruta).</summary>
    public int TentativasLoginFalhas { get; set; }

    /// <summary>Se preenchido e no futuro, o login está temporariamente bloqueado.</summary>
    public DateTime? BloqueadoAte { get; set; }

    public virtual ICollection<NotificacaoUsuario> Notificacoes { get; set; } = new List<NotificacaoUsuario>();
}
