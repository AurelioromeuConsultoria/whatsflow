using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IProjetoService
{
    Task<IEnumerable<ProjetoDto>> GetAllAsync();
    Task<ProjetoDto?> GetByIdAsync(int id);
    Task<ProjetoDto> CreateAsync(CriarProjetoDto dto);
    Task<ProjetoDto> UpdateAsync(int id, AtualizarProjetoDto dto);
    Task DeleteAsync(int id);
}

public class ProjetoService : IProjetoService
{
    private readonly IProjetoRepository _repository;

    public ProjetoService(IProjetoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProjetoDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<ProjetoDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<ProjetoDto> CreateAsync(CriarProjetoDto dto)
    {
        var entity = new Projeto
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            DataInicio = dto.DataInicio,
            DataFim = dto.DataFim,
            Orcamento = dto.Orcamento,
            Ativo = true,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<ProjetoDto> UpdateAsync(int id, AtualizarProjetoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Projeto não encontrado");

        entity.Nome = dto.Nome;
        entity.Descricao = dto.Descricao;
        entity.DataInicio = dto.DataInicio;
        entity.DataFim = dto.DataFim;
        entity.Orcamento = dto.Orcamento;
        entity.Ativo = dto.Ativo;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static ProjetoDto MapToDto(Projeto p)
    {
        return new ProjetoDto
        {
            Id = p.Id,
            Nome = p.Nome,
            Descricao = p.Descricao,
            DataInicio = p.DataInicio,
            DataFim = p.DataFim,
            Orcamento = p.Orcamento,
            Ativo = p.Ativo,
            DataCriacao = p.DataCriacao,
        };
    }
}
