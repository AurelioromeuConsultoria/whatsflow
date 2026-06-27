using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum GivingProvider
{
    Asaas = 1
}

public enum GivingProviderEnvironment
{
    Sandbox = 1,
    Production = 2
}

public class GivingProviderConfig : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public GivingProvider Provider { get; set; } = GivingProvider.Asaas;

    [Required]
    public GivingProviderEnvironment Environment { get; set; } = GivingProviderEnvironment.Sandbox;

    [MaxLength(4000)]
    public string? ApiKeyProtegida { get; set; }

    [MaxLength(40)]
    public string? ApiKeyUltimosDigitos { get; set; }

    [MaxLength(500)]
    public string? WebhookUrl { get; set; }

    [MaxLength(200)]
    public string? WebhookSecretProtegido { get; set; }

    public bool PixEnabled { get; set; } = true;

    public bool CreditCardEnabled { get; set; }

    public bool BoletoEnabled { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public DateTime? DataAtualizacao { get; set; }
}
