namespace WhatsFlow.Application.Configuration;

public class PublicAppUrlSettings
{
    public const string SectionName = "PublicAppUrl";

    public string ApiBaseUrl { get; set; } = string.Empty;
}
