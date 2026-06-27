using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public TagRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public TagRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync()
    {
        return await _context.Tags
            .Include(t => t.ContatoTags)
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await _context.Tags
            .Include(t => t.ContatoTags)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tag?> GetByNomeAsync(string nome, int? ignoreId = null)
    {
        var normalized = (nome ?? string.Empty).Trim();
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.Nome == normalized && (!ignoreId.HasValue || t.Id != ignoreId.Value));
    }

    public async Task<Tag> CreateAsync(Tag tag)
    {
        tag.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> UpdateAsync(Tag tag)
    {
        _context.Tags.Update(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Tags
            .Include(t => t.ContatoTags)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity != null)
        {
            _context.Tags.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
