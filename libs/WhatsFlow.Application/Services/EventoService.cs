using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IEventoService
{
    Task<IEnumerable<EventoDto>> GetAllAsync();
    Task<EventoDto?> GetByIdAsync(int id);
    Task<IEnumerable<EventoDto>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim);
    Task<EventoDto> CreateAsync(CriarEventoDto dto);
    Task<EventoDto> UpdateAsync(int id, AtualizarEventoDto dto);
    Task DeleteAsync(int id);
}

public class EventoService : IEventoService
{
    private readonly IEventoRepository _repository;
    private readonly ITenantContext _tenantContext;

    public EventoService(IEventoRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public EventoService(IEventoRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<EventoDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<EventoDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<IEnumerable<EventoDto>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim)
    {
        var entities = await _repository.GetByPeriodoAsync(dataInicio, dataFim);
        return entities.Select(MapToDto);
    }

    public async Task<EventoDto> CreateAsync(CriarEventoDto dto)
    {
        var tipo = Enum.IsDefined(typeof(TipoEvento), dto.Tipo) ? (TipoEvento)dto.Tipo : TipoEvento.Evento;
        var entity = new Evento
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Titulo = dto.Titulo,
            Descricao = dto.Descricao,
            ImagemDestaque = dto.ImagemDestaque,
            Url = dto.Url,
            DataInicio = dto.DataInicio,
            DataFim = dto.DataFim,
            Tipo = tipo,
            EhRecorrente = dto.EhRecorrente,
            Ativo = dto.Ativo,
            AceitaInscricoes = dto.AceitaInscricoes,
            ConfiguracaoFormularioInscricao = dto.ConfiguracaoFormularioInscricao,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<EventoDto> UpdateAsync(int id, AtualizarEventoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Evento não encontrado");

        entity.Titulo = dto.Titulo;
        entity.TenantId = _tenantContext.TenantId ?? entity.TenantId;
        entity.Descricao = dto.Descricao;
        entity.ImagemDestaque = dto.ImagemDestaque;
        entity.Url = dto.Url;
        entity.DataInicio = dto.DataInicio;
        entity.DataFim = dto.DataFim;
        entity.Tipo = Enum.IsDefined(typeof(TipoEvento), dto.Tipo) ? (TipoEvento)dto.Tipo : entity.Tipo;
        entity.EhRecorrente = dto.EhRecorrente;
        entity.Ativo = dto.Ativo;
        entity.AceitaInscricoes = dto.AceitaInscricoes;
        entity.ConfiguracaoFormularioInscricao = dto.ConfiguracaoFormularioInscricao;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static EventoDto MapToDto(Evento e)
    {
        return new EventoDto
        {
            Id = e.Id,
            Titulo = e.Titulo,
            Descricao = e.Descricao,
            ImagemDestaque = e.ImagemDestaque,
            Url = e.Url,
            DataInicio = e.DataInicio,
            DataFim = e.DataFim,
            Tipo = (int)e.Tipo,
            TipoDescricao = GetTipoDescricao(e.Tipo),
            EhRecorrente = e.EhRecorrente,
            Ativo = e.Ativo,
            AceitaInscricoes = e.AceitaInscricoes,
            ConfiguracaoFormularioInscricao = e.ConfiguracaoFormularioInscricao,
            DataCriacao = e.DataCriacao
        };
    }

    private static string GetTipoDescricao(TipoEvento tipo)
    {
        return tipo switch
        {
            TipoEvento.Evento => "Evento",
            TipoEvento.Culto => "Culto",
            TipoEvento.Reuniao => "Reunião",
            TipoEvento.Outro => "Outro",
            _ => tipo.ToString()
        };
    }
}


