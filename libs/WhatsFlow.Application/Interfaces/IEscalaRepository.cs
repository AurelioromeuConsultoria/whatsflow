using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IEscalaRepository
{
    Task<Escala?> GetByIdAsync(int id);
    Task<Escala?> GetByEventoOcorrenciaIdAsync(int eventoOcorrenciaId);
    Task<Escala?> GetByEventoOcorrenciaAndEquipeAsync(int eventoOcorrenciaId, int equipeId);
    Task<IEnumerable<Escala>> GetAllByEventoOcorrenciaAsync(int eventoOcorrenciaId);
    Task<IEnumerable<Escala>> GetByPessoaIdAsync(int pessoaId, bool somenteFuturas = false);
    Task<Escala> CreateAsync(Escala escala);
    Task<Escala> UpdateAsync(Escala escala);
    Task DeleteAsync(int id);

    Task<EscalaItem?> GetItemByIdAsync(int escalaItemId);
    Task<EscalaItem> AddItemAsync(EscalaItem item);
    Task<EscalaItem> UpdateItemAsync(EscalaItem item);
    Task DeleteItemAsync(int escalaItemId);
    Task<IEnumerable<EscalaItem>> GetItensComOcorrenciaNoPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? equipeId = null, int? eventoId = null);

    Task<EscalaItem?> GetConflitoPessoaNaEscalaAsync(int escalaId, int voluntarioId, int? ignorarEscalaItemId = null);
    Task<HashSet<int>> GetPessoaIdsJaEscaladasAsync(int escalaId);
    Task<Dictionary<int, int>> GetCargaRecentePorVoluntarioAsync(int equipeId, DateTime dataMinima);
    Task<Dictionary<int, int>> GetQuantidadeEscalasNoMesPorVoluntarioAsync(int equipeId, int ano, int mes);
    Task<Dictionary<int, int>> GetQuantidadeEscalasEmPeriodoPorVoluntarioAsync(int equipeId, DateTime dataInicio, DateTime dataFim);
}
