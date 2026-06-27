using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>
/// Registro de eventos de webhook de billing recebidos do gateway (Asaas plataforma).
/// Garante idempotência (o Asaas reenvia eventos) e serve de trilha. É nível plataforma
/// — NÃO é ITenantEntity, pois o webhook chega sem contexto de tenant.
/// </summary>
public class EventoWebhookBilling
{
    public int Id { get; set; }

    /// <summary>Identificador único do evento no gateway, usado para idempotência.</summary>
    [MaxLength(120)]
    public string? GatewayEventId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Evento { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? GatewayPaymentId { get; set; }

    [MaxLength(120)]
    public string? GatewaySubscriptionId { get; set; }

    /// <summary>Tenant resolvido a partir da assinatura (quando identificado).</summary>
    public int? TenantId { get; set; }

    [Required]
    public bool Processado { get; set; }

    [MaxLength(500)]
    public string? Observacao { get; set; }

    public DateTime RecebidoEm { get; set; } = DateTime.UtcNow;

    public string? PayloadJson { get; set; }
}
