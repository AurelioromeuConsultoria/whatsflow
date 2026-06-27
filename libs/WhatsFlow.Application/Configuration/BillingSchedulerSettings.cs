namespace WhatsFlow.Application.Configuration;

public class BillingSchedulerSettings
{
    public const string SectionName = "BillingScheduler";

    public bool Enabled { get; set; } = true;

    /// <summary>Intervalo base entre execuções do ciclo de billing (padrão: 6h).</summary>
    public int BaseIntervalMinutes { get; set; } = 360;

    public int JitterSecondsMax { get; set; } = 60;
}
