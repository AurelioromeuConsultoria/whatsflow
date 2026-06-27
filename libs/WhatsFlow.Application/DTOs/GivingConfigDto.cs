using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class GivingProviderConfigDto
{
    public int Id { get; set; }
    public GivingProvider Provider { get; set; }
    public string ProviderDescricao { get; set; } = string.Empty;
    public GivingProviderEnvironment Environment { get; set; }
    public string EnvironmentDescricao { get; set; } = string.Empty;
    public bool Configurado { get; set; }
    public string? ApiKeyUltimosDigitos { get; set; }
    public string? WebhookUrl { get; set; }
    public bool PixEnabled { get; set; }
    public bool CreditCardEnabled { get; set; }
    public bool BoletoEnabled { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class SalvarGivingProviderConfigDto
{
    public GivingProvider Provider { get; set; } = GivingProvider.Asaas;

    public GivingProviderEnvironment Environment { get; set; } = GivingProviderEnvironment.Sandbox;

    [MaxLength(400)]
    public string? ApiKey { get; set; }

    [MaxLength(500)]
    public string? WebhookUrl { get; set; }

    [MaxLength(200)]
    public string? WebhookSecret { get; set; }

    public bool PixEnabled { get; set; } = true;

    public bool CreditCardEnabled { get; set; }

    public bool BoletoEnabled { get; set; }

    public bool Ativo { get; set; }
}
