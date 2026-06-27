using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IGaleriaFotoRepository
{
    Task<IEnumerable<GaleriaFoto>> GetAllAsync();
    Task<IEnumerable<GaleriaFoto>> GetAtivasAsync();
    Task<GaleriaFoto?> GetByIdAsync(int id);
    Task<IEnumerable<GaleriaFoto>> GetByEventoIdAsync(int eventoId);
    Task<IEnumerable<GaleriaFoto>> GetByCategoriaMidiaIdAsync(int categoriaMidiaId);
    Task<GaleriaFoto> CreateAsync(GaleriaFoto galeria);
    Task<GaleriaFoto> UpdateAsync(GaleriaFoto galeria);
    Task<bool> DeleteAsync(int id);
}





