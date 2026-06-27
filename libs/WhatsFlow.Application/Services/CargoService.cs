using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface ICargoService
{
    Task<IEnumerable<CargoDto>> GetAllAsync();
    Task<CargoDto?> GetByIdAsync(int id);
    Task<CargoDto> CreateAsync(CriarCargoDto dto);
    Task<CargoDto> UpdateAsync(int id, AtualizarCargoDto dto);
    Task DeleteAsync(int id);
}

public class CargoService : ICargoService
{
    private readonly ICargoRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CargoService(ICargoRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public CargoService(ICargoRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<CargoDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<CargoDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<CargoDto> CreateAsync(CriarCargoDto dto)
    {
        var entity = new Cargo
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<CargoDto> UpdateAsync(int id, AtualizarCargoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Cargo não encontrado");

        entity.Nome = dto.Nome;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static CargoDto MapToDto(Cargo c)
    {
        return new CargoDto
        {
            Id = c.Id,
            Nome = c.Nome,
            DataCriacao = c.DataCriacao
        };
    }
}
