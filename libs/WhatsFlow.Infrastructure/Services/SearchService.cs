using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs.Search;
using WhatsFlow.Application.Services;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly WhatsFlowDbContext _db;

    public SearchService(WhatsFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GlobalSearchItemDto>> SearchAsync(string query, int limit)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < 2) return Array.Empty<GlobalSearchItemDto>();

        var takeTotal = limit <= 0 ? 20 : Math.Min(limit, 50);
        var perType = Math.Max(3, Math.Min(10, takeTotal / 2));

        var qLower = q.ToLowerInvariant();
        var digits = new string(q.Where(char.IsDigit).ToArray());
        var hasDigits = digits.Length >= 3;

        var pessoasTask = _db.Pessoas
            .AsNoTracking()
            .Where(p =>
                p.Nome.ToLower().Contains(qLower) ||
                (p.Email != null && p.Email.ToLower().Contains(qLower)) ||
                (hasDigits && (
                    (p.Telefone != null && p.Telefone.Contains(digits)) ||
                    (p.WhatsApp != null && p.WhatsApp.Contains(digits))
                )))
            .OrderBy(p => p.Nome)
            .Take(perType)
            .Select(p => new GlobalSearchItemDto
            {
                Type = "Pessoa",
                Id = p.Id,
                Title = p.Nome,
                Subtitle = p.Email ?? p.WhatsApp ?? p.Telefone
            })
            .ToListAsync();

        var visitantesTask = _db.Visitantes
            .AsNoTracking()
            .Where(v =>
                v.Pessoa.Nome.ToLower().Contains(qLower) ||
                (v.Pessoa.Email != null && v.Pessoa.Email.ToLower().Contains(qLower)) ||
                (hasDigits && (
                    (v.Pessoa.Telefone != null && v.Pessoa.Telefone.Contains(digits)) ||
                    (v.Pessoa.WhatsApp != null && v.Pessoa.WhatsApp.Contains(digits))
                )))
            .OrderByDescending(v => v.DataVisita)
            .Take(perType)
            .Select(v => new GlobalSearchItemDto
            {
                Type = "Visitante",
                Id = v.Id,
                Title = v.Pessoa.Nome,
                Subtitle = $"Visita: {v.DataVisita:yyyy-MM-dd}"
            })
            .ToListAsync();

        var eventosTask = _db.Eventos
            .AsNoTracking()
            .Where(e =>
                e.Titulo.ToLower().Contains(qLower) ||
                (e.Descricao != null && e.Descricao.ToLower().Contains(qLower)))
            .OrderByDescending(e => e.DataInicio)
            .Take(perType)
            .Select(e => new GlobalSearchItemDto
            {
                Type = "Evento",
                Id = e.Id,
                Title = e.Titulo,
                Subtitle = $"{e.DataInicio:yyyy-MM-dd} → {e.DataFim:yyyy-MM-dd}"
            })
            .ToListAsync();

        var noticiasTask = _db.Noticias
            .AsNoTracking()
            .Where(n =>
                n.Titulo.ToLower().Contains(qLower) ||
                (n.Descricao != null && n.Descricao.ToLower().Contains(qLower)))
            .OrderByDescending(n => n.Data)
            .Take(perType)
            .Select(n => new GlobalSearchItemDto
            {
                Type = "Noticia",
                Id = n.Id,
                Title = n.Titulo,
                Subtitle = n.Data.ToString("yyyy-MM-dd")
            })
            .ToListAsync();

        var usuariosTask = _db.Usuarios
            .AsNoTracking()
            .Where(u =>
                u.EmailLogin.ToLower().Contains(qLower) ||
                u.Pessoa.Nome.ToLower().Contains(qLower))
            .OrderBy(u => u.Pessoa.Nome)
            .Take(perType)
            .Select(u => new GlobalSearchItemDto
            {
                Type = "Usuario",
                Id = u.Id,
                Title = u.Pessoa.Nome,
                Subtitle = u.EmailLogin
            })
            .ToListAsync();

        await Task.WhenAll(pessoasTask, visitantesTask, eventosTask, noticiasTask, usuariosTask);

        var all = new List<GlobalSearchItemDto>(takeTotal);
        all.AddRange(pessoasTask.Result);
        all.AddRange(visitantesTask.Result);
        all.AddRange(eventosTask.Result);
        all.AddRange(noticiasTask.Result);
        all.AddRange(usuariosTask.Result);

        return all.Take(takeTotal).ToList();
    }
}

