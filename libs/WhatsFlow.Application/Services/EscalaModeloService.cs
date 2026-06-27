using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IEscalaModeloService
{
    Task<EscalaModeloDto?> GetByIdAsync(int id);
    Task<EscalaModeloDto?> GetByEventoAndEquipeAsync(int? eventoId, int equipeId);
    Task<IEnumerable<EscalaModeloDto>> GetByEquipeAsync(int equipeId);
    Task<IEnumerable<EscalaModeloDto>> GetByEventoAsync(int eventoId);
    Task<EscalaModeloDto> CreateAsync(CriarEscalaModeloDto dto);
    Task<EscalaModeloDto> UpdateAsync(int id, AtualizarEscalaModeloDto dto);
    Task DeleteAsync(int id);
}

public class EscalaModeloService : IEscalaModeloService
{
    private readonly IEscalaModeloRepository _repository;
    private readonly IEquipeRepository _equipeRepository;
    private readonly IEventoRepository _eventoRepository;
    private readonly ITenantContext _tenantContext;

    public EscalaModeloService(
        IEscalaModeloRepository repository,
        IEquipeRepository equipeRepository,
        IEventoRepository eventoRepository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _equipeRepository = equipeRepository;
        _eventoRepository = eventoRepository;
        _tenantContext = tenantContext;
    }

    public EscalaModeloService(
        IEscalaModeloRepository repository,
        IEquipeRepository equipeRepository,
        IEventoRepository eventoRepository)
        : this(repository, equipeRepository, eventoRepository, new DefaultTenantContext())
    {
    }

    public async Task<EscalaModeloDto?> GetByIdAsync(int id)
    {
        var modelo = await _repository.GetByIdAsync(id);
        return modelo != null ? MapToDto(modelo) : null;
    }

    public async Task<EscalaModeloDto?> GetByEventoAndEquipeAsync(int? eventoId, int equipeId)
    {
        var modelo = await _repository.GetByEventoAndEquipeAsync(eventoId, equipeId);
        return modelo != null ? MapToDto(modelo) : null;
    }

    public async Task<IEnumerable<EscalaModeloDto>> GetByEquipeAsync(int equipeId)
    {
        var modelos = await _repository.GetByEquipeAsync(equipeId);
        return modelos.Select(MapToDto);
    }

    public async Task<IEnumerable<EscalaModeloDto>> GetByEventoAsync(int eventoId)
    {
        var modelos = await _repository.GetByEventoAsync(eventoId);
        return modelos.Select(MapToDto);
    }

    public async Task<EscalaModeloDto> CreateAsync(CriarEscalaModeloDto dto)
    {
        var equipe = await _equipeRepository.GetByIdAsync(dto.EquipeId);
        if (equipe == null) throw new ArgumentException("Equipe não encontrada");

        if (dto.EventoId.HasValue)
        {
            var evento = await _eventoRepository.GetByIdAsync(dto.EventoId.Value);
            if (evento == null) throw new ArgumentException("Evento não encontrado");
        }

        var existente = await _repository.GetByEventoAndEquipeAsync(dto.EventoId, dto.EquipeId);
        if (existente != null)
            throw new ArgumentException("Já existe um modelo de escala para este evento e equipe.");

        var modelo = new EscalaModelo
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EventoId = dto.EventoId,
            EquipeId = dto.EquipeId,
            Nome = dto.Nome?.Trim(),
            DiasFolgaAposEscala = dto.DiasFolgaAposEscala,
            Ativo = dto.Ativo,
            DataCriacao = DateTime.Now
        };

        var ordem = 0;
        foreach (var item in dto.Itens.OrderBy(i => i.Ordem).ThenBy(_ => ordem++))
        {
            modelo.Itens.Add(new EscalaModeloItem
            {
                TenantId = modelo.TenantId,
                CargoId = item.CargoId,
                Quantidade = Math.Max(1, item.Quantidade),
                Ordem = item.Ordem,
                DataCriacao = DateTime.Now
            });
        }

        var created = await _repository.CreateAsync(modelo);
        var full = await _repository.GetByIdAsync(created.Id);
        return MapToDto(full!);
    }

    public async Task<EscalaModeloDto> UpdateAsync(int id, AtualizarEscalaModeloDto dto)
    {
        var modelo = await _repository.GetByIdAsync(id);
        if (modelo == null) throw new ArgumentException("Modelo de escala não encontrado");

        modelo.Nome = dto.Nome?.Trim();
        modelo.DiasFolgaAposEscala = dto.DiasFolgaAposEscala;
        modelo.Ativo = dto.Ativo;

        if (dto.Itens != null)
        {
            modelo.Itens.Clear();
            var ordem = 0;
            foreach (var item in dto.Itens.OrderBy(i => i.Ordem).ThenBy(_ => ordem++))
            {
                modelo.Itens.Add(new EscalaModeloItem
                {
                    TenantId = modelo.TenantId,
                    EscalaModeloId = modelo.Id,
                    CargoId = item.CargoId,
                    Quantidade = Math.Max(1, item.Quantidade),
                    Ordem = item.Ordem,
                    DataCriacao = DateTime.Now
                });
            }
        }

        await _repository.UpdateAsync(modelo);
        var full = await _repository.GetByIdAsync(id);
        return MapToDto(full!);
    }

    public async Task DeleteAsync(int id)
    {
        var modelo = await _repository.GetByIdAsync(id);
        if (modelo == null) throw new ArgumentException("Modelo de escala não encontrado");
        await _repository.DeleteAsync(id);
    }

    private static EscalaModeloDto MapToDto(EscalaModelo m)
    {
        return new EscalaModeloDto
        {
            Id = m.Id,
            EventoId = m.EventoId,
            EventoNome = m.Evento?.Titulo,
            EquipeId = m.EquipeId,
            EquipeNome = m.Equipe?.Nome ?? string.Empty,
            Nome = m.Nome,
            DiasFolgaAposEscala = m.DiasFolgaAposEscala,
            Ativo = m.Ativo,
            DataCriacao = m.DataCriacao,
            Itens = m.Itens
                .OrderBy(i => i.Ordem)
                .ThenBy(i => i.Id)
                .Select(i => new EscalaModeloItemDto
                {
                    Id = i.Id,
                    EscalaModeloId = i.EscalaModeloId,
                    CargoId = i.CargoId,
                    CargoNome = i.Cargo?.Nome,
                    Quantidade = i.Quantidade,
                    Ordem = i.Ordem
                })
                .ToList()
        };
    }
}
