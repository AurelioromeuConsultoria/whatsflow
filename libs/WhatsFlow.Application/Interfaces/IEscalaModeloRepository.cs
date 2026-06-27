using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IEscalaModeloRepository
{
    Task<EscalaModelo?> GetByIdAsync(int id);
    Task<EscalaModelo?> GetByEventoAndEquipeAsync(int? eventoId, int equipeId);
    Task<IEnumerable<EscalaModelo>> GetByEquipeAsync(int equipeId);
    Task<IEnumerable<EscalaModelo>> GetByEventoAsync(int eventoId);
    Task<EscalaModelo> CreateAsync(EscalaModelo modelo);
    Task<EscalaModelo> UpdateAsync(EscalaModelo modelo);
    Task DeleteAsync(int id);
}
