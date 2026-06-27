using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.Infrastructure.Services;

/// <summary>
/// Job que aplica periodicamente as transições do ciclo de billing
/// (trial expirado → inadimplente; carência esgotada → suspensa).
/// </summary>
public class BillingSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BillingSchedulerSettings _settings;
    private readonly ILogger<BillingSchedulerService> _logger;

    public BillingSchedulerService(
        IServiceProvider serviceProvider,
        IOptions<BillingSchedulerSettings> settings,
        ILogger<BillingSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BillingSchedulerService iniciado (Enabled: {Enabled}, intervalo: {Min}min).",
            _settings.Enabled, _settings.BaseIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_settings.Enabled)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var cycle = scope.ServiceProvider.GetRequiredService<IBillingCycleService>();
                    await cycle.ExecutarTransicoesAutomaticasAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar o ciclo de billing.");
                }
            }

            await Task.Delay(ProximoIntervalo(), stoppingToken);
        }
    }

    private TimeSpan ProximoIntervalo()
    {
        var baseInterval = TimeSpan.FromMinutes(_settings.BaseIntervalMinutes);
        var jitter = Random.Shared.Next(0, _settings.JitterSecondsMax + 1);
        return baseInterval.Add(TimeSpan.FromSeconds(jitter));
    }
}
