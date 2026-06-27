using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class TenantDomain
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Domain { get; set; } = string.Empty;

    [Required]
    public bool IsPrimary { get; set; } = false;

    [Required]
    public bool Ativo { get; set; } = true;
}
