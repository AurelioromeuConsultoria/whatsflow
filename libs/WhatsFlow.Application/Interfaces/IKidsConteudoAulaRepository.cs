using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IKidsConteudoAulaRepository
{
    Task<KidsConteudoAula?> GetByIdAsync(int id);
    Task<IEnumerable<KidsConteudoAula>> GetAllAsync(string? status = null, string? salaId = null, string? turmaId = null, DateTime? dataReferencia = null, int? limit = null);
    Task<KidsConteudoAula> CreateWithoutSaveAsync(KidsConteudoAula conteudo);
    Task UpdateWithoutSaveAsync(KidsConteudoAula conteudo);
}

public interface IKidsConteudoAulaAnexoRepository
{
    Task<IEnumerable<KidsConteudoAulaAnexo>> GetByConteudoAulaIdAsync(int conteudoAulaId);
    Task CreateRangeWithoutSaveAsync(IEnumerable<KidsConteudoAulaAnexo> anexos);
    Task DeleteByConteudoAulaIdWithoutSaveAsync(int conteudoAulaId);
}
