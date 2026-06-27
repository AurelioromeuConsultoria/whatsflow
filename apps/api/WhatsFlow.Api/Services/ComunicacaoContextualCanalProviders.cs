using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.API.Services;

public class ComunicacaoPushCanalProvider : IComunicacaoCanalProvider
{
    private readonly IKidsPushNotificationService _kidsPushNotificationService;
    private readonly FirebaseKidsPushOptions _options;
    private readonly ILogger<ComunicacaoPushCanalProvider> _logger;

    public ComunicacaoPushCanalProvider(
        IKidsPushNotificationService kidsPushNotificationService,
        IOptions<FirebaseKidsPushOptions> options,
        ILogger<ComunicacaoPushCanalProvider> logger)
    {
        _kidsPushNotificationService = kidsPushNotificationService;
        _options = options.Value;
        _logger = logger;
    }

    public CanalComunicacao Canal => CanalComunicacao.Push;
    public string Nome => "Push";

    public Task<ComunicacaoCanalDiagnostico> ValidarConfiguracaoAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.CredentialsPath))
        {
            return Task.FromResult(new ComunicacaoCanalDiagnostico { Configurado = true });
        }

        const string mensagem = "Configuração do canal Push incompleta. Campo obrigatório: Firebase:CredentialsPath.";
        _logger.LogWarning(mensagem);
        return Task.FromResult(new ComunicacaoCanalDiagnostico
        {
            Configurado = false,
            Mensagem = mensagem
        });
    }

    public async Task<ComunicacaoCanalEnvioResultado> EnviarAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken = default)
    {
        if (!entrega.DestinatarioPessoaId.HasValue || entrega.DestinatarioPessoaId.Value <= 0)
        {
            return new ComunicacaoCanalEnvioResultado
            {
                Sucesso = false,
                Mensagem = "Destinatário pessoa não resolvido para push."
            };
        }

        await _kidsPushNotificationService.SendToPessoasAsync(
            [entrega.DestinatarioPessoaId.Value],
            string.IsNullOrWhiteSpace(entrega.RemetenteResolvido) ? "Comunicacao AppIgreja" : entrega.RemetenteResolvido!,
            entrega.ConteudoFinal,
            new Dictionary<string, string>
            {
                ["origem"] = "COMUNICACAO_CENTRAL",
                ["entregaId"] = entrega.Id.ToString()
            });

        return new ComunicacaoCanalEnvioResultado { Sucesso = true };
    }
}
