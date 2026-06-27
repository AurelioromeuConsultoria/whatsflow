using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface ICategoriaDespesaService
{
    Task<IEnumerable<CategoriaDespesaDto>> GetAllAsync();
    Task<CategoriaDespesaDto?> GetByIdAsync(int id);
    Task<CategoriaDespesaDto> CreateAsync(CriarCategoriaDespesaDto dto);
    Task<CategoriaDespesaDto> UpdateAsync(int id, AtualizarCategoriaDespesaDto dto);
    Task DeleteAsync(int id);
}

public class CategoriaDespesaService : ICategoriaDespesaService
{
    private readonly ICategoriaDespesaRepository _repository;

    public CategoriaDespesaService(ICategoriaDespesaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CategoriaDespesaDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<CategoriaDespesaDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<CategoriaDespesaDto> CreateAsync(CriarCategoriaDespesaDto dto)
    {
        var entity = new CategoriaDespesa
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Ativo = true,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<CategoriaDespesaDto> UpdateAsync(int id, AtualizarCategoriaDespesaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Categoria de despesa não encontrada");

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

    private static CategoriaDespesaDto MapToDto(CategoriaDespesa c)
    {
        return new CategoriaDespesaDto
        {
            Id = c.Id,
            Nome = c.Nome,
            Descricao = c.Descricao,
            Ativo = c.Ativo,
            DataCriacao = c.DataCriacao,
        };
    }
}
