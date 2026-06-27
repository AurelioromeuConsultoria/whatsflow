namespace WhatsFlow.Application.Configuration;

public class BirthdayCampaignSchedulerSettings
{
    public const string SectionName = "BirthdayCampaignScheduler";

    public int BaseIntervalMinutes { get; set; } = 10;

    public int JitterSecondsMax { get; set; } = 20;

    public int MaxPessoasPorExecucao { get; set; } = 200;

    public int MaxTentativasPorPessoa { get; set; } = 3;

    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
}
