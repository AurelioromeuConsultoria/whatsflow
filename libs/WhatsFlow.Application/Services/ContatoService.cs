using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IContatoService
{
    Task<IEnumerable<ContatoDto>> GetAllAsync();
    Task<ContatoDto?> GetByIdAsync(int id);
    Task<ContatoDto> CreateAsync(CriarContatoDto dto);
    Task<ContatoDto> UpdateAsync(int id, AtualizarContatoDto dto);
    Task DeleteAsync(int id);
}

public class ContatoService : IContatoService
{
    private readonly IContatoRepository _repository;
    private readonly ITenantContext _tenantContext;

    public ContatoService(IContatoRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public ContatoService(IContatoRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<ContatoDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<ContatoDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<ContatoDto> CreateAsync(CriarContatoDto dto)
    {
        var entity = new Contato
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            WhatsApp = dto.WhatsApp,
            Email = dto.Email,
            Membro = dto.Membro,
            Mensagem = dto.Mensagem,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<ContatoDto> UpdateAsync(int id, AtualizarContatoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Contato não encontrado");

        entity.Nome = dto.Nome;
        entity.WhatsApp = dto.WhatsApp;
        entity.Email = dto.Email;
        entity.Membro = dto.Membro;
        entity.Mensagem = dto.Mensagem;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static ContatoDto MapToDto(Contato c)
    {
        return new ContatoDto
        {
            Id = c.Id,
            Nome = c.Nome,
            WhatsApp = c.WhatsApp,
            Email = c.Email,
            Membro = c.Membro,
            Mensagem = c.Mensagem,
            DataCriacao = c.DataCriacao
        };
    }
}





