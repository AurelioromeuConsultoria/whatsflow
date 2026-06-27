using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IIndisponibilidadeVoluntarioService
{
    Task<IndisponibilidadeVoluntarioDto?> GetByIdAsync(int id);
    Task<IEnumerable<IndisponibilidadeVoluntarioDto>> GetByVoluntarioAsync(int voluntarioId, DateTime? dataInicio = null, DateTime? dataFim = null);
    Task<IndisponibilidadeVoluntarioDto> CreateAsync(CriarIndisponibilidadeVoluntarioDto dto);
    Task DeleteAsync(int id);
}

public class IndisponibilidadeVoluntarioService : IIndisponibilidadeVoluntarioService
{
    private readonly IIndisponibilidadeVoluntarioRepository _repository;
    private readonly IVoluntarioRepository _voluntarioRepository;
    private readonly ITenantContext _tenantContext;

    public IndisponibilidadeVoluntarioService(
        IIndisponibilidadeVoluntarioRepository repository,
        IVoluntarioRepository voluntarioRepository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _voluntarioRepository = voluntarioRepository;
        _tenantContext = tenantContext;
    }

    public IndisponibilidadeVoluntarioService(
        IIndisponibilidadeVoluntarioRepository repository,
        IVoluntarioRepository voluntarioRepository)
        : this(repository, voluntarioRepository, new DefaultTenantContext())
    {
    }

    public async Task<IndisponibilidadeVoluntarioDto?> GetByIdAsync(int id)
    {
        var ind = await _repository.GetByIdAsync(id);
        return ind != null ? MapToDto(ind) : null;
    }

    public async Task<IEnumerable<IndisponibilidadeVoluntarioDto>> GetByVoluntarioAsync(int voluntarioId, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var list = await _repository.GetByVoluntarioAsync(voluntarioId, dataInicio, dataFim);
        return list.Select(MapToDto);
    }

    public async Task<IndisponibilidadeVoluntarioDto> CreateAsync(CriarIndisponibilidadeVoluntarioDto dto)
    {
        var voluntario = await _voluntarioRepository.GetByIdAsync(dto.VoluntarioId);
        if (voluntario == null) throw new ArgumentException("Voluntário não encontrado");

        var indisponibilidade = new IndisponibilidadeVoluntario
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            VoluntarioId = dto.VoluntarioId,
            Data = dto.Data.Date,
            Motivo = dto.Motivo?.Trim(),
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(indisponibilidade);
        var full = await _repository.GetByIdAsync(created.Id);
        return MapToDto(full!);
    }

    public async Task DeleteAsync(int id)
    {
        var ind = await _repository.GetByIdAsync(id);
        if (ind == null) throw new ArgumentException("Indisponibilidade não encontrada");
        await _repository.DeleteAsync(id);
    }

    private static IndisponibilidadeVoluntarioDto MapToDto(IndisponibilidadeVoluntario i)
    {
        return new IndisponibilidadeVoluntarioDto
        {
            Id = i.Id,
            VoluntarioId = i.VoluntarioId,
            VoluntarioNome = i.Voluntario?.Pessoa?.Nome,
            Data = i.Data,
            Motivo = i.Motivo,
            DataCriacao = i.DataCriacao
        };
    }
}
