using Microsoft.Extensions.Logging;
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

    public ComunicacaoProcessamentoService(
        IComunicacaoEntregaService entregaService,
        IComunicacaoEntregaRepository entregaRepository,
        IEnumerable<IComunicacaoCanalProvider> providers,
        ILogger<ComunicacaoProcessamentoService> logger)
    {
        _entregaService = entregaService;
        _entregaRepository = entregaRepository;
        _providers = providers.ToDictionary(x => x.Canal);
        _logger = logger;
    }

    public async Task<int> ProcessarPendentesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var reservadas = await _entregaService.ReservarPendentesAsync(limit);
        var processadas = 0;

        foreach (var entregaResumo in reservadas)
        {
            try
            {
                var entrega = await _entregaRepository.GetByIdAsync(entregaResumo.Id);
                if (entrega == null)
                {
                    continue;
                }

                await ProcessarEntregaAsync(entrega, cancellationToken);
                await _entregaService.MarcarComoEnviadaAsync(entrega.Id);
                processadas++;
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
            await ProcessarEntregaAsync(entrega, cancellationToken);
            await _entregaService.MarcarComoEnviadaAsync(entregaId);
            return true;
        }
        catch (Exception ex)
        {
            await _entregaService.MarcarComoFalhaAsync(entregaId, ex.Message);
            _logger.LogWarning(ex, "Falha ao processar entrega de comunicação. EntregaId={EntregaId}", entregaId);
            return false;
        }
    }

    private async Task ProcessarEntregaAsync(ComunicacaoEntrega entrega, CancellationToken cancellationToken)
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

        var resultado = await provider.EnviarAsync(entrega, cancellationToken);
        if (!resultado.Sucesso)
        {
            throw new InvalidOperationException(resultado.Mensagem ?? $"Falha ao enviar entrega pelo canal {provider.Nome}.");
        }
    }
}
