using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync();
    Task<TagDto?> GetByIdAsync(int id);
    Task<TagDto> CreateAsync(CriarTagDto dto);
    Task<TagDto> UpdateAsync(int id, AtualizarTagDto dto);
    Task DeleteAsync(int id);
}

public class TagService : ITagService
{
    private readonly ITagRepository _repository;
    private readonly ITenantContext _tenantContext;

    public TagService(ITagRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public TagService(ITagRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<TagDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<TagDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<TagDto> CreateAsync(CriarTagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome da tag é obrigatório.");
        }

        var nome = dto.Nome.Trim();
        if (await _repository.GetByNomeAsync(nome) != null)
        {
            throw new ArgumentException($"Já existe uma tag com o nome '{nome}'.");
        }

        var entity = new Tag
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = nome,
            Cor = string.IsNullOrWhiteSpace(dto.Cor) ? null : dto.Cor.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<TagDto> UpdateAsync(int id, AtualizarTagDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new ArgumentException("Tag não encontrada");

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome da tag é obrigatório.");
        }

        var nome = dto.Nome.Trim();
        if (await _repository.GetByNomeAsync(nome, ignoreId: id) != null)
        {
            throw new ArgumentException($"Já existe uma tag com o nome '{nome}'.");
        }

        entity.Nome = nome;
        entity.Cor = string.IsNullOrWhiteSpace(dto.Cor) ? null : dto.Cor.Trim();

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public Task DeleteAsync(int id)
    {
        return _repository.DeleteAsync(id);
    }

    private static TagDto MapToDto(Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Nome = tag.Nome,
            Cor = tag.Cor,
            CriadoEm = tag.CriadoEm,
            TotalContatos = tag.ContatoTags?.Count ?? 0
        };
    }
}
