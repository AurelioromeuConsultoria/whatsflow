using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WhatsFlow.Infrastructure.Services;

public class BirthdayCampaignSchedulerService : BackgroundService
{
    private const string SchedulerName = "birthday_campaign_scheduler";
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BirthdayCampaignSchedulerService> _logger;
    private readonly BirthdayCampaignSchedulerSettings _settings;
    private readonly ISchedulerExecutionMonitor _executionMonitor;

    public BirthdayCampaignSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<BirthdayCampaignSchedulerService> logger,
        IOptions<BirthdayCampaignSchedulerSettings> settings,
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
            "BirthdayCampaignSchedulerService iniciado. Intervalo base: {BaseMin} min, jitter: 0–{Jitter}s, limite por execução: {Max}.",
            _settings.BaseIntervalMinutes,
            _settings.JitterSecondsMax,
            _settings.MaxPessoasPorExecucao);

        while (!stoppingToken.IsCancellationRequested)
        {
            var startedAtUtc = DateTime.UtcNow;
            try
            {
                var processedTenants = 0;
                foreach (var tenant in await GetActiveTenantsAsync(stoppingToken))
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    using var scope = _serviceProvider.CreateScope();
                    scope.ServiceProvider.GetService<TenantScopeOverride>()?.SetTenant(tenant.Id, tenant.Slug);
                    var service = scope.ServiceProvider.GetRequiredService<ICampanhaAniversarioService>();
                    var resultado = await service.ProcessarAniversariantesDoDiaAsync(stoppingToken);

                    if (resultado.TotalElegiveis > 0)
                    {
                        _logger.LogInformation(
                            "Tenant {TenantSlug}: campanha de aniversário processada. Elegíveis: {Elegiveis}, Enviados: {Enviados}, Ignorados: {Ignorados}, Falhas: {Falhas}.",
                            tenant.Slug,
                            resultado.TotalElegiveis,
                            resultado.TotalEnviados,
                            resultado.TotalIgnorados,
                            resultado.TotalFalhas);
                    }

                    processedTenants++;
                }

                _executionMonitor.RecordSuccess(
                    SchedulerName,
                    startedAtUtc,
                    DateTime.UtcNow,
                    $"Max por execução: {_settings.MaxPessoasPorExecucao}; timezone: {_settings.TimeZoneId}; tenants: {processedTenants}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar campanha de aniversário.");
                _executionMonitor.RecordFailure(
                    SchedulerName,
                    startedAtUtc,
                    DateTime.UtcNow,
                    ex.Message,
                    $"Max por execução: {_settings.MaxPessoasPorExecucao}; timezone: {_settings.TimeZoneId}");
            }

            await Task.Delay(ObterDelayComJitter(), stoppingToken);
        }
    }

    private TimeSpan ObterDelayComJitter()
    {
        var baseInterval = TimeSpan.FromMinutes(_settings.BaseIntervalMinutes);
        var jitter = Random.Shared.Next(0, _settings.JitterSecondsMax + 1);
        return baseInterval.Add(TimeSpan.FromSeconds(jitter));
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
}
