using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IInscricaoEventoRepository
{
    Task<IEnumerable<InscricaoEvento>> GetAllAsync();
    Task<InscricaoEvento?> GetByIdAsync(int id);
    Task<IEnumerable<InscricaoEvento>> GetByEventoAsync(int eventoId);
    Task<IEnumerable<InscricaoEvento>> GetByStatusAsync(StatusInscricao status);
    Task<IEnumerable<InscricaoEvento>> GetByEmailAsync(string email);
    Task<int> ContarInscricoesPorEventoAsync(int eventoId);
    Task<int> ContarInscricoesConfirmadasPorEventoAsync(int eventoId);
    Task<bool> ExisteInscricaoAsync(int eventoId, string whatsApp);
    Task<InscricaoEvento> CreateAsync(InscricaoEvento inscricaoEvento);
    Task<InscricaoEvento> UpdateAsync(InscricaoEvento inscricaoEvento);
    Task DeleteAsync(int id);
}





