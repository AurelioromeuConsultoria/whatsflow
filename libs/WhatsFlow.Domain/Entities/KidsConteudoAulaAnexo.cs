using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class KidsConteudoAulaAnexo : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int ConteudoAulaId { get; set; }
    public virtual KidsConteudoAula ConteudoAula { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; } = string.Empty; // Pdf, Imagem, Link

    [Required]
    [MaxLength(200)]
    public string NomeExibicao { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? StoragePath { get; set; }

    [MaxLength(120)]
    public string? MimeType { get; set; }

    public long? TamanhoBytes { get; set; }

    public int Ordem { get; set; }

    [Required]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
