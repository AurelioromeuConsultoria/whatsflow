using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoProcessamentoService
{
    Task<int> ProcessarPendentesAsync(int limit, CancellationToken cancellationToken = default);
    Task<bool> ProcessarEntregaAsync(int entregaId, CancellationToken cancellationToken = default);
}

public class ComunicacaoProcessamentoService : IComunicacaoProcessamentoService
{
    private readonly IComunicacaoEntregaService _entregaService;
    private readonly IComunicacaoEntregaRepository _entregaRepository;
    private readonly IReadOnlyDictionary<CanalComunicacao, IComunicacaoCanalProvider> _providers;
    private readonly ILogger<ComunicacaoProcessamentoService> _logger;
    private readonly MessageSchedulerSettings _settings;

    public ComunicacaoProcessamentoService(
        IComunicacaoEntregaService entregaService,
        IComunicacaoEntregaRepository entregaRepository,
        IEnumerable<IComunicacaoCanalProvider> providers,
        ILogger<ComunicacaoProcessamentoService> logger,
        IOptions<MessageSchedulerSettings> settings)
    {
        _entregaService = entregaService;
        _entregaRepository = entregaRepository;
        _providers = providers.ToDictionary(x => x.Canal);
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<int> ProcessarPendentesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var reservadas = await _entregaService.ReservarPendentesAsync(limit);
        var processadas = 0;
        var primeira = true;

        foreach (var entregaResumo in reservadas)
        {
            try
            {
                var entrega = await _entregaRepository.GetByIdAsync(entregaResumo.Id);
                if (entrega == null)
                {
                    continue;
                }

                // Rate limit simples entre envios (DelayBetweenSendsMs).
                if (!primeira && _settings.DelayBetweenSendsMs > 0)
                {
                    await Task.Delay(_settings.DelayBetweenSendsMs, cancellationToken);
                }
                primeira = false;

                var resultado = await ProcessarEntregaAsync(entrega, cancellationToken);
                if (resultado.Sucesso)
                {
                    await _entregaService.MarcarComoEnviadaAsync(entrega.Id, resultado.ProviderMessageId);
                    processadas++;
                }
                else
                {
                    await _entregaService.MarcarComoFalhaAsync(
                        entrega.Id, resultado.Mensagem ?? "Falha no envio", resultado.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                await _entregaService.MarcarComoFalhaAsync(entregaResumo.Id, ex.Message);
                _logger.LogWarning(ex, "Falha ao processar entrega de comunicação. EntregaId={EntregaId}", entregaResumo.Id);
            }
        }

        return processadas;
    }

    public async Task<bool> ProcessarEntregaAsync(int entregaId, CancellationToken cancellationToken = default)
    {
        var entrega = await _entregaRepository.GetByIdAsync(entregaId);
        if (entrega == null)
        {
            return false;
        }

        try
        {
            var resultado = await ProcessarEntregaAsync(entrega, cancellationToken);
            if (resultado.Sucesso)
            {
                await _entregaService.MarcarComoEnviadaAsync(entregaId, resultado.ProviderMessageId);
                return true;
            }

            await _entregaService.MarcarComoFalhaAsync(entregaId, resultado.Mensagem ?? "Falha no envio", resultado.ErrorCode);
            return false;
        }
        catch (Exception ex)
        {
            await _entregaService.MarcarComoFalhaAsync(entregaId, ex.Message);
            _logger.LogWarning(ex, "Falha ao processar entrega de comunicação. EntregaId={EntregaId}", entregaId);
            return false;
        }
    }

    /// <summary>
    /// Despacha a entrega pelo provider do canal. Lança apenas em erro de configuração/canal;
    /// falha de envio é retornada no resultado (para o chamador decidir retry/falha).
    /// </summary>
    private async Task<ComunicacaoCanalEnvioResultado> ProcessarEntregaAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(entrega.Canal, out var provider))
        {
            throw new InvalidOperationException($"Canal {entrega.Canal} ainda não está habilitado para processamento neste estágio.");
        }

        var diagnostico = await provider.ValidarConfiguracaoAsync(cancellationToken);
        if (!diagnostico.Configurado)
        {
            throw new InvalidOperationException(diagnostico.Mensagem ?? $"Canal {provider.Nome} não configurado.");
        }

        return await provider.EnviarAsync(entrega, cancellationToken);
    }
}
