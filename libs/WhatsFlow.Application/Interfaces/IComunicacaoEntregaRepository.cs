using WhatsFlow.Application.DTOs;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IComunicacaoEntregaRepository
{
    Task<(IReadOnlyList<ComunicacaoEntrega> Items, int Total)> GetPagedAsync(ComunicacaoEntregaPagedQueryDto query);
    Task<IReadOnlyList<ComunicacaoEntrega>> GetByCampanhaIdAsync(int campanhaId);
    Task<ComunicacaoEntrega?> GetByIdAsync(int id);
    Task<ComunicacaoEntrega?> GetByProviderMessageIdAsync(string providerMessageId);
    Task<ComunicacaoEntrega> CreateAsync(ComunicacaoEntrega entrega);
    Task<IReadOnlyList<ComunicacaoEntrega>> CreateManyAsync(IEnumerable<ComunicacaoEntrega> entregas);
    Task<ComunicacaoEntrega> UpdateAsync(ComunicacaoEntrega entrega);
    Task<IReadOnlyList<ComunicacaoEntrega>> ReservarPendentesAsync(int limit);

    /// <summary>Total de entregas criadas (DataCriacao) no mês-calendário corrente para o tenant atual.</summary>
    Task<int> CountCriadasNoMesAsync(DateTime referencia);

    /// <summary>Cancela (Status=Cancelado) as entregas ainda Pendentes de uma campanha. Retorna o total afetado.</summary>
    Task<int> CancelarPendentesPorCampanhaAsync(int campanhaId);
}
