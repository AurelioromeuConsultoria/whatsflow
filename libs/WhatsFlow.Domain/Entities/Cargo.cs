using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Cargo : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public virtual ICollection<Voluntario> Voluntarios { get; set; } = new List<Voluntario>();
}
