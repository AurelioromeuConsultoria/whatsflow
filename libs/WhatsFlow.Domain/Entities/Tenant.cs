using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Tenant
{
    public const int InitialTenantId = 1;
    public const string InitialTenantName = "Mang Guarulhos";
    public const string InitialTenantSlug = "mang-guarulhos";

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

    [Required]
    public bool IsRootTenant { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TenantDomain> Domains { get; set; } = new List<TenantDomain>();
}
