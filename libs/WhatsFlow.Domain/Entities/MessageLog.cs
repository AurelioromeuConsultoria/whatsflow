using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>
/// Histórico append-only das transições de status de uma mensagem (ComunicacaoEntrega).
/// Alimentado pelo Worker (envio) e pelo processamento de webhooks (delivered/read/failed).
/// </summary>
public class MessageLog : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public int ComunicacaoEntregaId { get; set; }
    public virtual ComunicacaoEntrega ComunicacaoEntrega { get; set; } = null!;

    [Required]
    public StatusComunicacaoEntrega Status { get; set; }

    [MaxLength(150)]
    public string? ProviderMessageId { get; set; }

    [MaxLength(60)]
    public string? ErrorCode { get; set; }

    [MaxLength(1000)]
    public string? Detalhe { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
