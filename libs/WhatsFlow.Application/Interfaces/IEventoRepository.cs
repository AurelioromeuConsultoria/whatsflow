using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IEventoRepository
{
    Task<IEnumerable<Evento>> GetAllAsync();
    Task<Evento?> GetByIdAsync(int id);
    Task<IEnumerable<Evento>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim);
    Task<Evento> CreateAsync(Evento evento);
    Task<Evento> UpdateAsync(Evento evento);
    Task DeleteAsync(int id);
}



