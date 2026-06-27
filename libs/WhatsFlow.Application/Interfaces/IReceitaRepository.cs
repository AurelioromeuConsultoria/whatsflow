using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IReceitaRepository
{
    Task<IEnumerable<Receita>> GetAllAsync();
    Task<IEnumerable<Receita>> GetByPessoaIdAsync(int pessoaId);
    Task<IEnumerable<Receita>> GetContribuicoesNoPeriodoAsync(DateTime dataInicio, DateTime dataFim, int? categoriaId = null);
    Task<IEnumerable<Receita>> GetPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);
    Task<IEnumerable<Receita>> GetInformeAnualAsync(int pessoaId, int ano);
    Task<Receita?> GetByIdAsync(int id);
    Task<Receita> CreateAsync(Receita receita);
    Task<Receita> UpdateAsync(Receita receita);
    Task DeleteAsync(int id);
}
