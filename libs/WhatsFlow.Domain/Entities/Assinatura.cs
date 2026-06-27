using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum StatusAssinatura
{
    Trial = 1,
    Ativa = 2,
    Inadimplente = 3,
    Suspensa = 4,
    Cancelada = 5
}

public enum MetodoPagamentoAssinatura
{
    Pix = 1,
    Boleto = 2,
    Cartao = 3
}

/// <summary>
/// Assinatura da plataforma por tenant (a igreja paga a VerboPlus). É ITenantEntity
/// para que o tenant veja só a própria; leituras cross-tenant (admin plataforma e o
/// processamento de webhook) usam IgnoreTenantFilters de forma controlada.
/// </summary>
public class Assinatura : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int PlanoId { get; set; }
    public virtual Plano Plano { get; set; } = null!;

    [Required]
    public StatusAssinatura Status { get; set; } = StatusAssinatura.Trial;

    [Required]
    public CicloCobranca Ciclo { get; set; } = CicloCobranca.Mensal;

    [Required]
    public decimal Valor { get; set; }

    public MetodoPagamentoAssinatura? MetodoPagamento { get; set; }

    public DateTime? TrialFim { get; set; }

    /// <summary>Quando o aviso de "trial acabando" foi enviado (evita reenvio).</summary>
    public DateTime? TrialAvisoEnviadoEm { get; set; }

    public DateTime? VigenciaInicio { get; set; }
    public DateTime? ProximaCobranca { get; set; }
    public DateTime? InadimplenteDesde { get; set; }
    public DateTime? SuspensaEm { get; set; }
    public DateTime? CanceladaEm { get; set; }

    /// <summary>IDs no gateway (Asaas) — customer e subscription da conta da plataforma.</summary>
    [MaxLength(120)]
    public string? GatewayCustomerId { get; set; }

    [MaxLength(120)]
    public string? GatewaySubscriptionId { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
}
