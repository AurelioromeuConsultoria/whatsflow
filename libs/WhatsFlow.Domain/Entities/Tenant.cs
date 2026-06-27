using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Tenant
{
    public const int InitialTenantId = 1;
    public const string InitialTenantName = "WhatsFlow Demo";
    public const string InitialTenantSlug = "demo";

    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? NomeExibicao { get; set; }

    [Required]
    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? FaviconUrl { get; set; }

    [MaxLength(20)]
    public string? CorPrimaria { get; set; }

    [MaxLength(20)]
    public string? CorSecundaria { get; set; }

    // ----- Dados da empresa cliente (WhatsFlow) -----
    [MaxLength(30)]
    public string? Documento { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [Required]
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public int? PlanoId { get; set; }
    public virtual Plano? Plano { get; set; }

    /// <summary>Limite mensal de mensagens (0 = ilimitado / herdado do plano).</summary>
    public int LimiteMensalMensagens { get; set; }

    /// <summary>Limite de contatos (0 = ilimitado / herdado do plano).</summary>
    public int LimiteContatos { get; set; }

    [MaxLength(60)]
    public string FusoHorario { get; set; } = "America/Sao_Paulo";

    [Required]
    public bool IsRootTenant { get; set; }

    /// <summary>Mantido por compatibilidade com o legado; Status é a fonte de verdade.</summary>
    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    public virtual ICollection<TenantDomain> Domains { get; set; } = new List<TenantDomain>();
}

public enum TenantStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}
