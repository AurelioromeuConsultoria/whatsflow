using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IGaleriaFotoItemRepository
{
    Task<List<GaleriaFotoItem>> GetByGaleriaIdAsync(int galeriaId);
    Task AddRangeAsync(IEnumerable<GaleriaFotoItem> items);
    Task SetDestaqueAsync(int galeriaId, string nomeArquivoDestaque);
}
