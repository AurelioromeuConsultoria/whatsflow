using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>
/// Evento bruto recebido de um provider de WhatsApp. É salvo ANTES do processamento para
/// nunca perder eventos. O processamento (idempotente por ProviderMessageId) atualiza a fila/log.
/// </summary>
public class WebhookEvent : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public int? WhatsAppAccountId { get; set; }
    public virtual WhatsAppAccount? WhatsAppAccount { get; set; }

    [Required]
    public WhatsAppProviderType Provider { get; set; }

    /// <summary>Tipo de evento mapeado (ex: delivered, read, failed, inbound, optout).</summary>
    [MaxLength(60)]
    public string? EventType { get; set; }

    [MaxLength(150)]
    public string? ProviderMessageId { get; set; }

    /// <summary>Payload bruto recebido (jsonb).</summary>
    [Required]
    public string RawPayload { get; set; } = string.Empty;

    [Required]
    public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Recebido;

    [MaxLength(1000)]
    public string? Erro { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessadoEm { get; set; }
}

public enum WebhookEventStatus
{
    Recebido = 1,
    Processado = 2,
    Falhou = 3,
    Ignorado = 4
}
