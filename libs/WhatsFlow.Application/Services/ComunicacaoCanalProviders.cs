using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services.WhatsApp;
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

    /// <summary>Id da mensagem no provider (quando aplicável), para rastrear status via webhook.</summary>
    public string? ProviderMessageId { get; init; }

    /// <summary>Código de erro do provider (quando aplicável).</summary>
    public string? ErrorCode { get; init; }
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
    private readonly IWhatsAppProviderResolver _providerResolver;
    private readonly IWhatsAppAccountRepository _accountRepository;
    private readonly ILogger<ComunicacaoWhatsAppCanalProvider> _logger;

    public ComunicacaoWhatsAppCanalProvider(
        IWhatsAppProviderResolver providerResolver,
        IWhatsAppAccountRepository accountRepository,
        ILogger<ComunicacaoWhatsAppCanalProvider> logger)
    {
        _providerResolver = providerResolver;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public CanalComunicacao Canal => CanalComunicacao.WhatsApp;
    public string Nome => "WhatsApp";

    /// <summary>Conta WhatsApp ativa do tenant atual (a 1ª ativa). Null = canal não configurado.</summary>
    private async Task<WhatsAppAccount?> ObterContaAtivaAsync()
    {
        var contas = await _accountRepository.GetAllAsync();
        return contas.FirstOrDefault(c => c.Status == WhatsAppAccountStatus.Ativa);
    }

    public async Task<ComunicacaoCanalDiagnostico> ValidarConfiguracaoAsync(CancellationToken cancellationToken = default)
    {
        var conta = await ObterContaAtivaAsync();
        if (conta != null)
        {
            return new ComunicacaoCanalDiagnostico
            {
                Configurado = true,
                Mensagem = $"Conta ativa: {conta.Nome} ({conta.Provider})."
            };
        }

        const string mensagem = "Nenhuma conta WhatsApp ativa configurada para este tenant.";
        _logger.LogWarning(mensagem);
        return new ComunicacaoCanalDiagnostico { Configurado = false, Mensagem = mensagem };
    }

    public async Task<ComunicacaoCanalEnvioResultado> EnviarAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entrega.DestinoResolvido))
        {
            return new ComunicacaoCanalEnvioResultado { Sucesso = false, Mensagem = "Destino de WhatsApp não resolvido." };
        }

        var conta = await ObterContaAtivaAsync();
        if (conta == null)
        {
            return new ComunicacaoCanalEnvioResultado { Sucesso = false, Mensagem = "Nenhuma conta WhatsApp ativa." };
        }

        var provider = _providerResolver.ResolveFor(conta);

        // Quando a entrega referencia um template com ProviderTemplateId, usa o fluxo de template;
        // caso contrário envia o conteúdo já renderizado como texto.
        ProviderSendResult result;
        if (!string.IsNullOrWhiteSpace(entrega.Template?.ProviderTemplateId))
        {
            result = await provider.SendTemplateMessageAsync(
                conta, entrega.DestinoResolvido, entrega.Template!.ProviderTemplateId!,
                new Dictionary<string, string>(), cancellationToken);
        }
        else
        {
            result = await provider.SendTextMessageAsync(
                conta, entrega.DestinoResolvido, entrega.ConteudoFinal, cancellationToken);
        }

        if (result.Success)
        {
            // best-effort: 4D persiste ProviderMessageId via MarcarComoEnviadaAsync
            entrega.ProviderMessageId = result.ProviderMessageId;
            return new ComunicacaoCanalEnvioResultado
            {
                Sucesso = true,
                ProviderMessageId = result.ProviderMessageId
            };
        }

        return new ComunicacaoCanalEnvioResultado
        {
            Sucesso = false,
            Mensagem = $"{provider.Type}: {result.ErrorMessage} (Code: {result.ErrorCode})",
            ErrorCode = result.ErrorCode
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
    private readonly INotificacaoUsuarioRepository _notificacaoUsuarioRepository;

    public ComunicacaoNotificacaoInternaCanalProvider(
        INotificacaoUsuarioRepository notificacaoUsuarioRepository)
    {
        _notificacaoUsuarioRepository = notificacaoUsuarioRepository;
    }

    public CanalComunicacao Canal => CanalComunicacao.NotificacaoInterna;
    public string Nome => "Notificação interna";

    public Task<ComunicacaoCanalDiagnostico> ValidarConfiguracaoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ComunicacaoCanalDiagnostico { Configurado = true });
    }

    public Task<ComunicacaoCanalEnvioResultado> EnviarAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken = default)
    {
        // TODO(WhatsFlow Etapa 4C): redefinir notificação interna no modelo Contato.
        // Antes, a entrega resolvia um Usuario a partir de Pessoa; com o fim de Pessoa,
        // não há mais vínculo Contato->Usuario, então este canal fica desabilitado por ora.
        _ = _notificacaoUsuarioRepository; // mantém a dependência registrada para uso futuro
        return Task.FromResult(new ComunicacaoCanalEnvioResultado
        {
            Sucesso = false,
            Mensagem = "Canal de notificação interna indisponível no modelo Contato (Etapa 4C)."
        });
    }
}
