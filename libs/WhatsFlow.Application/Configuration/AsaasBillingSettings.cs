namespace WhatsFlow.Application.Configuration;

/// <summary>
/// Credenciais da conta Asaas DA PLATAFORMA (VerboPlus), usada para cobrar as igrejas.
/// É distinta do Asaas de doações (por-tenant). Valores reais vêm de env vars
/// (ex.: Billing__Asaas__ApiKey) — nunca commitados.
/// </summary>
public class AsaasBillingSettings
{
    public const string SectionName = "Billing:Asaas";

    public string? ApiKey { get; set; }

    /// <summary>Token usado para validar os webhooks de billing (header asaas-access-token).</summary>
    public string? WebhookToken { get; set; }

    /// <summary>"Sandbox" (padrão) ou "Production".</summary>
    public string Environment { get; set; } = "Sandbox";

    public bool IsProduction =>
        string.Equals(Environment, "Production", StringComparison.OrdinalIgnoreCase);

    public string BaseUrl => IsProduction
        ? "https://api.asaas.com/v3/"
        : "https://sandbox.asaas.com/api/v3/";
}
