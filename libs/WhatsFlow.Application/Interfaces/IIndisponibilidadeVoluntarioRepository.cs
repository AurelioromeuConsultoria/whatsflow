using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IIndisponibilidadeVoluntarioRepository
{
    Task<IndisponibilidadeVoluntario?> GetByIdAsync(int id);
    Task<IEnumerable<IndisponibilidadeVoluntario>> GetByVoluntarioAsync(int voluntarioId, DateTime? dataInicio = null, DateTime? dataFim = null);
    Task<HashSet<int>> GetVoluntarioIdsIndisponiveisNaDataAsync(IEnumerable<int> voluntarioIds, DateTime data);
    Task<IndisponibilidadeVoluntario> CreateAsync(IndisponibilidadeVoluntario indisponibilidade);
    Task DeleteAsync(int id);
}
