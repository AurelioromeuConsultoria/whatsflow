using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IKidsNotificacaoRepository
{
    Task<KidsNotificacao?> GetByIdAsync(int id);
    Task<IEnumerable<KidsNotificacao>> GetByCriancaIdAsync(int criancaPessoaId);
    Task<IEnumerable<KidsNotificacao>> GetByResponsavelIdAsync(int responsavelPessoaId);
    Task<IEnumerable<KidsNotificacao>> GetFeedByResponsavelIdAsync(int responsavelPessoaId, bool somenteNaoLidos = false, string? tipo = null, int? criancaPessoaId = null, int? limit = null);
    Task<IEnumerable<KidsNotificacao>> GetAdministrativosAsync(string? tipo = null, int? responsavelPessoaId = null, int? criancaPessoaId = null, int? limit = null);
    Task<IEnumerable<KidsNotificacao>> GetPendentesAsync();
    Task<KidsNotificacao> CreateAsync(KidsNotificacao notificacao);
    Task<KidsNotificacao> CreateWithoutSaveAsync(KidsNotificacao notificacao);
    Task CreateRangeAsync(IEnumerable<KidsNotificacao> notificacoes);
    Task<KidsNotificacao> UpdateAsync(KidsNotificacao notificacao);
}

