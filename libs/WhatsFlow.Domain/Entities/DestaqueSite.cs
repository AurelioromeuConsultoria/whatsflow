using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class DestaqueSite : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Texto { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? Imagem { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}


