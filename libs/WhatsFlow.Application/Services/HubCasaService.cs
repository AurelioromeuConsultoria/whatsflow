using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IHubCasaService
{
    Task<IEnumerable<HubCasaDto>> GetAllAsync();
    Task<HubCasaDto?> GetByIdAsync(int id);
    Task<HubCasaDto> CreateAsync(CriarHubCasaDto dto);
    Task<HubCasaDto> UpdateAsync(int id, AtualizarHubCasaDto dto);
    Task DeleteAsync(int id);
}

public class HubCasaService : IHubCasaService
{
    private readonly IHubCasaRepository _repository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITenantContext _tenantContext;

    public HubCasaService(IHubCasaRepository repository, IUsuarioRepository usuarioRepository)
        : this(repository, usuarioRepository, new DefaultTenantContext())
    {
    }

    public HubCasaService(IHubCasaRepository repository, IUsuarioRepository usuarioRepository, ITenantContext tenantContext)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<HubCasaDto>> GetAllAsync()
    {
        var casas = await _repository.GetAllAsync();
        return casas.Select(MapToDto);
    }

    public async Task<HubCasaDto?> GetByIdAsync(int id)
    {
        var casa = await _repository.GetByIdAsync(id);
        return casa != null ? MapToDto(casa) : null;
    }

    public async Task<HubCasaDto> CreateAsync(CriarHubCasaDto dto)
    {
        await ValidarUsuariosAsync(dto.AbertoPorId, dto.LiderId, dto.TimoteoId);

        var casa = new HubCasa
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            AbertoPorId = dto.AbertoPorId,
            LiderId = dto.LiderId,
            TimoteoId = dto.TimoteoId,
            EnderecoCompleto = dto.EnderecoCompleto,
            Anfitriao = dto.Anfitriao,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(casa);
        return MapToDto(created);
    }

    public async Task<HubCasaDto> UpdateAsync(int id, AtualizarHubCasaDto dto)
    {
        var casa = await _repository.GetByIdAsync(id);
        if (casa == null) throw new ArgumentException("Casa não encontrada");

        await ValidarUsuariosAsync(dto.AbertoPorId, dto.LiderId, dto.TimoteoId);

        casa.Nome = dto.Nome;
        casa.AbertoPorId = dto.AbertoPorId;
        casa.LiderId = dto.LiderId;
        casa.TimoteoId = dto.TimoteoId;
        casa.EnderecoCompleto = dto.EnderecoCompleto;
        casa.Anfitriao = dto.Anfitriao;

        var updated = await _repository.UpdateAsync(casa);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private async Task ValidarUsuariosAsync(int abertoPorId, int liderId, int timoteoId)
    {
        var abertoPor = await _usuarioRepository.GetByIdAsync(abertoPorId);
        if (abertoPor == null) throw new ArgumentException("Usuário (Aberto por) inválido");

        var lider = await _usuarioRepository.GetByIdAsync(liderId);
        if (lider == null) throw new ArgumentException("Usuário (Líder) inválido");

        var timoteo = await _usuarioRepository.GetByIdAsync(timoteoId);
        if (timoteo == null) throw new ArgumentException("Usuário (Timóteo) inválido");
    }

    private static HubCasaDto MapToDto(HubCasa c)
    {
        return new HubCasaDto
        {
            Id = c.Id,
            Nome = c.Nome,
            AbertoPorId = c.AbertoPorId,
            AbertoPorNome = c.AbertoPor?.Pessoa?.Nome ?? string.Empty,
            LiderId = c.LiderId,
            LiderNome = c.Lider?.Pessoa?.Nome ?? string.Empty,
            TimoteoId = c.TimoteoId,
            TimoteoNome = c.Timoteo?.Pessoa?.Nome ?? string.Empty,
            EnderecoCompleto = c.EnderecoCompleto,
            Anfitriao = c.Anfitriao,
            DataCriacao = c.DataCriacao,
        };
    }
}
