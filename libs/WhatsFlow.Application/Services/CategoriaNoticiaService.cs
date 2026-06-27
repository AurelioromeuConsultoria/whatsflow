using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface ICategoriaNoticiaService
{
    Task<IEnumerable<CategoriaNoticiaDto>> GetAllAsync();
    Task<CategoriaNoticiaDto?> GetByIdAsync(int id);
    Task<CategoriaNoticiaDto> CreateAsync(CriarCategoriaNoticiaDto dto);
    Task<CategoriaNoticiaDto> UpdateAsync(int id, AtualizarCategoriaNoticiaDto dto);
    Task DeleteAsync(int id);
}

public class CategoriaNoticiaService : ICategoriaNoticiaService
{
    private readonly ICategoriaNoticiaRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CategoriaNoticiaService(ICategoriaNoticiaRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public CategoriaNoticiaService(ICategoriaNoticiaRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<CategoriaNoticiaDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<CategoriaNoticiaDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<CategoriaNoticiaDto> CreateAsync(CriarCategoriaNoticiaDto dto)
    {
        var entity = new CategoriaNoticia
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<CategoriaNoticiaDto> UpdateAsync(int id, AtualizarCategoriaNoticiaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Categoria não encontrada");

        entity.Nome = dto.Nome;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static CategoriaNoticiaDto MapToDto(CategoriaNoticia c)
    {
        return new CategoriaNoticiaDto
        {
            Id = c.Id,
            Nome = c.Nome,
            DataCriacao = c.DataCriacao
        };
    }
}


