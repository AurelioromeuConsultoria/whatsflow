namespace WhatsFlow.Application.Configuration;

/// <summary>
/// Parâmetros de billing configuráveis (seção "Billing" do appsettings / env vars).
/// </summary>
public class BillingSettings
{
    public const string SectionName = "Billing";

    /// <summary>Dias de teste grátis ao provisionar uma assinatura.</summary>
    public int TrialDias { get; set; } = 14;

    /// <summary>Dias de carência após inadimplência antes de suspender (acesso liberado com aviso nesse período).</summary>
    public int CarenciaDias { get; set; } = 7;

    /// <summary>Antecedência (dias) para enviar o aviso de "trial acabando".</summary>
    public int TrialAvisoDiasAntes { get; set; } = 3;
}
