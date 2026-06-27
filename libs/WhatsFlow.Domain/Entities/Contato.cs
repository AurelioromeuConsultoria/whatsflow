using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Contato : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string WhatsApp { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [Required]
    public bool Membro { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Mensagem { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}





