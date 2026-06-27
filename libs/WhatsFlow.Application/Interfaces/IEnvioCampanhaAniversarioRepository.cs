using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IEnvioCampanhaAniversarioRepository
{
    Task<EnvioCampanhaAniversario?> GetByIdAsync(int id);
    Task<EnvioCampanhaAniversario?> GetByPessoaAnoAsync(int pessoaId, int anoReferencia);
    Task<EnvioCampanhaAniversario> CreateAsync(EnvioCampanhaAniversario envio);
    Task<EnvioCampanhaAniversario> UpdateAsync(EnvioCampanhaAniversario envio);
    Task<IReadOnlyList<EnvioCampanhaAniversario>> GetRecentesAsync(int limit);
    Task<IReadOnlyList<EnvioCampanhaAniversario>> GetHistoricoAsync(string? busca, string? status, int limit);
    Task<int> CountAsync();
    Task<int> CountByStatusAnoAsync(StatusEnvioCampanhaAniversario status, int anoReferencia);
    Task<int> CountByStatusDataAsync(StatusEnvioCampanhaAniversario status, DateTime dataReferencia);
    Task<int> CountPendentesAnoAsync(int anoReferencia);
}
