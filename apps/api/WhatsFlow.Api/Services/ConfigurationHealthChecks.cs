using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;

namespace WhatsFlow.API.Services;

public class EvolutionApiConfigurationHealthCheck : IHealthCheck
{
    private readonly EvolutionApiSettings _settings;

    public EvolutionApiConfigurationHealthCheck(IOptions<EvolutionApiSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl)) missing.Add(nameof(_settings.BaseUrl));
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)) missing.Add(nameof(_settings.ApiKey));
        if (string.IsNullOrWhiteSpace(_settings.InstanceName)) missing.Add(nameof(_settings.InstanceName));

        if (missing.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Evolution API configuration OK."));
        }

        return Task.FromResult(HealthCheckResult.Degraded(
            $"Evolution API configuration incomplete. Missing: {string.Join(", ", missing)}."));
    }
}

public class MessageSchedulerConfigurationHealthCheck : IHealthCheck
{
    private readonly MessageSchedulerSettings _settings;

    public MessageSchedulerConfigurationHealthCheck(IOptions<MessageSchedulerSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_settings.BaseIntervalMinutes <= 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("MessageScheduler BaseIntervalMinutes must be greater than zero."));
        }

        if (_settings.BatchSizeReserva <= 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("MessageScheduler BatchSizeReserva must be greater than zero."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("MessageScheduler configuration OK."));
    }
}

public class EmailConfigurationHealthCheck : IHealthCheck
{
    private readonly EmailSettings _settings;

    public EmailConfigurationHealthCheck(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (!_settings.Enabled) missing.Add(nameof(_settings.Enabled));
        if (string.IsNullOrWhiteSpace(_settings.Host)) missing.Add(nameof(_settings.Host));
        if (string.IsNullOrWhiteSpace(_settings.FromAddress)) missing.Add(nameof(_settings.FromAddress));
        if (!string.IsNullOrWhiteSpace(_settings.Username) && string.IsNullOrWhiteSpace(_settings.Password))
            missing.Add(nameof(_settings.Password));

        if (missing.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Email configuration OK."));
        }

        return Task.FromResult(HealthCheckResult.Degraded(
            $"Email configuration incomplete. Missing: {string.Join(", ", missing)}."));
    }
}

// TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
