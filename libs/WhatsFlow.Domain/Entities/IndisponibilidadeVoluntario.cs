using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>Indica que o voluntário não está disponível em uma data específica.</summary>
public class IndisponibilidadeVoluntario : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int VoluntarioId { get; set; }
    public virtual Voluntario Voluntario { get; set; } = null!;

    [Required]
    public DateTime Data { get; set; }

    [MaxLength(500)]
    public string? Motivo { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
