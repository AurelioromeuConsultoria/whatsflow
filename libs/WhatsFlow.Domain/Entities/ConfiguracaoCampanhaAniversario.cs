using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class ConfiguracaoCampanhaAniversario : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    public bool Ativo { get; set; } = true;

    [MaxLength(500)]
    public string? ImagemUrl { get; set; }

    [Required]
    [MaxLength(4000)]
    public string MensagemTemplate { get; set; } = string.Empty;

    public TimeSpan HorarioEnvio { get; set; } = new(9, 0, 0);

    public DateTime DataAtualizacao { get; set; } = DateTime.Now;
}
