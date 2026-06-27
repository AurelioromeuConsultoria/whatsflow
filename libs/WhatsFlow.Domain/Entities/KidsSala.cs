using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class KidsSala : ITenantEntity
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    public int? CapacidadeMaxima { get; set; }

    public bool Ativo { get; set; } = true;

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    public virtual ICollection<KidsTurma> Turmas { get; set; } = new List<KidsTurma>();
}
