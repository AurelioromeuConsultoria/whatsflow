using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface INotificacaoUsuarioRepository
{
    Task<NotificacaoUsuario?> GetByIdAsync(int id);
    Task<IEnumerable<NotificacaoUsuario>> GetByUsuarioAsync(int usuarioId, bool somenteNaoLidas = false, int? limit = null);
    Task<int> GetUnreadCountAsync(int usuarioId);
    Task<NotificacaoUsuario> CreateAsync(NotificacaoUsuario notificacao);
    Task CreateRangeAsync(IEnumerable<NotificacaoUsuario> notificacoes);
    Task<NotificacaoUsuario> UpdateAsync(NotificacaoUsuario notificacao);
    Task<int> MarcarTodasComoLidasAsync(int usuarioId, DateTime dataLeitura);
}
