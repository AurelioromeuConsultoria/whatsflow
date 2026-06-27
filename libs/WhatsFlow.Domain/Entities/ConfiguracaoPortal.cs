using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class ConfiguracaoPortal : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Tempo de transição do carrossel de destaques em segundos (padrão: 5 segundos)
    /// </summary>
    public int TempoTransicaoCarrossel { get; set; } = 5;

    public DateTime DataAtualizacao { get; set; } = DateTime.Now;
}
