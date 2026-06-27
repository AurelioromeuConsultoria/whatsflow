using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Noticia : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [MaxLength(5000)]
    public string? Texto { get; set; }

    [Required]
    public DateTime Data { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? Imagem { get; set; }

    [Required]
    public int CategoriaNoticiaId { get; set; }
    public virtual CategoriaNoticia CategoriaNoticia { get; set; } = null!;

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}


