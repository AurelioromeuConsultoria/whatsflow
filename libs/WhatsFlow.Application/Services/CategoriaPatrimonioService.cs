using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface ICategoriaPatrimonioService
{
    Task<IEnumerable<CategoriaPatrimonioDto>> GetAllAsync();
    Task<CategoriaPatrimonioDto?> GetByIdAsync(int id);
    Task<CategoriaPatrimonioDto> CreateAsync(CriarCategoriaPatrimonioDto dto);
    Task<CategoriaPatrimonioDto> UpdateAsync(int id, AtualizarCategoriaPatrimonioDto dto);
    Task DeleteAsync(int id);
}

public class CategoriaPatrimonioService : ICategoriaPatrimonioService
{
    private readonly ICategoriaPatrimonioRepository _repository;

    public CategoriaPatrimonioService(ICategoriaPatrimonioRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CategoriaPatrimonioDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<CategoriaPatrimonioDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<CategoriaPatrimonioDto> CreateAsync(CriarCategoriaPatrimonioDto dto)
    {
        await ValidarNomeDuplicadoAsync(dto.Nome);

        var entity = new CategoriaPatrimonio
        {
            Nome = dto.Nome.Trim(),
            Descricao = dto.Descricao?.Trim(),
            Ativo = true,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<CategoriaPatrimonioDto> UpdateAsync(int id, AtualizarCategoriaPatrimonioDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Categoria de patrimônio não encontrada");

        await ValidarNomeDuplicadoAsync(dto.Nome, id);

        entity.Nome = dto.Nome.Trim();
        entity.Descricao = dto.Descricao?.Trim();
        entity.Ativo = dto.Ativo;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private async Task ValidarNomeDuplicadoAsync(string nome, int? idAtual = null)
    {
        var existente = await _repository.GetByNomeAsync(nome.Trim());
        if (existente != null && existente.Id != idAtual)
        {
            throw new InvalidOperationException("Já existe uma categoria de patrimônio com este nome");
        }
    }

    private static CategoriaPatrimonioDto MapToDto(CategoriaPatrimonio item)
    {
        return new CategoriaPatrimonioDto
        {
            Id = item.Id,
            Nome = item.Nome,
            Descricao = item.Descricao,
            Ativo = item.Ativo,
            DataCriacao = item.DataCriacao,
        };
    }
}
