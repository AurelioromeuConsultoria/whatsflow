using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IFornecedorService
{
    Task<IEnumerable<FornecedorDto>> GetAllAsync();
    Task<FornecedorDto?> GetByIdAsync(int id);
    Task<FornecedorDto> CreateAsync(CriarFornecedorDto dto);
    Task<FornecedorDto> UpdateAsync(int id, AtualizarFornecedorDto dto);
    Task DeleteAsync(int id);
}

public class FornecedorService : IFornecedorService
{
    private readonly IFornecedorRepository _repository;

    public FornecedorService(IFornecedorRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FornecedorDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<FornecedorDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<FornecedorDto> CreateAsync(CriarFornecedorDto dto)
    {
        var entity = new Fornecedor
        {
            Nome = dto.Nome,
            RazaoSocial = dto.RazaoSocial,
            CnpjCpf = dto.CnpjCpf,
            InscricaoEstadual = dto.InscricaoEstadual,
            Endereco = dto.Endereco,
            Telefone = dto.Telefone,
            Site = dto.Site,
            ContatoNome = dto.ContatoNome,
            ContatoCpf = dto.ContatoCpf,
            ContatoWhatsApp = dto.ContatoWhatsApp,
            ContatoEmail = dto.ContatoEmail,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<FornecedorDto> UpdateAsync(int id, AtualizarFornecedorDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Fornecedor não encontrado");

        entity.Nome = dto.Nome;
        entity.RazaoSocial = dto.RazaoSocial;
        entity.CnpjCpf = dto.CnpjCpf;
        entity.InscricaoEstadual = dto.InscricaoEstadual;
        entity.Endereco = dto.Endereco;
        entity.Telefone = dto.Telefone;
        entity.Site = dto.Site;
        entity.ContatoNome = dto.ContatoNome;
        entity.ContatoCpf = dto.ContatoCpf;
        entity.ContatoWhatsApp = dto.ContatoWhatsApp;
        entity.ContatoEmail = dto.ContatoEmail;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static FornecedorDto MapToDto(Fornecedor f)
    {
        return new FornecedorDto
        {
            Id = f.Id,
            Nome = f.Nome,
            RazaoSocial = f.RazaoSocial,
            CnpjCpf = f.CnpjCpf,
            InscricaoEstadual = f.InscricaoEstadual,
            Endereco = f.Endereco,
            Telefone = f.Telefone,
            Site = f.Site,
            ContatoNome = f.ContatoNome,
            ContatoCpf = f.ContatoCpf,
            ContatoWhatsApp = f.ContatoWhatsApp,
            ContatoEmail = f.ContatoEmail,
            DataCriacao = f.DataCriacao,
        };
    }
}
