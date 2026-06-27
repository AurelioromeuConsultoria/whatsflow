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

        var contatosTask = _db.Contatos
            .AsNoTracking()
            .Where(c =>
                c.Nome.ToLower().Contains(qLower) ||
                (c.Email != null && c.Email.ToLower().Contains(qLower)) ||
                (hasDigits && c.TelefoneWhatsApp.Contains(digits)))
            .OrderBy(c => c.Nome)
            .Take(perType)
            .Select(c => new GlobalSearchItemDto
            {
                Type = "Contato",
                Id = c.Id,
                Title = c.Nome,
                Subtitle = c.Email ?? c.TelefoneWhatsApp
            })
            .ToListAsync();

        var usuariosTask = _db.Usuarios
            .AsNoTracking()
            .Where(u =>
                u.EmailLogin.ToLower().Contains(qLower) ||
                u.Nome.ToLower().Contains(qLower))
            .OrderBy(u => u.Nome)
            .Take(perType)
            .Select(u => new GlobalSearchItemDto
            {
                Type = "Usuario",
                Id = u.Id,
                Title = u.Nome,
                Subtitle = u.EmailLogin
            })
            .ToListAsync();

        await Task.WhenAll(contatosTask, usuariosTask);

        var all = new List<GlobalSearchItemDto>(takeTotal);
        all.AddRange(contatosTask.Result);
        all.AddRange(usuariosTask.Result);

        return all.Take(takeTotal).ToList();
    }
}

