using WhatsFlow.Domain.Entities;
using WhatsFlow.Application.DTOs.Visitantes;

namespace WhatsFlow.Application.Interfaces;

public interface IVisitanteRepository
{
    Task<IEnumerable<Visitante>> GetAllAsync();
    Task<(IReadOnlyList<Visitante> Items, int Total)> GetPagedAsync(VisitantePagedQuery query);
    Task<Visitante?> GetByIdAsync(int id);
    Task<Visitante> CreateAsync(Visitante visitante);
    Task<Visitante> CreateWithoutSaveAsync(Visitante visitante); // Para uso em transações
    Task<Visitante> UpdateAsync(Visitante visitante);
    Task DeleteAsync(int id);
    Task<IEnumerable<Visitante>> GetVisitantesPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);
    Task<IEnumerable<Visitante>> GetVisitantesPorPessoaAsync(int pessoaId);
}

