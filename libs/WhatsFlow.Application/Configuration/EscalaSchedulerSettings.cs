namespace WhatsFlow.Application.Configuration;

public class EscalaSchedulerSettings
{
    public const string SectionName = "EscalaScheduler";

    public bool Enabled { get; set; } = true;
    public int BaseIntervalMinutes { get; set; } = 60;
    public int JitterSecondsMax { get; set; } = 30;

    // Janela de geração relativa à data atual.
    public int DiasJanelaInicio { get; set; } = 0;
    public int DiasJanelaFim { get; set; } = 60;
    public bool EnviarLembretesAutomaticos { get; set; } = true;
}
