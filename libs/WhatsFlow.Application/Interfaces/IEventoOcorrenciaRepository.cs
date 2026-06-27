using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IEventoOcorrenciaRepository
{
    Task<IEnumerable<EventoOcorrencia>> GetByEventoAsync(int eventoId);
    Task<IEnumerable<EventoOcorrencia>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? eventoId = null);
    Task<EventoOcorrencia?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<EventoOcorrencia> CreateAsync(EventoOcorrencia ocorrencia);
    Task<EventoOcorrencia> UpdateAsync(EventoOcorrencia ocorrencia);
    Task DeleteAsync(int id);
    Task<IEnumerable<EventoRecorrencia>> GetRecorrenciasAtivasByEventoAsync(int eventoId);
    Task<bool> ExistsOcorrenciaNoHorarioAsync(int eventoId, DateTime dataHoraInicio);
}
