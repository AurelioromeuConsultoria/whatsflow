using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ContatoRepository : IContatoRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ContatoRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public ContatoRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Contato>> GetAllAsync()
    {
        return await _context.Contatos
            .Include(c => c.ContatoTags)
                .ThenInclude(ct => ct.Tag)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<Contato> Items, int Total)> GetPagedAsync(ContatoPagedQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);

        var q = _context.Contatos
            .Include(c => c.ContatoTags)
                .ThenInclude(ct => ct.Tag)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            q = q.Where(c => c.Status == status);
        }

        if (query.OptIn.HasValue)
        {
            var optIn = query.OptIn.Value;
            q = q.Where(c => c.OptIn == optIn);
        }

        if (query.TagId.HasValue)
        {
            var tagId = query.TagId.Value;
            q = q.Where(c => c.ContatoTags.Any(ct => ct.TagId == tagId));
        }

        if (!string.IsNullOrWhiteSpace(query.Texto))
        {
            var t = query.Texto.Trim().ToLower();
            var digits = new string(query.Texto.Where(char.IsDigit).ToArray());
            var hasDigits = digits.Length >= 3;
            q = q.Where(c =>
                c.Nome.ToLower().Contains(t) ||
                (c.Email != null && c.Email.ToLower().Contains(t)) ||
                (hasDigits && c.TelefoneWhatsApp.Contains(digits)));
        }

        var sort = (query.Sort ?? "criadoem").Trim().ToLowerInvariant();
        var desc = !string.Equals(query.Direction, "asc", StringComparison.OrdinalIgnoreCase);
        q = sort switch
        {
            "nome" => desc ? q.OrderByDescending(c => c.Nome) : q.OrderBy(c => c.Nome),
            _ => desc ? q.OrderByDescending(c => c.CriadoEm) : q.OrderBy(c => c.CriadoEm),
        };

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Contato?> GetByIdAsync(int id)
    {
        return await _context.Contatos
            .Include(c => c.ContatoTags)
                .ThenInclude(ct => ct.Tag)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contato?> GetByTelefoneWhatsAppAsync(string telefoneWhatsApp, int? ignoreId = null)
    {
        var normalized = (telefoneWhatsApp ?? string.Empty).Trim();
        return await _context.Contatos
            .FirstOrDefaultAsync(c => c.TelefoneWhatsApp == normalized && (!ignoreId.HasValue || c.Id != ignoreId.Value));
    }

    public async Task<IReadOnlyList<Tag>> GetTagsByIdsAsync(IEnumerable<int> tagIds)
    {
        var ids = tagIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.Tags
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();
    }

    public async Task<Contato> CreateAsync(Contato contato)
    {
        contato.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Contatos.Add(contato);
        await _context.SaveChangesAsync();
        return contato;
    }

    public async Task<Contato> UpdateAsync(Contato contato)
    {
        _context.Contatos.Update(contato);
        await _context.SaveChangesAsync();
        return contato;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Contatos
            .Include(c => c.ContatoTags)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (entity != null)
        {
            _context.Contatos.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
