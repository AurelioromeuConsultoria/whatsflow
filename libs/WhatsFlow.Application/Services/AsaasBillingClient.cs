using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public class AsaasCustomerRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
}

public class AsaasSubscriptionRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public CicloCobranca Ciclo { get; set; } = CicloCobranca.Mensal;
    public DateTime PrimeiroVencimento { get; set; }
    public string? Descricao { get; set; }

    /// <summary>"UNDEFINED" deixa o pagador escolher (PIX/boleto/cartão). Padrão para trial sem cartão.</summary>
    public string BillingType { get; set; } = "UNDEFINED";
}

public class AsaasBillingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AsaasCustomerResult : AsaasBillingResult
{
    public string? CustomerId { get; set; }
}

public class AsaasSubscriptionResult : AsaasBillingResult
{
    public string? SubscriptionId { get; set; }
    public string? Status { get; set; }
}

public class AsaasSubscriptionStatusResult : AsaasBillingResult
{
    public string? Status { get; set; }
    public bool Deleted { get; set; }
}

/// <summary>
/// Cliente para a API recorrente do Asaas (conta da plataforma): clientes e assinaturas.
/// Distinto de IAsaasPaymentService (PIX avulso de doações, por-tenant).
/// </summary>
public interface IAsaasBillingClient
{
    /// <summary>True quando a API key da plataforma está configurada.</summary>
    bool Configurado { get; }

    Task<AsaasCustomerResult> CreateCustomerAsync(AsaasCustomerRequest request, CancellationToken cancellationToken = default);
    Task<AsaasSubscriptionResult> CreateSubscriptionAsync(AsaasSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<AsaasSubscriptionStatusResult> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<AsaasSubscriptionStatusResult> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
}

public class AsaasBillingClient : IAsaasBillingClient
{
    private readonly HttpClient _httpClient;
    private readonly AsaasBillingSettings _settings;
    private readonly ILogger<AsaasBillingClient> _logger;

    public AsaasBillingClient(HttpClient httpClient, IOptions<AsaasBillingSettings> settings, ILogger<AsaasBillingClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public async Task<AsaasCustomerResult> CreateCustomerAsync(AsaasCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (!Configurado)
        {
            return new AsaasCustomerResult { Success = false, ErrorMessage = "API key de billing (Asaas plataforma) não configurada." };
        }

        try
        {
            using var response = await SendAsync(HttpMethod.Post, "customers", new
            {
                name = request.Nome,
                email = request.Email,
                cpfCnpj = request.CpfCnpj,
                mobilePhone = request.Telefone
            }, cancellationToken);

            var (ok, id, _, _, erro) = await ReadAsync(response, cancellationToken);
            return ok
                ? new AsaasCustomerResult { Success = true, CustomerId = id }
                : new AsaasCustomerResult { Success = false, ErrorMessage = erro };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao criar customer de billing no Asaas");
            return new AsaasCustomerResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<AsaasSubscriptionResult> CreateSubscriptionAsync(AsaasSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Configurado)
        {
            return new AsaasSubscriptionResult { Success = false, ErrorMessage = "API key de billing (Asaas plataforma) não configurada." };
        }

        try
        {
            using var response = await SendAsync(HttpMethod.Post, "subscriptions", new
            {
                customer = request.CustomerId,
                billingType = request.BillingType,
                value = request.Valor,
                nextDueDate = request.PrimeiroVencimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cycle = request.Ciclo == CicloCobranca.Anual ? "YEARLY" : "MONTHLY",
                description = request.Descricao
            }, cancellationToken);

            var (ok, id, status, _, erro) = await ReadAsync(response, cancellationToken);
            return ok
                ? new AsaasSubscriptionResult { Success = true, SubscriptionId = id, Status = status }
                : new AsaasSubscriptionResult { Success = false, ErrorMessage = erro };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao criar assinatura no Asaas");
            return new AsaasSubscriptionResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<AsaasSubscriptionStatusResult> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        if (!Configurado)
        {
            return new AsaasSubscriptionStatusResult { Success = false, ErrorMessage = "API key de billing não configurada." };
        }

        try
        {
            using var response = await SendAsync(HttpMethod.Get, $"subscriptions/{subscriptionId}", null, cancellationToken);
            var (ok, _, status, deleted, erro) = await ReadAsync(response, cancellationToken);
            return ok
                ? new AsaasSubscriptionStatusResult { Success = true, Status = status, Deleted = deleted }
                : new AsaasSubscriptionStatusResult { Success = false, ErrorMessage = erro };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar assinatura no Asaas");
            return new AsaasSubscriptionStatusResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<AsaasSubscriptionStatusResult> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        if (!Configurado)
        {
            return new AsaasSubscriptionStatusResult { Success = false, ErrorMessage = "API key de billing não configurada." };
        }

        try
        {
            using var response = await SendAsync(HttpMethod.Delete, $"subscriptions/{subscriptionId}", null, cancellationToken);
            var (ok, _, status, deleted, erro) = await ReadAsync(response, cancellationToken);
            return ok
                ? new AsaasSubscriptionStatusResult { Success = true, Status = status, Deleted = deleted }
                : new AsaasSubscriptionStatusResult { Success = false, ErrorMessage = erro };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao cancelar assinatura no Asaas");
            return new AsaasSubscriptionStatusResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, _settings.BaseUrl + path);
        request.Headers.Add("access_token", _settings.ApiKey);
        request.Headers.UserAgent.ParseAdd("VerboPlus/1.0");
        request.Headers.Accept.ParseAdd("application/json");
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<(bool ok, string? id, string? status, bool deleted, string? erro)> ReadAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Asaas billing respondeu {Status}: {Body}", (int)response.StatusCode, content);
            return (false, null, null, false, $"Asaas retornou {(int)response.StatusCode}.");
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<AsaasEntityResponse>(content);
            return (true, parsed?.Id, parsed?.Status, parsed?.Deleted ?? false, null);
        }
        catch (Exception)
        {
            return (true, null, null, false, null);
        }
    }

    private sealed class AsaasEntityResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }
    }
}
