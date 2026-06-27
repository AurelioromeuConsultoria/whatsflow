using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public class AsaasPaymentResult
{
    public bool Success { get; set; }
    public string? ExternalPaymentId { get; set; }
    public string? PixPayload { get; set; }
    public string? PixEncodedImage { get; set; }
    public DateTime? PixExpirationDate { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AsaasPaymentStatusResult
{
    public bool Success { get; set; }
    public string? Status { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IAsaasPaymentService
{
    Task<AsaasPaymentResult> CreatePixPaymentAsync(GivingProviderConfig config, string apiKey, DoacaoOnline doacao, CancellationToken cancellationToken = default);
    Task<AsaasPaymentStatusResult> GetPaymentStatusAsync(GivingProviderConfig config, string apiKey, string externalPaymentId, CancellationToken cancellationToken = default);
}

public class AsaasPaymentService : IAsaasPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AsaasPaymentService> _logger;

    public AsaasPaymentService(HttpClient httpClient, ILogger<AsaasPaymentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AsaasPaymentResult> CreatePixPaymentAsync(GivingProviderConfig config, string apiKey, DoacaoOnline doacao, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AsaasPaymentResult { Success = false, ErrorMessage = "API Key do Asaas não configurada." };
        }

        ConfigureHttpClient(config, apiKey);

        try
        {
            var customerResponse = await _httpClient.PostAsJsonAsync("customers", new
            {
                name = doacao.NomeDoador,
                email = doacao.Email,
                mobilePhone = doacao.WhatsApp,
                cpfCnpj = doacao.Documento
            }, cancellationToken);

            if (!customerResponse.IsSuccessStatusCode)
            {
                var error = await customerResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Falha ao criar customer Asaas: {Status} {Body}", customerResponse.StatusCode, error);
                return new AsaasPaymentResult { Success = false, ErrorMessage = "Não foi possível criar o doador no Asaas." };
            }

            var customer = await customerResponse.Content.ReadFromJsonAsync<AsaasCustomerResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(customer?.Id))
            {
                return new AsaasPaymentResult { Success = false, ErrorMessage = "Asaas não retornou o identificador do doador." };
            }

            var dueDate = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
            var paymentResponse = await _httpClient.PostAsJsonAsync("payments", new
            {
                customer = customer.Id,
                billingType = "PIX",
                value = doacao.Valor,
                dueDate,
                description = doacao.FinalidadeDoacao?.Nome is { Length: > 0 } nome
                    ? $"Doação - {nome}"
                    : "Doação online"
            }, cancellationToken);

            if (!paymentResponse.IsSuccessStatusCode)
            {
                var error = await paymentResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Falha ao criar cobrança Asaas: {Status} {Body}", paymentResponse.StatusCode, error);
                return new AsaasPaymentResult { Success = false, ErrorMessage = "Não foi possível criar a cobrança Pix no Asaas." };
            }

            var payment = await paymentResponse.Content.ReadFromJsonAsync<AsaasPaymentResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(payment?.Id))
            {
                return new AsaasPaymentResult { Success = false, ErrorMessage = "Asaas não retornou o identificador da cobrança." };
            }

            var qrCodeResponse = await _httpClient.GetAsync($"payments/{payment.Id}/pixQrCode", cancellationToken);
            if (!qrCodeResponse.IsSuccessStatusCode)
            {
                var error = await qrCodeResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Falha ao obter QR Code Asaas: {Status} {Body}", qrCodeResponse.StatusCode, error);
                return new AsaasPaymentResult
                {
                    Success = true,
                    ExternalPaymentId = payment.Id,
                    ErrorMessage = "Cobrança criada, mas QR Code Pix não foi retornado."
                };
            }

            var qrCode = await qrCodeResponse.Content.ReadFromJsonAsync<AsaasPixQrCodeResponse>(cancellationToken: cancellationToken);
            return new AsaasPaymentResult
            {
                Success = true,
                ExternalPaymentId = payment.Id,
                PixPayload = qrCode?.Payload,
                PixEncodedImage = qrCode?.EncodedImage,
                PixExpirationDate = ParseAsaasDate(qrCode?.ExpirationDate),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cobrança Asaas para doação {DoacaoId}", doacao.Id);
            return new AsaasPaymentResult { Success = false, ErrorMessage = "Erro de comunicação com o Asaas." };
        }
    }

    public async Task<AsaasPaymentStatusResult> GetPaymentStatusAsync(GivingProviderConfig config, string apiKey, string externalPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AsaasPaymentStatusResult { Success = false, ErrorMessage = "API Key do Asaas não configurada." };
        }

        if (string.IsNullOrWhiteSpace(externalPaymentId))
        {
            return new AsaasPaymentStatusResult { Success = false, ErrorMessage = "Cobrança Asaas não informada." };
        }

        ConfigureHttpClient(config, apiKey);

        try
        {
            var response = await _httpClient.GetAsync($"payments/{externalPaymentId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Falha ao consultar cobrança Asaas {PaymentId}: {Status} {Body}", externalPaymentId, response.StatusCode, error);
                return new AsaasPaymentStatusResult { Success = false, ErrorMessage = "Não foi possível consultar a cobrança no Asaas." };
            }

            var payment = await response.Content.ReadFromJsonAsync<AsaasPaymentStatusResponse>(cancellationToken: cancellationToken);
            return new AsaasPaymentStatusResult
            {
                Success = true,
                Status = payment?.Status,
                ConfirmedDate = ParseAsaasDate(payment?.ConfirmedDate ?? payment?.PaymentDate ?? payment?.ClientPaymentDate)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar cobrança Asaas {PaymentId}", externalPaymentId);
            return new AsaasPaymentStatusResult { Success = false, ErrorMessage = "Erro de comunicação com o Asaas." };
        }
    }

    private void ConfigureHttpClient(GivingProviderConfig config, string apiKey)
    {
        var baseUrl = config.Environment == GivingProviderEnvironment.Production
            ? "https://api.asaas.com/v3/"
            : "https://sandbox.asaas.com/api/v3/";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WhatsFlow/1.0");
        _httpClient.DefaultRequestHeaders.Add("access_token", apiKey);
    }

    private static DateTime? ParseAsaasDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var formats = new[]
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ssK"
        };

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var exact))
        {
            return exact;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed
            : null;
    }

    private class AsaasCustomerResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private class AsaasPaymentResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private class AsaasPaymentStatusResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("confirmedDate")]
        public string? ConfirmedDate { get; set; }

        [JsonPropertyName("paymentDate")]
        public string? PaymentDate { get; set; }

        [JsonPropertyName("clientPaymentDate")]
        public string? ClientPaymentDate { get; set; }
    }

    private class AsaasPixQrCodeResponse
    {
        [JsonPropertyName("encodedImage")]
        public string? EncodedImage { get; set; }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }

        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }
    }
}
