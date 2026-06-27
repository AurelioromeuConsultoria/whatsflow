using WhatsFlow.Application.DTOs;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IComunicacaoEntregaRepository
{
    Task<(IReadOnlyList<ComunicacaoEntrega> Items, int Total)> GetPagedAsync(ComunicacaoEntregaPagedQueryDto query);
    Task<IReadOnlyList<ComunicacaoEntrega>> GetByCampanhaIdAsync(int campanhaId);
    Task<ComunicacaoEntrega?> GetByIdAsync(int id);
    Task<ComunicacaoEntrega> CreateAsync(ComunicacaoEntrega entrega);
    Task<IReadOnlyList<ComunicacaoEntrega>> CreateManyAsync(IEnumerable<ComunicacaoEntrega> entregas);
    Task<ComunicacaoEntrega> UpdateAsync(ComunicacaoEntrega entrega);
    Task<IReadOnlyList<ComunicacaoEntrega>> ReservarPendentesAsync(int limit);
}
