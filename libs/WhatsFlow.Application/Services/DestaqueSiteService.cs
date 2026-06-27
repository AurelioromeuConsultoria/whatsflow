using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IDestaqueSiteService
{
    Task<IEnumerable<DestaqueSiteDto>> GetAllAsync();
    Task<DestaqueSiteDto?> GetByIdAsync(int id);
    Task<DestaqueSiteDto> CreateAsync(CriarDestaqueSiteDto dto);
    Task<DestaqueSiteDto> UpdateAsync(int id, AtualizarDestaqueSiteDto dto);
    Task DeleteAsync(int id);
}

public class DestaqueSiteService : IDestaqueSiteService
{
    private readonly IDestaqueSiteRepository _repository;
    private readonly ITenantContext _tenantContext;

    public DestaqueSiteService(IDestaqueSiteRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public DestaqueSiteService(IDestaqueSiteRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<DestaqueSiteDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<DestaqueSiteDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<DestaqueSiteDto> CreateAsync(CriarDestaqueSiteDto dto)
    {
        var entity = new DestaqueSite
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Texto = dto.Texto,
            Descricao = dto.Descricao,
            Url = dto.Url,
            Imagem = dto.Imagem,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<DestaqueSiteDto> UpdateAsync(int id, AtualizarDestaqueSiteDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Destaque não encontrado");

        entity.Texto = dto.Texto;
        entity.Descricao = dto.Descricao;
        entity.Url = dto.Url;
        entity.Imagem = dto.Imagem;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static DestaqueSiteDto MapToDto(DestaqueSite d)
    {
        return new DestaqueSiteDto
        {
            Id = d.Id,
            Texto = d.Texto,
            Descricao = d.Descricao,
            Url = d.Url,
            Imagem = d.Imagem,
            DataCriacao = d.DataCriacao
        };
    }
}


