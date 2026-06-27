namespace WhatsFlow.Application.Configuration;

/// <summary>
/// Configurações do MessageSchedulerService (worker de mensagens agendadas).
/// </summary>
public class MessageSchedulerSettings
{
    /// <summary>Seção no appsettings (ex: "MessageScheduler").</summary>
    public const string SectionName = "MessageScheduler";

    /// <summary>
    /// Intervalo base entre execuções, em minutos (ex: 5 ou 10).
    /// </summary>
    public int BaseIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Jitter máximo em segundos adicionado ao intervalo (ex: 0–20).
    /// Reduz sincronismo fixo entre múltiplas instâncias.
    /// </summary>
    public int JitterSecondsMax { get; set; } = 20;

    /// <summary>
    /// Quantidade máxima de mensagens reservadas por execução (batch).
    /// </summary>
    public int BatchSizeReserva { get; set; } = 50;

    /// <summary>
    /// Número máximo de tentativas de envio antes de marcar a entrega como Falhou.
    /// </summary>
    public int MaxTentativas { get; set; } = 3;

    /// <summary>
    /// Backoff base entre tentativas, em minutos. O atraso efetivo é RetryBackoffMinutes * Tentativas.
    /// </summary>
    public int RetryBackoffMinutes { get; set; } = 5;

    /// <summary>
    /// Atraso entre envios consecutivos dentro do batch, em milissegundos (rate limit simples).
    /// 0 = sem atraso. Para rate limit distribuído, usar Redis no futuro.
    /// </summary>
    public int DelayBetweenSendsMs { get; set; } = 0;
}
