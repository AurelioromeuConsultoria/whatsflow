using WhatsFlow.Application.DTOs;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IComunicacaoCampanhaRepository
{
    Task<(IReadOnlyList<ComunicacaoCampanha> Items, int Total)> GetPagedAsync(ComunicacaoCampanhaPagedQueryDto query);
    Task<ComunicacaoCampanha?> GetByIdAsync(int id);
    Task<ComunicacaoCampanha> CreateAsync(ComunicacaoCampanha campanha);
    Task<ComunicacaoCampanha> UpdateAsync(ComunicacaoCampanha campanha);
    Task AtualizarStatusPorEntregasAsync(int campanhaId);
    Task<ComunicacaoStatsDto> GetStatsAsync();
}
