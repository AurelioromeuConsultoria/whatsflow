using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface INotificacaoUsuarioService
{
    Task<IEnumerable<NotificacaoUsuarioDto>> GetMinhasAsync(int usuarioId, bool somenteNaoLidas = false, int? limit = null);
    Task<int> GetUnreadCountAsync(int usuarioId);
    Task<NotificacaoUsuarioDto> MarcarComoLidaAsync(int id, int usuarioId);
    Task<int> MarcarTodasComoLidasAsync(int usuarioId);
    Task CriarAsync(CriarNotificacaoUsuarioDto dto);
    Task CriarParaUsuariosAsync(IEnumerable<CriarNotificacaoUsuarioDto> notificacoes);
}

public class NotificacaoUsuarioService : INotificacaoUsuarioService
{
    private readonly INotificacaoUsuarioRepository _repository;

    public NotificacaoUsuarioService(INotificacaoUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<NotificacaoUsuarioDto>> GetMinhasAsync(int usuarioId, bool somenteNaoLidas = false, int? limit = null)
    {
        var items = await _repository.GetByUsuarioAsync(usuarioId, somenteNaoLidas, limit);
        return items.Select(MapToDto);
    }

    public Task<int> GetUnreadCountAsync(int usuarioId)
    {
        return _repository.GetUnreadCountAsync(usuarioId);
    }

    public async Task<NotificacaoUsuarioDto> MarcarComoLidaAsync(int id, int usuarioId)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null || item.UsuarioId != usuarioId)
        {
            throw new ArgumentException("Notificação não encontrada.");
        }

        if (!item.DataLeitura.HasValue)
        {
            item.DataLeitura = DateTime.Now;
            await _repository.UpdateAsync(item);
        }

        return MapToDto(item);
    }

    public Task<int> MarcarTodasComoLidasAsync(int usuarioId)
    {
        return _repository.MarcarTodasComoLidasAsync(usuarioId, DateTime.Now);
    }

    public async Task CriarAsync(CriarNotificacaoUsuarioDto dto)
    {
        if (dto.UsuarioId <= 0 || string.IsNullOrWhiteSpace(dto.Titulo) || string.IsNullOrWhiteSpace(dto.Mensagem))
        {
            return;
        }

        var item = new NotificacaoUsuario
        {
            UsuarioId = dto.UsuarioId,
            Tipo = dto.Tipo,
            Titulo = dto.Titulo.Trim(),
            Mensagem = dto.Mensagem.Trim(),
            Link = dto.Link?.Trim(),
            DataCriacao = DateTime.Now
        };

        await _repository.CreateAsync(item);
    }

    public async Task CriarParaUsuariosAsync(IEnumerable<CriarNotificacaoUsuarioDto> notificacoes)
    {
        var now = DateTime.Now;
        var items = notificacoes
            .Where(x => x.UsuarioId > 0 && !string.IsNullOrWhiteSpace(x.Titulo) && !string.IsNullOrWhiteSpace(x.Mensagem))
            .GroupBy(x => new { x.UsuarioId, Titulo = x.Titulo.Trim(), Mensagem = x.Mensagem.Trim(), Link = x.Link?.Trim(), x.Tipo })
            .Select(g => new NotificacaoUsuario
            {
                UsuarioId = g.Key.UsuarioId,
                Tipo = g.Key.Tipo,
                Titulo = g.Key.Titulo,
                Mensagem = g.Key.Mensagem,
                Link = g.Key.Link,
                DataCriacao = now
            })
            .ToList();

        await _repository.CreateRangeAsync(items);
    }

    private static NotificacaoUsuarioDto MapToDto(NotificacaoUsuario item)
    {
        return new NotificacaoUsuarioDto
        {
            Id = item.Id,
            Tipo = item.Tipo,
            Titulo = item.Titulo,
            Mensagem = item.Mensagem,
            Link = item.Link,
            DataCriacao = item.DataCriacao,
            DataLeitura = item.DataLeitura
        };
    }
}
