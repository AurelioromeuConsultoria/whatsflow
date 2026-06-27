using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class NotificacaoUsuarioRepository : INotificacaoUsuarioRepository
{
    private readonly WhatsFlowDbContext _context;

    public NotificacaoUsuarioRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<NotificacaoUsuario?> GetByIdAsync(int id)
    {
        return await _context.NotificacoesUsuarios
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<NotificacaoUsuario>> GetByUsuarioAsync(int usuarioId, bool somenteNaoLidas = false, int? limit = null)
    {
        var query = _context.NotificacoesUsuarios
            .Where(x => x.UsuarioId == usuarioId);

        if (somenteNaoLidas)
        {
            query = query.Where(x => x.DataLeitura == null);
        }

        query = query
            .OrderBy(x => x.DataLeitura == null ? 0 : 1)
            .ThenByDescending(x => x.DataCriacao);

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(int usuarioId)
    {
        return _context.NotificacoesUsuarios.CountAsync(x => x.UsuarioId == usuarioId && x.DataLeitura == null);
    }

    public async Task<NotificacaoUsuario> CreateAsync(NotificacaoUsuario notificacao)
    {
        _context.NotificacoesUsuarios.Add(notificacao);
        await _context.SaveChangesAsync();
        return notificacao;
    }

    public async Task CreateRangeAsync(IEnumerable<NotificacaoUsuario> notificacoes)
    {
        var items = notificacoes.ToList();
        if (items.Count == 0) return;

        _context.NotificacoesUsuarios.AddRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task<NotificacaoUsuario> UpdateAsync(NotificacaoUsuario notificacao)
    {
        _context.NotificacoesUsuarios.Update(notificacao);
        await _context.SaveChangesAsync();
        return notificacao;
    }

    public async Task<int> MarcarTodasComoLidasAsync(int usuarioId, DateTime dataLeitura)
    {
        var items = await _context.NotificacoesUsuarios
            .Where(x => x.UsuarioId == usuarioId && x.DataLeitura == null)
            .ToListAsync();

        foreach (var item in items)
        {
            item.DataLeitura = dataLeitura;
        }

        if (items.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return items.Count;
    }
}
