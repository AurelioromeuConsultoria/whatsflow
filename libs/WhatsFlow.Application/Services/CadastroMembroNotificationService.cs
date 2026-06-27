using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.Application.Services;

public class CadastroMembroNotificationService : ICadastroMembroNotificationService
{
    private readonly IEvolutionApiService _evolutionApiService;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<CadastroMembroNotificationService> _logger;

    public CadastroMembroNotificationService(
        IEvolutionApiService evolutionApiService,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        ILogger<CadastroMembroNotificationService> logger)
    {
        _evolutionApiService = evolutionApiService;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<CadastroMembroNotificationResult> NotifySuccessAsync(CadastroMembroNotification notification, CancellationToken cancellationToken = default)
    {
        var result = new CadastroMembroNotificationResult();

        if (!string.IsNullOrWhiteSpace(notification.WhatsApp))
        {
            try
            {
                var response = await _evolutionApiService.EnviarMensagemTextoAsync(
                    notification.WhatsApp,
                    BuildWhatsAppMessage(notification.Nome),
                    cancellationToken);

                if (!response.Sucesso)
                {
                    result.WhatsApp = new CadastroMembroCanalResultado
                    {
                        Status = "failed",
                        Mensagem = response.MensagemErro ?? "Falha ao enviar mensagem de WhatsApp."
                    };

                    _logger.LogWarning(
                        "Falha ao enviar WhatsApp de cadastro para {WhatsApp}: {Erro}",
                        notification.WhatsApp,
                        response.MensagemErro);
                }
                else
                {
                    result.WhatsApp = new CadastroMembroCanalResultado
                    {
                        Status = "sent",
                        Mensagem = "Mensagem enviada com sucesso."
                    };
                }
            }
            catch (Exception ex)
            {
                result.WhatsApp = new CadastroMembroCanalResultado
                {
                    Status = "failed",
                    Mensagem = "Erro inesperado ao enviar mensagem de WhatsApp."
                };

                _logger.LogWarning(
                    ex,
                    "Erro inesperado ao enviar WhatsApp de cadastro para {WhatsApp}",
                    notification.WhatsApp);
            }
        }
        else
        {
            result.WhatsApp = new CadastroMembroCanalResultado
            {
                Status = "skipped",
                Mensagem = "Nenhum WhatsApp informado."
            };
        }

        if (!string.IsNullOrWhiteSpace(notification.Email))
        {
            if (!_emailSettings.Enabled)
            {
                result.Email = new CadastroMembroCanalResultado
                {
                    Status = "skipped",
                    Mensagem = "Envio de e-mail está desabilitado no momento."
                };
            }
            else
            {
                try
                {
                    await _emailService.SendAsync(new EmailMessage
                    {
                        To = notification.Email,
                        Subject = "Cadastro recebido com sucesso",
                        HtmlBody = BuildHtmlEmail(notification.Nome),
                        TextBody = BuildTextEmail(notification.Nome)
                    }, cancellationToken);

                    result.Email = new CadastroMembroCanalResultado
                    {
                        Status = "sent",
                        Mensagem = "E-mail enviado com sucesso."
                    };
                }
                catch (Exception ex)
                {
                    result.Email = new CadastroMembroCanalResultado
                    {
                        Status = "failed",
                        Mensagem = "Falha ao enviar e-mail de confirmação."
                    };

                    _logger.LogWarning(
                        ex,
                        "Erro inesperado ao enviar e-mail de cadastro para {Email}",
                        notification.Email);
                }
            }
        }
        else
        {
            result.Email = new CadastroMembroCanalResultado
            {
                Status = "skipped",
                Mensagem = "Nenhum e-mail informado."
            };
        }

        return result;
    }

    private static string BuildWhatsAppMessage(string nome)
    {
        return $"Olá, {nome}!\n\nSeu cadastro foi efetuado com sucesso no sistema da Kingdom!\n\nObrigado!";
    }

    private static string BuildHtmlEmail(string nome)
    {
        return $"""
                <html>
                  <body style="font-family: Arial, sans-serif; color: #1f2937;">
                    <h2>Cadastro recebido com sucesso</h2>
                    <p>Olá, {System.Net.WebUtility.HtmlEncode(nome)}!</p>
                    <p>Recebemos o seu cadastro e os seus dados já foram registrados com sucesso.</p>
                    <p>Se precisarmos de alguma informação complementar, entraremos em contato.</p>
                  </body>
                </html>
                """;
    }

    private static string BuildTextEmail(string nome)
    {
        return $"Olá, {nome}! Recebemos o seu cadastro com sucesso. Se precisarmos de alguma informação complementar, entraremos em contato.";
    }
}
