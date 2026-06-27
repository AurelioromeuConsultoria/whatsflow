using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IWhatsAppAccountService
{
    Task<IReadOnlyList<WhatsAppAccountDto>> GetAllAsync();
    Task<WhatsAppAccountDto?> GetByIdAsync(int id);
    Task<WhatsAppAccountDto> CreateAsync(CriarWhatsAppAccountDto dto);
    Task<WhatsAppAccountDto> UpdateAsync(int id, AtualizarWhatsAppAccountDto dto);
    Task DeleteAsync(int id);
}

public class WhatsAppAccountService : IWhatsAppAccountService
{
    private readonly IWhatsAppAccountRepository _repository;
    private readonly ISecretProtector _secretProtector;
    private readonly ITenantContext _tenantContext;

    public WhatsAppAccountService(IWhatsAppAccountRepository repository, ISecretProtector secretProtector)
        : this(repository, secretProtector, new DefaultTenantContext())
    {
    }

    public WhatsAppAccountService(
        IWhatsAppAccountRepository repository,
        ISecretProtector secretProtector,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _secretProtector = secretProtector;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<WhatsAppAccountDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<WhatsAppAccountDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<WhatsAppAccountDto> CreateAsync(CriarWhatsAppAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome da conta WhatsApp é obrigatório.");
        }

        var entity = new WhatsAppAccount
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome.Trim(),
            Provider = dto.Provider,
            PhoneNumberId = Normalize(dto.PhoneNumberId),
            BusinessAccountId = Normalize(dto.BusinessAccountId),
            AccessTokenProtegido = ProtectOrNull(dto.AccessToken),
            WebhookSecret = Normalize(dto.WebhookSecret),
            Status = dto.Status,
            ConfiguracoesJson = Normalize(dto.ConfiguracoesJson),
            CriadoEm = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<WhatsAppAccountDto> UpdateAsync(int id, AtualizarWhatsAppAccountDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new ArgumentException("Conta WhatsApp não encontrada");

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome da conta WhatsApp é obrigatório.");
        }

        entity.Nome = dto.Nome.Trim();
        entity.Provider = dto.Provider;
        entity.PhoneNumberId = Normalize(dto.PhoneNumberId);
        entity.BusinessAccountId = Normalize(dto.BusinessAccountId);
        entity.WebhookSecret = Normalize(dto.WebhookSecret);
        entity.Status = dto.Status;
        entity.ConfiguracoesJson = Normalize(dto.ConfiguracoesJson);
        entity.AtualizadoEm = DateTime.UtcNow;

        // null = manter token atual; vazio = remover; valor = substituir (protegido).
        if (dto.AccessToken != null)
        {
            entity.AccessTokenProtegido = ProtectOrNull(dto.AccessToken);
        }

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public Task DeleteAsync(int id)
    {
        return _repository.DeleteAsync(id);
    }

    private string? ProtectOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : _secretProtector.Protect(value.Trim());

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Nunca retorna AccessTokenProtegido nem WebhookSecret — apenas flags de presença.
    private static WhatsAppAccountDto MapToDto(WhatsAppAccount a)
    {
        return new WhatsAppAccountDto
        {
            Id = a.Id,
            Nome = a.Nome,
            Provider = a.Provider,
            PhoneNumberId = a.PhoneNumberId,
            BusinessAccountId = a.BusinessAccountId,
            PossuiAccessToken = !string.IsNullOrWhiteSpace(a.AccessTokenProtegido),
            PossuiWebhookSecret = !string.IsNullOrWhiteSpace(a.WebhookSecret),
            Status = a.Status,
            ConfiguracoesJson = a.ConfiguracoesJson,
            CriadoEm = a.CriadoEm,
            AtualizadoEm = a.AtualizadoEm
        };
    }
}
