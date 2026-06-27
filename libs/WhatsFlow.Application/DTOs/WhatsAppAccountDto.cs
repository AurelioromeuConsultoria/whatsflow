using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class WhatsAppAccountDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public WhatsAppProviderType Provider { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? BusinessAccountId { get; set; }
    /// <summary>Indica se há um token de acesso configurado (o valor nunca é retornado).</summary>
    public bool PossuiAccessToken { get; set; }
    /// <summary>Indica se há um webhook secret configurado (o valor nunca é retornado).</summary>
    public bool PossuiWebhookSecret { get; set; }
    public WhatsAppAccountStatus Status { get; set; }
    public string? ConfiguracoesJson { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class CriarWhatsAppAccountDto
{
    public string Nome { get; set; } = string.Empty;
    public WhatsAppProviderType Provider { get; set; } = WhatsAppProviderType.Fake;
    public string? PhoneNumberId { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public WhatsAppAccountStatus Status { get; set; } = WhatsAppAccountStatus.Ativa;
    public string? ConfiguracoesJson { get; set; }
}

public class AtualizarWhatsAppAccountDto
{
    public string Nome { get; set; } = string.Empty;
    public WhatsAppProviderType Provider { get; set; } = WhatsAppProviderType.Fake;
    public string? PhoneNumberId { get; set; }
    public string? BusinessAccountId { get; set; }
    /// <summary>Quando null, o token atual é mantido. Quando string vazia, o token é removido.</summary>
    public string? AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public WhatsAppAccountStatus Status { get; set; } = WhatsAppAccountStatus.Ativa;
    public string? ConfiguracoesJson { get; set; }
}
