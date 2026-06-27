using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface ICentroCustoService
{
    Task<IEnumerable<CentroCustoDto>> GetAllAsync();
    Task<CentroCustoDto?> GetByIdAsync(int id);
    Task<CentroCustoDto> CreateAsync(CriarCentroCustoDto dto);
    Task<CentroCustoDto> UpdateAsync(int id, AtualizarCentroCustoDto dto);
    Task DeleteAsync(int id);
}

public class CentroCustoService : ICentroCustoService
{
    private readonly ICentroCustoRepository _repository;

    public CentroCustoService(ICentroCustoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CentroCustoDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<CentroCustoDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<CentroCustoDto> CreateAsync(CriarCentroCustoDto dto)
    {
        var entity = new CentroCusto
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Ativo = true,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<CentroCustoDto> UpdateAsync(int id, AtualizarCentroCustoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Centro de custo não encontrado");

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

    private static CentroCustoDto MapToDto(CentroCusto c)
    {
        return new CentroCustoDto
        {
            Id = c.Id,
            Nome = c.Nome,
            Descricao = c.Descricao,
            Ativo = c.Ativo,
            DataCriacao = c.DataCriacao,
        };
    }
}
