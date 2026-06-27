namespace WhatsFlow.Application.Configuration;

/// <summary>
/// Configurações da Evolution API
/// </summary>
public class EvolutionApiSettings
{
    /// <summary>
    /// URL base da Evolution API (ex: https://evolution.kingdombr.com.br)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Chave de autenticação (API Key)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Nome da instância configurada na Evolution API
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// Timeout em segundos para requisições HTTP (padrão: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Número máximo de tentativas em caso de erro (padrão: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay em segundos entre tentativas (padrão: 5)
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Código do país padrão para formatação de telefone (padrão: 55 para Brasil)
    /// </summary>
    public string CodigoPaisPadrao { get; set; } = "55";

    /// <summary>
    /// Delay padrão (em ms) para envio de mensagens (algumas instalações exigem este campo).
    /// </summary>
    public int DelayMs { get; set; } = 0;
}
