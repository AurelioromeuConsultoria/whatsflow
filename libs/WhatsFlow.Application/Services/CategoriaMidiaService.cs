using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public class CategoriaMidiaService : ICategoriaMidiaService
{
    private readonly ICategoriaMidiaRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CategoriaMidiaService(ICategoriaMidiaRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public CategoriaMidiaService(ICategoriaMidiaRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<CategoriaMidiaDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<CategoriaMidiaDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<CategoriaMidiaDto> CreateAsync(CriarCategoriaMidiaDto dto)
    {
        var entity = new CategoriaMidia
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<CategoriaMidiaDto> UpdateAsync(int id, AtualizarCategoriaMidiaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Categoria não encontrada");

        entity.Nome = dto.Nome;
        entity.Descricao = dto.Descricao;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    private static CategoriaMidiaDto MapToDto(CategoriaMidia c)
    {
        return new CategoriaMidiaDto
        {
            Id = c.Id,
            Nome = c.Nome,
            Descricao = c.Descricao,
            DataCriacao = c.DataCriacao
        };
    }
}




