using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IDoacoesService
{
    Task<IEnumerable<FinalidadeDoacaoDto>> GetFinalidadesAsync(bool publicOnly = false);
    Task<FinalidadeDoacaoDto?> GetFinalidadeByIdAsync(int id);
    Task<FinalidadeDoacaoDto> CreateFinalidadeAsync(SalvarFinalidadeDoacaoDto dto);
    Task<FinalidadeDoacaoDto> UpdateFinalidadeAsync(int id, SalvarFinalidadeDoacaoDto dto);
    Task DeleteFinalidadeAsync(int id);
    Task<IEnumerable<DoacaoOnlineDto>> GetDoacoesAsync();
    Task<DoacaoOnlineDto?> GetDoacaoByIdAsync(int id);
    Task<DoacaoOnlineDto?> GetDoacaoByReciboTokenAsync(string reciboToken);
    Task<DoacaoReciboDto?> GetReciboByTokenAsync(string reciboToken);
    Task<DoacaoOnlineDto> CreateDoacaoAsync(CriarDoacaoOnlineDto dto);
    Task<GivingProviderConfigDto> GetProviderConfigAsync(GivingProvider provider);
    Task<GivingProviderConfigDto> SaveProviderConfigAsync(SalvarGivingProviderConfigDto dto);
    Task<bool> ProcessAsaasWebhookAsync(JsonElement payload, string? accessTokenHeader);
}

public class DoacoesService : IDoacoesService
{
    private readonly IDoacoesRepository _repository;
    private readonly ISecretProtector _secretProtector;
    private readonly IAsaasPaymentService _asaasPaymentService;

    public DoacoesService(
        IDoacoesRepository repository,
        ISecretProtector secretProtector,
        IAsaasPaymentService asaasPaymentService)
    {
        _repository = repository;
        _secretProtector = secretProtector;
        _asaasPaymentService = asaasPaymentService;
    }

    public async Task<IEnumerable<FinalidadeDoacaoDto>> GetFinalidadesAsync(bool publicOnly = false)
    {
        var items = await _repository.GetFinalidadesAsync(publicOnly);
        return items.Select(MapFinalidadeToDto);
    }

    public async Task<FinalidadeDoacaoDto?> GetFinalidadeByIdAsync(int id)
    {
        var item = await _repository.GetFinalidadeByIdAsync(id);
        return item is null ? null : MapFinalidadeToDto(item);
    }

    public async Task<FinalidadeDoacaoDto> CreateFinalidadeAsync(SalvarFinalidadeDoacaoDto dto)
    {
        var slug = await BuildUniqueSlugAsync(dto.Slug, dto.Nome);
        var entity = new FinalidadeDoacao
        {
            Nome = dto.Nome.Trim(),
            Slug = slug,
            DescricaoPublica = NormalizeOptional(dto.DescricaoPublica),
            ImagemUrl = NormalizeOptional(dto.ImagemUrl),
            CorHex = NormalizeOptional(dto.CorHex),
            ValoresSugeridos = SerializeValores(dto.ValoresSugeridos),
            ValorMinimo = dto.ValorMinimo,
            Ordem = dto.Ordem,
            Ativo = dto.Ativo,
            VisivelPortal = dto.VisivelPortal,
            PermiteAnonimo = dto.PermiteAnonimo,
            PermitePix = dto.PermitePix,
            PermiteCartaoCredito = dto.PermiteCartaoCredito,
            CategoriaReceitaId = dto.CategoriaReceitaId,
            ContaBancariaId = dto.ContaBancariaId,
            CentroCustoId = dto.CentroCustoId,
            ProjetoId = dto.ProjetoId,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateFinalidadeAsync(entity);
        var createdWithRelations = await _repository.GetFinalidadeByIdAsync(created.Id);
        return MapFinalidadeToDto(createdWithRelations ?? created);
    }

    public async Task<FinalidadeDoacaoDto> UpdateFinalidadeAsync(int id, SalvarFinalidadeDoacaoDto dto)
    {
        var entity = await _repository.GetFinalidadeByIdAsync(id);
        if (entity is null) throw new ArgumentException("Finalidade de doação não encontrada");

        entity.Nome = dto.Nome.Trim();
        entity.Slug = await BuildUniqueSlugAsync(dto.Slug, dto.Nome, id);
        entity.DescricaoPublica = NormalizeOptional(dto.DescricaoPublica);
        entity.ImagemUrl = NormalizeOptional(dto.ImagemUrl);
        entity.CorHex = NormalizeOptional(dto.CorHex);
        entity.ValoresSugeridos = SerializeValores(dto.ValoresSugeridos);
        entity.ValorMinimo = dto.ValorMinimo;
        entity.Ordem = dto.Ordem;
        entity.Ativo = dto.Ativo;
        entity.VisivelPortal = dto.VisivelPortal;
        entity.PermiteAnonimo = dto.PermiteAnonimo;
        entity.PermitePix = dto.PermitePix;
        entity.PermiteCartaoCredito = dto.PermiteCartaoCredito;
        entity.CategoriaReceitaId = dto.CategoriaReceitaId;
        entity.ContaBancariaId = dto.ContaBancariaId;
        entity.CentroCustoId = dto.CentroCustoId;
        entity.ProjetoId = dto.ProjetoId;

        var updated = await _repository.UpdateFinalidadeAsync(entity);
        var updatedWithRelations = await _repository.GetFinalidadeByIdAsync(updated.Id);
        return MapFinalidadeToDto(updatedWithRelations ?? updated);
    }

    public async Task DeleteFinalidadeAsync(int id)
    {
        await _repository.DeleteFinalidadeAsync(id);
    }

    public async Task<IEnumerable<DoacaoOnlineDto>> GetDoacoesAsync()
    {
        var items = await _repository.GetDoacoesAsync();
        return items.Select(MapDoacaoToDto);
    }

    public async Task<DoacaoOnlineDto?> GetDoacaoByIdAsync(int id)
    {
        var item = await _repository.GetDoacaoByIdAsync(id);
        return item is null ? null : MapDoacaoToDto(item);
    }

    public async Task<DoacaoOnlineDto?> GetDoacaoByReciboTokenAsync(string reciboToken)
    {
        if (string.IsNullOrWhiteSpace(reciboToken)) return null;
        var item = await _repository.GetDoacaoByReciboTokenAsync(reciboToken.Trim());
        if (item is not null)
        {
            await TryRefreshAsaasStatusAsync(item);
        }

        return item is null ? null : MapDoacaoToDto(item);
    }

    public async Task<DoacaoReciboDto?> GetReciboByTokenAsync(string reciboToken)
    {
        if (string.IsNullOrWhiteSpace(reciboToken)) return null;
        var item = await _repository.GetDoacaoByReciboTokenAsync(reciboToken.Trim());
        if (item is null || item.Status != StatusDoacaoOnline.Confirmada || !item.DataConfirmacao.HasValue)
        {
            return null;
        }

        return MapReciboToDto(item);
    }

    public async Task<DoacaoOnlineDto> CreateDoacaoAsync(CriarDoacaoOnlineDto dto)
    {
        FinalidadeDoacao? finalidade = null;
        if (dto.FinalidadeDoacaoId.HasValue)
        {
            finalidade = await _repository.GetFinalidadeByIdAsync(dto.FinalidadeDoacaoId.Value);
            if (finalidade is null || !finalidade.Ativo || !finalidade.VisivelPortal)
            {
                throw new ArgumentException("Finalidade de doação inválida");
            }

            if (dto.Valor < (finalidade.ValorMinimo ?? 1))
            {
                throw new ArgumentException($"Valor mínimo para esta finalidade é {finalidade.ValorMinimo:C}");
            }

            if (dto.MetodoPagamento == MetodoPagamentoDoacao.Pix && !finalidade.PermitePix)
            {
                throw new ArgumentException("Pix não está disponível para esta finalidade");
            }

            if (dto.MetodoPagamento == MetodoPagamentoDoacao.CartaoCredito && !finalidade.PermiteCartaoCredito)
            {
                throw new ArgumentException("Cartão de crédito não está disponível para esta finalidade");
            }
        }

        var entity = new DoacaoOnline
        {
            FinalidadeDoacaoId = dto.FinalidadeDoacaoId,
            NomeDoador = dto.NomeDoador.Trim(),
            WhatsApp = NormalizeOptional(dto.WhatsApp),
            Email = NormalizeOptional(dto.Email),
            Documento = NormalizeOptional(dto.Documento),
            Anonima = dto.Anonima,
            Valor = dto.Valor,
            MetodoPagamento = dto.MetodoPagamento,
            Status = StatusDoacaoOnline.Pendente,
            Provider = "asaas",
            ReciboToken = GenerateReciboToken(),
            DataCriacao = DateTime.Now,
        };

        if (dto.MetodoPagamento == MetodoPagamentoDoacao.Pix && string.IsNullOrWhiteSpace(entity.Documento))
        {
            throw new ArgumentException("CPF ou CNPJ é obrigatório para gerar a cobrança Pix.");
        }

        var created = await _repository.CreateDoacaoAsync(entity);

        if (dto.MetodoPagamento == MetodoPagamentoDoacao.Pix)
        {
            await TryCreateAsaasPixPaymentAsync(created);
        }

        var createdWithRelations = await _repository.GetDoacaoByIdAsync(created.Id);
        return MapDoacaoToDto(createdWithRelations ?? created);
    }

    public async Task<GivingProviderConfigDto> GetProviderConfigAsync(GivingProvider provider)
    {
        var config = await _repository.GetProviderConfigAsync(provider);
        if (config is null)
        {
            return new GivingProviderConfigDto
            {
                Provider = provider,
                ProviderDescricao = GetProviderDescricao(provider),
                Environment = GivingProviderEnvironment.Sandbox,
                EnvironmentDescricao = GetEnvironmentDescricao(GivingProviderEnvironment.Sandbox),
                PixEnabled = true,
                CreditCardEnabled = false,
                BoletoEnabled = false,
                Ativo = false,
                Configurado = false,
                DataCriacao = DateTime.Now,
            };
        }

        return MapConfigToDto(config);
    }

    public async Task<GivingProviderConfigDto> SaveProviderConfigAsync(SalvarGivingProviderConfigDto dto)
    {
        var config = await _repository.GetProviderConfigAsync(dto.Provider) ?? new GivingProviderConfig
        {
            Provider = dto.Provider,
            DataCriacao = DateTime.Now,
        };

        config.Environment = dto.Environment;
        config.WebhookUrl = NormalizeOptional(dto.WebhookUrl);
        config.PixEnabled = dto.PixEnabled;
        config.CreditCardEnabled = dto.CreditCardEnabled;
        config.BoletoEnabled = dto.BoletoEnabled;
        config.Ativo = dto.Ativo;
        config.DataAtualizacao = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
        {
            config.ApiKeyProtegida = _secretProtector.Protect(dto.ApiKey.Trim());
            config.ApiKeyUltimosDigitos = MaskSecret(dto.ApiKey.Trim());
        }

        if (!string.IsNullOrWhiteSpace(dto.WebhookSecret))
        {
            config.WebhookSecretProtegido = _secretProtector.Protect(dto.WebhookSecret.Trim());
        }

        var saved = await _repository.SaveProviderConfigAsync(config);
        return MapConfigToDto(saved);
    }

    public async Task<bool> ProcessAsaasWebhookAsync(JsonElement payload, string? accessTokenHeader)
    {
        var eventName = GetJsonString(payload, "event");
        var payment = payload.TryGetProperty("payment", out var paymentElement) ? paymentElement : default;
        var paymentId = payment.ValueKind == JsonValueKind.Object ? GetJsonString(payment, "id") : null;

        if (string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(paymentId))
        {
            return false;
        }

        var doacao = await _repository.GetDoacaoByExternalPaymentIdAsync(paymentId);
        if (doacao is null)
        {
            return true;
        }

        var config = await _repository.GetProviderConfigByTenantAsync(doacao.TenantId, GivingProvider.Asaas);
        if (config?.WebhookSecretProtegido is { Length: > 0 })
        {
            string expectedToken;
            try
            {
                expectedToken = _secretProtector.Unprotect(config.WebhookSecretProtegido);
            }
            catch
            {
                return false;
            }

            if (!string.Equals(expectedToken, accessTokenHeader, StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (IsConfirmedAsaasEvent(eventName))
        {
            if (doacao.Status != StatusDoacaoOnline.Confirmada)
            {
                doacao.Status = StatusDoacaoOnline.Confirmada;
                doacao.DataConfirmacao = DateTime.Now;
                await _repository.UpdateDoacaoAsync(doacao);
            }

            await _repository.EnsureReceitaForDoacaoAsync(doacao);
            return true;
        }

        if (IsFailureAsaasEvent(eventName))
        {
            doacao.Status = eventName == "PAYMENT_REFUNDED"
                ? StatusDoacaoOnline.Estornada
                : StatusDoacaoOnline.Falhou;
            await _repository.UpdateDoacaoAsync(doacao);
            return true;
        }

        return true;
    }

    private async Task TryRefreshAsaasStatusAsync(DoacaoOnline doacao)
    {
        if (doacao.Provider != "asaas" ||
            string.IsNullOrWhiteSpace(doacao.ExternalPaymentId) ||
            doacao.Status is StatusDoacaoOnline.Confirmada or StatusDoacaoOnline.Cancelada or StatusDoacaoOnline.Estornada)
        {
            return;
        }

        var config = await _repository.GetProviderConfigByTenantAsync(doacao.TenantId, GivingProvider.Asaas);
        if (config is null || string.IsNullOrWhiteSpace(config.ApiKeyProtegida))
        {
            return;
        }

        string apiKey;
        try
        {
            apiKey = _secretProtector.Unprotect(config.ApiKeyProtegida);
        }
        catch
        {
            return;
        }

        var status = await _asaasPaymentService.GetPaymentStatusAsync(config, apiKey, doacao.ExternalPaymentId);
        if (!status.Success || string.IsNullOrWhiteSpace(status.Status))
        {
            return;
        }

        if (IsConfirmedAsaasStatus(status.Status))
        {
            doacao.Status = StatusDoacaoOnline.Confirmada;
            doacao.DataConfirmacao = status.ConfirmedDate ?? DateTime.Now;
            await _repository.UpdateDoacaoAsync(doacao);
            await _repository.EnsureReceitaForDoacaoAsync(doacao);
            return;
        }

        if (IsFailureAsaasStatus(status.Status))
        {
            doacao.Status = status.Status == "REFUNDED"
                ? StatusDoacaoOnline.Estornada
                : StatusDoacaoOnline.Cancelada;
            await _repository.UpdateDoacaoAsync(doacao);
        }
    }

    private async Task TryCreateAsaasPixPaymentAsync(DoacaoOnline doacao)
    {
        var config = await _repository.GetProviderConfigAsync(GivingProvider.Asaas);
        if (config is null || !config.Ativo || !config.PixEnabled || string.IsNullOrWhiteSpace(config.ApiKeyProtegida))
        {
            doacao.Status = StatusDoacaoOnline.Pendente;
            await _repository.UpdateDoacaoAsync(doacao);
            return;
        }

        string apiKey;
        try
        {
            apiKey = _secretProtector.Unprotect(config.ApiKeyProtegida);
        }
        catch
        {
            doacao.Status = StatusDoacaoOnline.Falhou;
            await _repository.UpdateDoacaoAsync(doacao);
            return;
        }

        var result = await _asaasPaymentService.CreatePixPaymentAsync(config, apiKey, doacao);
        if (!result.Success)
        {
            doacao.Status = StatusDoacaoOnline.Falhou;
            await _repository.UpdateDoacaoAsync(doacao);
            return;
        }

        doacao.Status = StatusDoacaoOnline.AguardandoPagamento;
        doacao.ExternalPaymentId = result.ExternalPaymentId;
        doacao.PixCopiaECola = result.PixPayload;
        doacao.PixQrCodeUrl = string.IsNullOrWhiteSpace(result.PixEncodedImage)
            ? null
            : $"data:image/png;base64,{result.PixEncodedImage}";
        doacao.DataVencimento = result.PixExpirationDate;
        await _repository.UpdateDoacaoAsync(doacao);
    }

    private async Task<string> BuildUniqueSlugAsync(string? requestedSlug, string nome, int? currentId = null)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? nome : requestedSlug);
        var candidate = baseSlug;
        var suffix = 2;

        while (true)
        {
            var existing = await _repository.GetFinalidadeBySlugAsync(candidate);
            if (existing is null || existing.Id == currentId)
            {
                return candidate;
            }

            candidate = $"{baseSlug}-{suffix++}";
        }
    }

    private static FinalidadeDoacaoDto MapFinalidadeToDto(FinalidadeDoacao f)
    {
        return new FinalidadeDoacaoDto
        {
            Id = f.Id,
            Nome = f.Nome,
            Slug = f.Slug,
            DescricaoPublica = f.DescricaoPublica,
            ImagemUrl = f.ImagemUrl,
            CorHex = f.CorHex,
            ValoresSugeridos = ParseValores(f.ValoresSugeridos),
            ValorMinimo = f.ValorMinimo,
            Ordem = f.Ordem,
            Ativo = f.Ativo,
            VisivelPortal = f.VisivelPortal,
            PermiteAnonimo = f.PermiteAnonimo,
            PermitePix = f.PermitePix,
            PermiteCartaoCredito = f.PermiteCartaoCredito,
            CategoriaReceitaId = f.CategoriaReceitaId,
            CategoriaReceitaNome = f.CategoriaReceita?.Nome,
            ContaBancariaId = f.ContaBancariaId,
            ContaBancariaNome = f.ContaBancaria?.Nome,
            CentroCustoId = f.CentroCustoId,
            CentroCustoNome = f.CentroCusto?.Nome,
            ProjetoId = f.ProjetoId,
            ProjetoNome = f.Projeto?.Nome,
            DataCriacao = f.DataCriacao,
        };
    }

    private static DoacaoOnlineDto MapDoacaoToDto(DoacaoOnline d)
    {
        return new DoacaoOnlineDto
        {
            Id = d.Id,
            FinalidadeDoacaoId = d.FinalidadeDoacaoId,
            FinalidadeNome = d.FinalidadeDoacao?.Nome,
            NomeDoador = d.NomeDoador,
            WhatsApp = d.WhatsApp,
            Email = d.Email,
            Anonima = d.Anonima,
            Valor = d.Valor,
            MetodoPagamento = d.MetodoPagamento,
            MetodoPagamentoDescricao = GetMetodoDescricao(d.MetodoPagamento),
            Status = d.Status,
            StatusDescricao = GetStatusDescricao(d.Status),
            Provider = d.Provider,
            ReciboToken = d.ReciboToken,
            ReciboDisponivel = d.Status == StatusDoacaoOnline.Confirmada && d.DataConfirmacao.HasValue,
            PixCopiaECola = d.PixCopiaECola,
            PixQrCodeUrl = d.PixQrCodeUrl,
            DataVencimento = d.DataVencimento,
            DataConfirmacao = d.DataConfirmacao,
            ReceitaId = d.ReceitaId,
            DataCriacao = d.DataCriacao,
        };
    }

    private static DoacaoReciboDto MapReciboToDto(DoacaoOnline d)
    {
        return new DoacaoReciboDto
        {
            Token = d.ReciboToken ?? string.Empty,
            DoacaoId = d.Id,
            FinalidadeNome = d.FinalidadeDoacao?.Nome ?? "Doação",
            NomeDoador = d.Anonima ? "Doador anônimo" : d.NomeDoador,
            Anonima = d.Anonima,
            Valor = d.Valor,
            MetodoPagamento = d.MetodoPagamento,
            MetodoPagamentoDescricao = GetMetodoDescricao(d.MetodoPagamento),
            Status = d.Status,
            StatusDescricao = GetStatusDescricao(d.Status),
            DataCriacao = d.DataCriacao,
            DataConfirmacao = d.DataConfirmacao ?? DateTime.Now,
            ReceitaId = d.ReceitaId,
        };
    }

    private static GivingProviderConfigDto MapConfigToDto(GivingProviderConfig config)
    {
        return new GivingProviderConfigDto
        {
            Id = config.Id,
            Provider = config.Provider,
            ProviderDescricao = GetProviderDescricao(config.Provider),
            Environment = config.Environment,
            EnvironmentDescricao = GetEnvironmentDescricao(config.Environment),
            Configurado = !string.IsNullOrWhiteSpace(config.ApiKeyProtegida),
            ApiKeyUltimosDigitos = config.ApiKeyUltimosDigitos,
            WebhookUrl = config.WebhookUrl,
            PixEnabled = config.PixEnabled,
            CreditCardEnabled = config.CreditCardEnabled,
            BoletoEnabled = config.BoletoEnabled,
            Ativo = config.Ativo,
            DataCriacao = config.DataCriacao,
            DataAtualizacao = config.DataAtualizacao,
        };
    }

    private static string SerializeValores(IEnumerable<decimal> valores)
    {
        return string.Join(",", valores.Where(v => v > 0).Distinct().OrderBy(v => v).Select(v => v.ToString(CultureInfo.InvariantCulture)));
    }

    private static decimal[] ParseValores(string? valores)
    {
        if (string.IsNullOrWhiteSpace(valores)) return [];

        return valores
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0)
            .Where(v => v > 0)
            .ToArray();
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        var slug = Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "doacao" : slug;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string GenerateReciboToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string GetMetodoDescricao(MetodoPagamentoDoacao metodo)
    {
        return metodo switch
        {
            MetodoPagamentoDoacao.Pix => "Pix",
            MetodoPagamentoDoacao.CartaoCredito => "Cartão de crédito",
            MetodoPagamentoDoacao.Boleto => "Boleto",
            _ => metodo.ToString()
        };
    }

    private static string GetStatusDescricao(StatusDoacaoOnline status)
    {
        return status switch
        {
            StatusDoacaoOnline.Pendente => "Pendente",
            StatusDoacaoOnline.AguardandoPagamento => "Aguardando pagamento",
            StatusDoacaoOnline.Confirmada => "Confirmada",
            StatusDoacaoOnline.Expirada => "Expirada",
            StatusDoacaoOnline.Cancelada => "Cancelada",
            StatusDoacaoOnline.Falhou => "Falhou",
            StatusDoacaoOnline.Estornada => "Estornada",
            _ => status.ToString()
        };
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool IsConfirmedAsaasEvent(string eventName)
    {
        return eventName is "PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED";
    }

    private static bool IsConfirmedAsaasStatus(string status)
    {
        return status is "RECEIVED" or "CONFIRMED" or "RECEIVED_IN_CASH";
    }

    private static bool IsFailureAsaasEvent(string eventName)
    {
        return eventName is "PAYMENT_REFUNDED" or "PAYMENT_CREDIT_CARD_CAPTURE_REFUSED" or "PAYMENT_REPROVED_BY_RISK_ANALYSIS" or "PAYMENT_DELETED";
    }

    private static bool IsFailureAsaasStatus(string status)
    {
        return status is "REFUNDED" or "CANCELLED" or "DELETED";
    }

    private static string MaskSecret(string value)
    {
        if (value.Length <= 4) return "****";
        return $"****{value[^4..]}";
    }

    private static string GetProviderDescricao(GivingProvider provider)
    {
        return provider switch
        {
            GivingProvider.Asaas => "Asaas",
            _ => provider.ToString()
        };
    }

    private static string GetEnvironmentDescricao(GivingProviderEnvironment environment)
    {
        return environment switch
        {
            GivingProviderEnvironment.Sandbox => "Sandbox",
            GivingProviderEnvironment.Production => "Produção",
            _ => environment.ToString()
        };
    }
}
