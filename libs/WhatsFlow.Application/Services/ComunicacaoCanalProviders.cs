using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public class ComunicacaoCanalDiagnostico
{
    public bool Configurado { get; init; }
    public string? Mensagem { get; init; }
}

public class ComunicacaoCanalEnvioResultado
{
    public bool Sucesso { get; init; }
    public string? Mensagem { get; init; }
}

public interface IComunicacaoCanalProvider
{
    CanalComunicacao Canal { get; }
    string Nome { get; }
    Task<ComunicacaoCanalDiagnostico> ValidarConfiguracaoAsync(CancellationToken cancellationToken = default);
    Task<ComunicacaoCanalEnvioResultado> EnviarAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken = default);
}

public class ComunicacaoWhatsAppCanalProvider : IComunicacaoCanalProvider
{
    private readonly IEvolutionApiService _evolutionApiService;
    private readonly EvolutionApiSettings _settings;
    private readonly ILogger<ComunicacaoWhatsAppCanalProvider> _logger;

    public ComunicacaoWhatsAppCanalProvider(
        IEvolutionApiService evolutionApiService,
        IOptions<EvolutionApiSettings> settings,
        ILogger<ComunicacaoWhatsAppCanalProvider> logger)
    {
        _evolutionApiService = evolutionApiService;
        _settings = settings.Value;
        _logger = logger;
    }

    public CanalComunicacao Canal => CanalComunicacao.WhatsApp;
    public string Nome => "WhatsApp";

    public Task<ComunicacaoCanalDiagnostico> ValidarConfiguracaoAsync(CancellationToken cancellationToken = default)
    {
        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl)) faltantes.Add("EvolutionApi:BaseUrl");
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)) faltantes.Add("EvolutionApi:ApiKey");
        if (string.IsNullOrWhiteSpace(_settings.InstanceName)) faltantes.Add("EvolutionApi:InstanceName");

        if (faltantes.Count == 0)
        {
            return Task.FromResult(new ComunicacaoCanalDiagnostico { Configurado = true });
        }

        var mensagem = $"Configuração do canal WhatsApp incompleta. Campos obrigatórios: {string.Join(", ", faltantes)}.";
        _logger.LogWarning(mensagem);
        return Task.FromResult(new ComunicacaoCanalDiagnostico
        {
            Configurado = false,
            Mensagem = mensagem
        });
    }

    public async Task<ComunicacaoCanalEnvioResultado> EnviarAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entrega.DestinoResolvido))
        {
            return new ComunicacaoCanalEnvioResultado
            {
                Sucesso = false,
                Mensagem = "Destino de WhatsApp não resolvido."
            };
        }

        var response = string.IsNullOrWhiteSpace(entrega.MidiaUrl)
            ? await _evolutionApiService.EnviarMensagemTextoAsync(entrega.DestinoResolvido, entrega.ConteudoFinal, cancellationToken)
            : await _evolutionApiService.EnviarMensagemImagemAsync(entrega.DestinoResolvido, entrega.MidiaUrl, entrega.ConteudoFinal, cancellationToken);
        if (response.Sucesso)
        {
            return new ComunicacaoCanalEnvioResultado { Sucesso = true };
        }

        return new ComunicacaoCanalEnvioResultado
        {
            Sucesso = false,
            Mensagem = $"Evolution API: {response.MensagemErro} (Status: {response.StatusCode})"
        };
    }
}

public class ComunicacaoEmailCanalProvider : IComunicacaoCanalProvider
{
    private readonly IEmailService _emailService;
    private readonly EmailSettings _settings;
    private readonly ILogger<ComunicacaoEmailCanalProvider> _logger;

    public ComunicacaoEmailCanalProvider(
        IEmailService emailService,
        IOptions<EmailSettings> settings,
        ILogger<ComunicacaoEmailCanalProvider> logger)
    {
        _emailService = emailService;
        _settings = settings.Value;
        _logger = logger;
    }

    public CanalComunicacao Canal => CanalComunicacao.Email;
    public string Nome => "E-mail";

    public Task<ComunicacaoCanalDiagnostico> ValidarConfiguracaoAsync(CancellationToken cancellationToken = default)
    {
        var faltantes = new List<string>();

        if (!_settings.Enabled) faltantes.Add("Email:Enabled");
        if (string.IsNullOrWhiteSpace(_settings.Host)) faltantes.Add("Email:Host");
        if (string.IsNullOrWhiteSpace(_settings.FromAddress)) faltantes.Add("Email:FromAddress");
        if (!string.IsNullOrWhiteSpace(_settings.Username) && string.IsNullOrWhiteSpace(_settings.Password))
            faltantes.Add("Email:Password");

        if (faltantes.Count == 0)
        {
            return Task.FromResult(new ComunicacaoCanalDiagnostico { Configurado = true });
        }

        var mensagem = $"Configuração do canal E-mail incompleta. Campos obrigatórios: {string.Join(", ", faltantes)}.";
        _logger.LogWarning(mensagem);
        return Task.FromResult(new ComunicacaoCanalDiagnostico
        {
            Configurado = false,
            Mensagem = mensagem
        });
    }

    public async Task<ComunicacaoCanalEnvioResultado> EnviarAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entrega.DestinoResolvido))
        {
            return new ComunicacaoCanalEnvioResultado
            {
                Sucesso = false,
                Mensagem = "Destino de e-mail não resolvido."
            };
        }

        var subject = string.IsNullOrWhiteSpace(entrega.RemetenteResolvido) ? "Comunicacao AppIgreja" : entrega.RemetenteResolvido;
        await _emailService.SendAsync(new EmailMessage
        {
            To = entrega.DestinoResolvido,
            Subject = subject,
            TextBody = entrega.ConteudoFinal,
            HtmlBody = string.IsNullOrWhiteSpace(entrega.ConteudoHtmlFinal) ? null : entrega.ConteudoHtmlFinal
        }, cancellationToken);

        return new ComunicacaoCanalEnvioResultado { Sucesso = true };
    }
}

public class ComunicacaoNotificacaoInternaCanalProvider : IComunicacaoCanalProvider
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly INotificacaoUsuarioRepository _notificacaoUsuarioRepository;

    public ComunicacaoNotificacaoInternaCanalProvider(
        IUsuarioRepository usuarioRepository,
        INotificacaoUsuarioRepository notificacaoUsuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
        _notificacaoUsuarioRepository = notificacaoUsuarioRepository;
    }

    public CanalComunicacao Canal => CanalComunicacao.NotificacaoInterna;
    public string Nome => "Notificação interna";

    public Task<ComunicacaoCanalDiagnostico> ValidarConfiguracaoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ComunicacaoCanalDiagnostico { Configurado = true });
    }

    public async Task<ComunicacaoCanalEnvioResultado> EnviarAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken = default)
    {
        if (!entrega.DestinatarioPessoaId.HasValue || entrega.DestinatarioPessoaId.Value <= 0)
        {
            return new ComunicacaoCanalEnvioResultado
            {
                Sucesso = false,
                Mensagem = "Destinatário pessoa não resolvido para notificação interna."
            };
        }

        var usuario = await _usuarioRepository.GetByPessoaIdAsync(entrega.DestinatarioPessoaId.Value);
        if (usuario == null || !usuario.Ativo)
        {
            return new ComunicacaoCanalEnvioResultado
            {
                Sucesso = false,
                Mensagem = $"Nenhum usuário ativo encontrado para a pessoa {entrega.DestinatarioPessoaId.Value}."
            };
        }

        await _notificacaoUsuarioRepository.CreateAsync(new NotificacaoUsuario
        {
            UsuarioId = usuario.Id,
            Tipo = TipoNotificacaoUsuario.Geral,
            Titulo = string.IsNullOrWhiteSpace(entrega.RemetenteResolvido) ? "Comunicacao AppIgreja" : entrega.RemetenteResolvido!,
            Mensagem = entrega.ConteudoFinal,
            DataCriacao = DateTime.Now
        });

        return new ComunicacaoCanalEnvioResultado { Sucesso = true };
    }
}
