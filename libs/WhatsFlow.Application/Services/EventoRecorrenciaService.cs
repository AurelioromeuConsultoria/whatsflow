using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IEventoRecorrenciaService
{
    Task<IEnumerable<EventoRecorrenciaDto>> GetByEventoAsync(int eventoId);
    Task<EventoRecorrenciaDto?> GetByIdAsync(int id);
    Task<EventoRecorrenciaDto> CreateAsync(CriarEventoRecorrenciaDto dto);
    Task<EventoRecorrenciaDto> UpdateAsync(int id, AtualizarEventoRecorrenciaDto dto);
    Task DeleteAsync(int id);
}

public class EventoRecorrenciaService : IEventoRecorrenciaService
{
    private readonly IEventoRecorrenciaRepository _repository;
    private readonly IEventoRepository _eventoRepository;
    private readonly ITenantContext _tenantContext;

    public EventoRecorrenciaService(
        IEventoRecorrenciaRepository repository,
        IEventoRepository eventoRepository)
        : this(repository, eventoRepository, new DefaultTenantContext())
    {
    }

    public EventoRecorrenciaService(
        IEventoRecorrenciaRepository repository,
        IEventoRepository eventoRepository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _eventoRepository = eventoRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<EventoRecorrenciaDto>> GetByEventoAsync(int eventoId)
    {
        var evento = await _eventoRepository.GetByIdAsync(eventoId);
        if (evento == null) throw new ArgumentException("Evento não encontrado");

        var items = await _repository.GetByEventoAsync(eventoId);
        return items.Select(MapToDto);
    }

    public async Task<EventoRecorrenciaDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<EventoRecorrenciaDto> CreateAsync(CriarEventoRecorrenciaDto dto)
    {
        var evento = await _eventoRepository.GetByIdAsync(dto.EventoId);
        if (evento == null) throw new ArgumentException("Evento não encontrado");

        var horaInicio = ParseTimeSpan(dto.HoraInicio, nameof(dto.HoraInicio));
        TimeSpan? horaFim = null;
        if (!string.IsNullOrWhiteSpace(dto.HoraFim))
            horaFim = ParseTimeSpan(dto.HoraFim!, nameof(dto.HoraFim));

        if (horaFim.HasValue && horaFim.Value <= horaInicio)
            throw new ArgumentException("Hora fim deve ser maior que hora início");

        var diaSemana = ParseDayOfWeek(dto.DiaSemana);
        var periodicidade = ParsePeriodicidade(dto.Periodicidade);

        if (dto.DataFimVigencia.HasValue && dto.DataFimVigencia.Value.Date < dto.DataInicioVigencia.Date)
            throw new ArgumentException("Data fim da vigência não pode ser anterior à data início");

        var entity = new EventoRecorrencia
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EventoId = dto.EventoId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFim = horaFim,
            Periodicidade = periodicidade,
            DataInicioVigencia = dto.DataInicioVigencia.Date,
            DataFimVigencia = dto.DataFimVigencia?.Date,
            Ativo = dto.Ativo,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<EventoRecorrenciaDto> UpdateAsync(int id, AtualizarEventoRecorrenciaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Recorrência não encontrada");

        var horaInicio = ParseTimeSpan(dto.HoraInicio, nameof(dto.HoraInicio));
        TimeSpan? horaFim = null;
        if (!string.IsNullOrWhiteSpace(dto.HoraFim))
            horaFim = ParseTimeSpan(dto.HoraFim!, nameof(dto.HoraFim));

        if (horaFim.HasValue && horaFim.Value <= horaInicio)
            throw new ArgumentException("Hora fim deve ser maior que hora início");

        if (dto.DataFimVigencia.HasValue && dto.DataFimVigencia.Value.Date < dto.DataInicioVigencia.Date)
            throw new ArgumentException("Data fim da vigência não pode ser anterior à data início");

        entity.DiaSemana = ParseDayOfWeek(dto.DiaSemana);
        entity.HoraInicio = horaInicio;
        entity.HoraFim = horaFim;
        entity.Periodicidade = ParsePeriodicidade(dto.Periodicidade);
        entity.DataInicioVigencia = dto.DataInicioVigencia.Date;
        entity.DataFimVigencia = dto.DataFimVigencia?.Date;
        entity.Ativo = dto.Ativo;

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return;
        await _repository.DeleteAsync(id);
    }

    private static TimeSpan ParseTimeSpan(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} é obrigatório");
        value = value.Trim();
        if (value.Length == 5 && value[2] == ':')
            value += ":00";
        if (!TimeSpan.TryParse(value, out var ts))
            throw new ArgumentException($"{fieldName} inválido. Use formato HH:mm (ex: 10:00)");
        return ts;
    }

    private static DayOfWeek ParseDayOfWeek(int value)
    {
        if (value < 0 || value > 6)
            throw new ArgumentException("Dia da semana inválido. Use 0=Domingo a 6=Sábado");
        return (DayOfWeek)value;
    }

    private static PeriodicidadeRecorrencia ParsePeriodicidade(int value)
    {
        if (!Enum.IsDefined(typeof(PeriodicidadeRecorrencia), value))
            throw new ArgumentException("Periodicidade inválida. Use 1=Semanal, 2=Quinzenal, 3=Mensal");
        return (PeriodicidadeRecorrencia)value;
    }

    private static string FormatTimeSpan(TimeSpan ts) => ts.ToString(@"hh\:mm");

    private static EventoRecorrenciaDto MapToDto(EventoRecorrencia r)
    {
        return new EventoRecorrenciaDto
        {
            Id = r.Id,
            EventoId = r.EventoId,
            DiaSemana = (int)r.DiaSemana,
            DiaSemanaDescricao = r.DiaSemana switch
            {
                DayOfWeek.Sunday => "Domingo",
                DayOfWeek.Monday => "Segunda-feira",
                DayOfWeek.Tuesday => "Terça-feira",
                DayOfWeek.Wednesday => "Quarta-feira",
                DayOfWeek.Thursday => "Quinta-feira",
                DayOfWeek.Friday => "Sexta-feira",
                DayOfWeek.Saturday => "Sábado",
                _ => r.DiaSemana.ToString()
            },
            HoraInicio = FormatTimeSpan(r.HoraInicio),
            HoraFim = r.HoraFim.HasValue ? FormatTimeSpan(r.HoraFim.Value) : null,
            Periodicidade = (int)r.Periodicidade,
            PeriodicidadeDescricao = r.Periodicidade switch
            {
                PeriodicidadeRecorrencia.Semanal => "Semanal",
                PeriodicidadeRecorrencia.Quinzenal => "Quinzenal",
                PeriodicidadeRecorrencia.Mensal => "Mensal",
                _ => r.Periodicidade.ToString()
            },
            DataInicioVigencia = r.DataInicioVigencia,
            DataFimVigencia = r.DataFimVigencia,
            Ativo = r.Ativo,
            DataCriacao = r.DataCriacao
        };
    }
}
