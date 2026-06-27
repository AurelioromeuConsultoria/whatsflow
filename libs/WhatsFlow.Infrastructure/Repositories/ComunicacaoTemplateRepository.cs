using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ComunicacaoTemplateRepository : IComunicacaoTemplateRepository
{
    private readonly WhatsFlowDbContext _context;

    public ComunicacaoTemplateRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ComunicacaoTemplate>> GetAllAsync()
    {
        return await _context.ComunicacaoTemplates
            .AsNoTracking()
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<ComunicacaoTemplate?> GetByIdAsync(int id)
    {
        return await _context.ComunicacaoTemplates
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<ComunicacaoTemplate> CreateAsync(ComunicacaoTemplate template)
    {
        _context.ComunicacaoTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<ComunicacaoTemplate> UpdateAsync(ComunicacaoTemplate template)
    {
        _context.ComunicacaoTemplates.Update(template);
        await _context.SaveChangesAsync();
        return template;
    }
}
