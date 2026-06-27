using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum StatusFatura
{
    Pendente = 1,
    Paga = 2,
    Vencida = 3,
    Falhou = 4,
    Cancelada = 5
}

/// <summary>
/// Cobrança individual de uma assinatura (histórico de faturas). ITenantEntity.
/// </summary>
public class Fatura : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int AssinaturaId { get; set; }
    public virtual Assinatura Assinatura { get; set; } = null!;

    [Required]
    public decimal Valor { get; set; }

    [Required]
    public StatusFatura Status { get; set; } = StatusFatura.Pendente;

    [Required]
    public DateTime Vencimento { get; set; }

    public DateTime? PagaEm { get; set; }

    [MaxLength(120)]
    public string? GatewayPaymentId { get; set; }

    [MaxLength(500)]
    public string? LinkPagamento { get; set; }

    [MaxLength(2000)]
    public string? PixCopiaECola { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
