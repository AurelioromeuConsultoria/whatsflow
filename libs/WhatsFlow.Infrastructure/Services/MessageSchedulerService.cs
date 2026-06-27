using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class MessageSchedulerService : BackgroundService
{
    private const string SchedulerName = "message_scheduler";
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageSchedulerService> _logger;
    private readonly MessageSchedulerSettings _settings;
    private readonly ISchedulerExecutionMonitor _executionMonitor;

    public MessageSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<MessageSchedulerService> logger,
        IOptions<MessageSchedulerSettings> settings,
        ISchedulerExecutionMonitor executionMonitor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
        _executionMonitor = executionMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MessageSchedulerService iniciado. Intervalo base: {BaseMin} min, jitter: 0–{Jitter}s, batch: {Batch}",
            _settings.BaseIntervalMinutes,
            _settings.JitterSecondsMax,
            _settings.BatchSizeReserva);

        await ValidarEvolutionApiNoBootAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var startedAtUtc = DateTime.UtcNow;
            try
            {
                var (processedTenants, entregasComunicacaoProcessadas) = await ProcessarMensagensAgendadas(stoppingToken);
                _executionMonitor.RecordSuccess(
                    SchedulerName,
                    startedAtUtc,
                    DateTime.UtcNow,
                    $"Intervalo base: {_settings.BaseIntervalMinutes} min; batch: {_settings.BatchSizeReserva}; tenants: {processedTenants}; comunicacao: {entregasComunicacaoProcessadas}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagens agendadas");
                _executionMonitor.RecordFailure(
                    SchedulerName,
                    startedAtUtc,
                    DateTime.UtcNow,
                    ex.Message,
                    $"Intervalo base: {_settings.BaseIntervalMinutes} min; batch: {_settings.BatchSizeReserva}");
            }

            var delay = ObterDelayComJitter();
            _logger.LogDebug("Próxima execução em {Delay}", delay);
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("MessageSchedulerService parado");
    }

    private async Task ValidarEvolutionApiNoBootAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var evolutionService = scope.ServiceProvider.GetRequiredService<IEvolutionApiService>();
            var ok = await evolutionService.ValidarInstanciaAsync(stoppingToken);

            if (ok)
            {
                _logger.LogInformation("Evolution API: validação inicial OK (instância encontrada)");
            }
            else
            {
                _logger.LogWarning("Evolution API: validação inicial falhou (veja logs de RequestUri/404 acima)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Evolution API: falha ao validar no boot");
        }
    }

    /// <summary>
    /// Intervalo base + jitter aleatório (0–JitterSecondsMax) para reduzir sincronismo entre instâncias.
    /// </summary>
    private TimeSpan ObterDelayComJitter()
    {
        var baseInterval = TimeSpan.FromMinutes(_settings.BaseIntervalMinutes);
        var jitterSec = Random.Shared.Next(0, _settings.JitterSecondsMax + 1);
        return baseInterval.Add(TimeSpan.FromSeconds(jitterSec));
    }

    private async Task<(int ProcessedTenants, int EntregasComunicacaoProcessadas)> ProcessarMensagensAgendadas(CancellationToken stoppingToken)
    {
        var processedTenants = 0;
        var entregasComunicacaoProcessadas = 0;
        foreach (var tenant in await GetActiveTenantsAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetService<TenantScopeOverride>()?.SetTenant(tenant.Id, tenant.Slug);
            var mensagemService = scope.ServiceProvider.GetRequiredService<IMensagemAgendadaService>();
            var comunicacaoProcessamentoService = scope.ServiceProvider.GetService<IComunicacaoProcessamentoService>();

            var reservadas = await mensagemService.ReservarProntasParaEnvioAsync(_settings.BatchSizeReserva);
            var lista = reservadas.ToList();

            foreach (var mensagem in lista)
            {
                try
                {
                    _logger.LogInformation(
                        "Processando mensagem ID {MensagemId} do tenant {TenantSlug} para visitante {VisitanteNome} ({Telefone})",
                        mensagem.Id,
                        tenant.Slug,
                        mensagem.NomeVisitante,
                        mensagem.TelefoneVisitante);

                    await EnviarViaEvolutionApi(mensagem);

                    await mensagemService.MarcarComoEnviadaAsync(mensagem.Id);

                    _logger.LogInformation(
                        "Mensagem ID {MensagemId} do tenant {TenantSlug} enviada com sucesso para {Telefone}",
                        mensagem.Id,
                        tenant.Slug,
                        mensagem.TelefoneVisitante);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Erro ao enviar mensagem ID {MensagemId} do tenant {TenantSlug} para {Telefone}",
                        mensagem.Id,
                        tenant.Slug,
                        mensagem.TelefoneVisitante);

                    await mensagemService.MarcarComoErroAsync(mensagem.Id, ex.Message);
                }
            }

            if (lista.Count > 0)
            {
                _logger.LogInformation("Tenant {TenantSlug}: processadas {Count} mensagens reservadas", tenant.Slug, lista.Count);
            }

            if (comunicacaoProcessamentoService != null)
            {
                var processadas = await comunicacaoProcessamentoService.ProcessarPendentesAsync(_settings.BatchSizeReserva, stoppingToken);
                entregasComunicacaoProcessadas += processadas;

                if (processadas > 0)
                {
                    _logger.LogInformation("Tenant {TenantSlug}: processadas {Count} entregas de comunicação", tenant.Slug, processadas);
                }
            }

            processedTenants++;
        }

        return (processedTenants, entregasComunicacaoProcessadas);
    }

    private async Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<WhatsFlowDbContext>();
        if (dbContext == null)
        {
            return [new Tenant { Id = Tenant.InitialTenantId, Nome = Tenant.InitialTenantName, Slug = Tenant.InitialTenantSlug, Ativo = true }];
        }

        return await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Ativo)
            .OrderBy(t => t.Id)
            .ToListAsync(stoppingToken);
    }

    private async Task EnviarViaEvolutionApi(Application.DTOs.MensagemAgendadaDto mensagem)
    {
        using var scope = _serviceProvider.CreateScope();
        var evolutionService = scope.ServiceProvider.GetRequiredService<IEvolutionApiService>();

        if (string.IsNullOrWhiteSpace(mensagem.TelefoneVisitante))
        {
            throw new InvalidOperationException(
                $"Mensagem ID {mensagem.Id} não possui número de telefone/WhatsApp para envio");
        }

        _logger.LogInformation(
            "Enviando mensagem via Evolution API - ID: {MensagemId}, Número: {Telefone}, Visitante: {Nome}",
            mensagem.Id,
            mensagem.TelefoneVisitante,
            mensagem.NomeVisitante);

        var resultado = await evolutionService.EnviarMensagemTextoAsync(
            mensagem.TelefoneVisitante,
            mensagem.TextoFinal);

        if (!resultado.Sucesso)
        {
            var erro = $"Evolution API: {resultado.MensagemErro} (Status: {resultado.StatusCode})";
            _logger.LogError(
                "Falha ao enviar mensagem ID {MensagemId} - {Erro}",
                mensagem.Id,
                erro);

            throw new Exception(erro);
        }

        _logger.LogInformation(
            "Mensagem ID {MensagemId} enviada via Evolution API - MessageId: {MessageId}",
            mensagem.Id,
            resultado.MessageId);
    }
}
