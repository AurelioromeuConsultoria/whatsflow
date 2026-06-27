using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

public interface ICategoriaMidiaService
{
    Task<IEnumerable<CategoriaMidiaDto>> GetAllAsync();
    Task<CategoriaMidiaDto?> GetByIdAsync(int id);
    Task<CategoriaMidiaDto> CreateAsync(CriarCategoriaMidiaDto dto);
    Task<CategoriaMidiaDto> UpdateAsync(int id, AtualizarCategoriaMidiaDto dto);
    Task<bool> DeleteAsync(int id);
}





