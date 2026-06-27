namespace WhatsFlow.Application.Interfaces;

public interface ICadastroMembroNotificationService
{
    Task<CadastroMembroNotificationResult> NotifySuccessAsync(CadastroMembroNotification notification, CancellationToken cancellationToken = default);
}

public class CadastroMembroNotification
{
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? WhatsApp { get; set; }
}

public class CadastroMembroNotificationResult
{
    public CadastroMembroCanalResultado WhatsApp { get; set; } = new();
    public CadastroMembroCanalResultado Email { get; set; } = new();

    public List<string> GetAvisos()
    {
        var avisos = new List<string>();

        if (WhatsApp.Status == "failed" && !string.IsNullOrWhiteSpace(WhatsApp.Mensagem))
            avisos.Add($"WhatsApp: {WhatsApp.Mensagem}");

        if (Email.Status == "failed" && !string.IsNullOrWhiteSpace(Email.Mensagem))
            avisos.Add($"E-mail: {Email.Mensagem}");

        return avisos;
    }
}

public class CadastroMembroCanalResultado
{
    public string Status { get; set; } = "skipped";
    public string? Mensagem { get; set; }
}
