using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IComunicacaoTemplateRepository
{
    Task<IReadOnlyList<ComunicacaoTemplate>> GetAllAsync();
    Task<ComunicacaoTemplate?> GetByIdAsync(int id);
    Task<ComunicacaoTemplate> CreateAsync(ComunicacaoTemplate template);
    Task<ComunicacaoTemplate> UpdateAsync(ComunicacaoTemplate template);
}
