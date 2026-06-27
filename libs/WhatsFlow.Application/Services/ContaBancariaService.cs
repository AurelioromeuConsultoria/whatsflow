using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IContaBancariaService
{
    Task<IEnumerable<ContaBancariaDto>> GetAllAsync();
    Task<ContaBancariaDto?> GetByIdAsync(int id);
    Task<ContaBancariaDto> CreateAsync(CriarContaBancariaDto dto);
    Task<ContaBancariaDto> UpdateAsync(int id, AtualizarContaBancariaDto dto);
    Task DeleteAsync(int id);
}

public class ContaBancariaService : IContaBancariaService
{
    private readonly IContaBancariaRepository _repository;

    public ContaBancariaService(IContaBancariaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ContaBancariaDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<ContaBancariaDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<ContaBancariaDto> CreateAsync(CriarContaBancariaDto dto)
    {
        var entity = new ContaBancaria
        {
            Nome = dto.Nome,
            Banco = dto.Banco,
            Agencia = dto.Agencia,
            Conta = dto.Conta,
            TipoConta = dto.TipoConta,
            SaldoInicial = dto.SaldoInicial,
            Ativo = true,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<ContaBancariaDto> UpdateAsync(int id, AtualizarContaBancariaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Conta bancária não encontrada");

        entity.Nome = dto.Nome;
        entity.Banco = dto.Banco;
        entity.Agencia = dto.Agencia;
        entity.Conta = dto.Conta;
        entity.TipoConta = dto.TipoConta;
        entity.SaldoInicial = dto.SaldoInicial;
        entity.Ativo = dto.Ativo;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static ContaBancariaDto MapToDto(ContaBancaria c)
    {
        return new ContaBancariaDto
        {
            Id = c.Id,
            Nome = c.Nome,
            Banco = c.Banco,
            Agencia = c.Agencia,
            Conta = c.Conta,
            TipoConta = c.TipoConta,
            SaldoInicial = c.SaldoInicial,
            Ativo = c.Ativo,
            DataCriacao = c.DataCriacao,
        };
    }
}
