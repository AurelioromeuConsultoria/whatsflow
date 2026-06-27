using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface ICategoriaReceitaService
{
    Task<IEnumerable<CategoriaReceitaDto>> GetAllAsync();
    Task<CategoriaReceitaDto?> GetByIdAsync(int id);
    Task<CategoriaReceitaDto> CreateAsync(CriarCategoriaReceitaDto dto);
    Task<CategoriaReceitaDto> UpdateAsync(int id, AtualizarCategoriaReceitaDto dto);
    Task DeleteAsync(int id);
}

public class CategoriaReceitaService : ICategoriaReceitaService
{
    private readonly ICategoriaReceitaRepository _repository;

    public CategoriaReceitaService(ICategoriaReceitaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CategoriaReceitaDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<CategoriaReceitaDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<CategoriaReceitaDto> CreateAsync(CriarCategoriaReceitaDto dto)
    {
        var entity = new CategoriaReceita
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Ativo = dto.Ativo,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<CategoriaReceitaDto> UpdateAsync(int id, AtualizarCategoriaReceitaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Categoria de receita não encontrada");

        entity.Nome = dto.Nome;
        entity.Descricao = dto.Descricao;
        entity.Ativo = dto.Ativo;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static CategoriaReceitaDto MapToDto(CategoriaReceita c)
    {
        return new CategoriaReceitaDto
        {
            Id = c.Id,
            Nome = c.Nome,
            Descricao = c.Descricao,
            Ativo = c.Ativo,
            DataCriacao = c.DataCriacao,
        };
    }
}
