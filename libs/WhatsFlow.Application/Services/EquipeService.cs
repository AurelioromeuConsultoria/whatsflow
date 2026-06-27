using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IEquipeService
{
    Task<IEnumerable<EquipeDto>> GetAllAsync();
    Task<EquipeDto?> GetByIdAsync(int id);
    Task<EquipeDto> CreateAsync(CriarEquipeDto dto);
    Task<EquipeDto> UpdateAsync(int id, AtualizarEquipeDto dto);
    Task DeleteAsync(int id);
}

public class EquipeService : IEquipeService
{
    private readonly IEquipeRepository _repository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITenantContext _tenantContext;

    public EquipeService(IEquipeRepository repository, IUsuarioRepository usuarioRepository)
        : this(repository, usuarioRepository, new DefaultTenantContext())
    {
    }

    public EquipeService(IEquipeRepository repository, IUsuarioRepository usuarioRepository, ITenantContext tenantContext)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<EquipeDto>> GetAllAsync()
    {
        var equipes = await _repository.GetAllAsync();
        return equipes.Select(MapToDto);
    }

    public async Task<EquipeDto?> GetByIdAsync(int id)
    {
        var equipe = await _repository.GetByIdAsync(id);
        return equipe != null ? MapToDto(equipe) : null;
    }

    public async Task<EquipeDto> CreateAsync(CriarEquipeDto dto)
    {
        await ValidarLiderAsync(dto.LiderUsuarioId);

        var equipe = new Equipe
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            Area = (AreaEquipe)dto.Area,
            LiderUsuarioId = dto.LiderUsuarioId,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(equipe);
        return MapToDto(created);
    }

    public async Task<EquipeDto> UpdateAsync(int id, AtualizarEquipeDto dto)
    {
        var equipe = await _repository.GetByIdAsync(id);
        if (equipe == null) throw new ArgumentException("Equipe não encontrada");

        await ValidarLiderAsync(dto.LiderUsuarioId);

        equipe.Nome = dto.Nome;
        equipe.Area = (AreaEquipe)dto.Area;
        equipe.LiderUsuarioId = dto.LiderUsuarioId;

        var updated = await _repository.UpdateAsync(equipe);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static EquipeDto MapToDto(Equipe e)
    {
        return new EquipeDto
        {
            Id = e.Id,
            Nome = e.Nome,
            Area = (int)e.Area,
            LiderUsuarioId = e.LiderUsuarioId,
            LiderNome = e.LiderUsuario?.Pessoa?.Nome,
            QuantidadeMembros = e.Voluntarios?.Count ?? 0,
            DataCriacao = e.DataCriacao
        };
    }

    private async Task ValidarLiderAsync(int? liderUsuarioId)
    {
        if (!liderUsuarioId.HasValue)
        {
            return;
        }

        var usuario = await _usuarioRepository.GetByIdAsync(liderUsuarioId.Value);
        if (usuario == null || !usuario.Ativo)
        {
            throw new ArgumentException("Líder da equipe inválido");
        }
    }
}
