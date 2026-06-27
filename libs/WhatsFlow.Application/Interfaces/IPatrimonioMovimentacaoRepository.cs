using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IPatrimonioMovimentacaoRepository
{
    Task<IEnumerable<PatrimonioMovimentacao>> GetByPatrimonioIdAsync(int patrimonioItemId);
    Task<PatrimonioMovimentacao> CreateAsync(PatrimonioMovimentacao movimentacao);
}
