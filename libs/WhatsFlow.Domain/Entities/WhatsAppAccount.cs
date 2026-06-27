using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>
/// Configuração da conta/provedor de WhatsApp por tenant. O envio real é desacoplado por provider
/// (ver IWhatsAppProvider). Segredos (AccessToken/WebhookSecret) devem ser protegidos em repouso.
/// </summary>
public class WhatsAppAccount : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(80)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public WhatsAppProviderType Provider { get; set; } = WhatsAppProviderType.Fake;

    [MaxLength(120)]
    public string? PhoneNumberId { get; set; }

    [MaxLength(120)]
    public string? BusinessAccountId { get; set; }

    /// <summary>Token de acesso do provider, protegido (cifrado em repouso).</summary>
    [MaxLength(2000)]
    public string? AccessTokenProtegido { get; set; }

    [MaxLength(200)]
    public string? WebhookSecret { get; set; }

    [Required]
    public WhatsAppAccountStatus Status { get; set; } = WhatsAppAccountStatus.Ativa;

    /// <summary>Configurações extras específicas do provider (JSON).</summary>
    public string? ConfiguracoesJson { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}

public enum WhatsAppProviderType
{
    Fake = 0,
    OfficialCloudApi = 1,
    EvolutionApi = 2,
    Twilio = 3,
    Zenvia = 4,
    Other = 99
}

public enum WhatsAppAccountStatus
{
    Ativa = 1,
    Inativa = 2,
    ErroConfiguracao = 3
}
