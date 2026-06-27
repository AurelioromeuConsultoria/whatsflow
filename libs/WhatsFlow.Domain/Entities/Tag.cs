using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>Etiqueta para segmentar contatos. Escopo por tenant.</summary>
public class Tag : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(80)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Cor em hex (#RRGGBB) para exibição no admin.</summary>
    [MaxLength(9)]
    public string? Cor { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ContatoTag> ContatoTags { get; set; } = new List<ContatoTag>();
}

/// <summary>Associação N:N entre Contato e Tag.</summary>
public class ContatoTag : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public int ContatoId { get; set; }
    public virtual Contato Contato { get; set; } = null!;

    public int TagId { get; set; }
    public virtual Tag Tag { get; set; } = null!;
}
