using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IEnqueteRepository
{
    Task<IEnumerable<Enquete>> GetAllAsync();
    Task<Enquete?> GetByIdAsync(int id);
    Task<IEnumerable<Enquete>> GetAtivasAsync();
    Task<Enquete> CreateAsync(Enquete enquete);
    Task<Enquete> UpdateAsync(Enquete enquete);
    Task DeleteAsync(int id);
    Task<EnqueteOpcao?> GetOpcaoByIdAsync(int id);
    Task<EnqueteOpcao> CreateOpcaoAsync(EnqueteOpcao opcao);
    Task<EnqueteOpcao> UpdateOpcaoAsync(EnqueteOpcao opcao);
    Task DeleteOpcaoAsync(int id);
    Task<EnqueteVoto> CreateVotoAsync(EnqueteVoto voto);
    Task<bool> UsuarioJaVotouAsync(int enqueteId, int? usuarioId);
    Task<IEnumerable<EnqueteVoto>> GetVotosPorEnqueteAsync(int enqueteId);
}
