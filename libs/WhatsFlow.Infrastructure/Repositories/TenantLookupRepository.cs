using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class TenantLookupRepository : ITenantLookupRepository
{
    private readonly WhatsFlowDbContext _context;

    public TenantLookupRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(int id)
    {
        // Tenant não tem filtro de tenant; busca direta por Id.
        return await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug)
    {
        var normalized = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == normalized && t.Ativo);
    }
}
