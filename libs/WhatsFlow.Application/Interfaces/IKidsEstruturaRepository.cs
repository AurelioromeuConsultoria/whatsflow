using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IKidsEstruturaRepository
{
    Task<IEnumerable<KidsSala>> GetSalasAsync(bool incluirInativas = false);
    Task<KidsSala?> GetSalaByIdAsync(string id);
    Task<KidsSala> CreateSalaAsync(KidsSala sala);
    Task<KidsSala> UpdateSalaAsync(KidsSala sala);

    Task<IEnumerable<KidsTurma>> GetTurmasAsync(string? salaId = null, bool incluirInativas = false);
    Task<KidsTurma?> GetTurmaByIdAsync(string id);
    Task<KidsTurma> CreateTurmaAsync(KidsTurma turma);
    Task<KidsTurma> UpdateTurmaAsync(KidsTurma turma);
}
