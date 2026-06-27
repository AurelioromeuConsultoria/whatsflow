using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class CategoriaMidia : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    // Relacionamento com galerias
    public virtual ICollection<GaleriaFoto> Galerias { get; set; } = new List<GaleriaFoto>();
}



