using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ComunicacaoSegmentoRepository : IComunicacaoSegmentoRepository
{
    private readonly WhatsFlowDbContext _context;

    public ComunicacaoSegmentoRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ComunicacaoSegmento>> GetAllAsync()
    {
        return await _context.ComunicacaoSegmentos
            .OrderByDescending(x => x.Padrao)
            .ThenBy(x => x.Nome)
            .ToListAsync();
    }

    public Task<ComunicacaoSegmento?> GetByIdAsync(int id)
    {
        return _context.ComunicacaoSegmentos.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ComunicacaoSegmento> CreateAsync(ComunicacaoSegmento segmento)
    {
        _context.ComunicacaoSegmentos.Add(segmento);
        await _context.SaveChangesAsync();
        return segmento;
    }

    public async Task<ComunicacaoSegmento> UpdateAsync(ComunicacaoSegmento segmento)
    {
        _context.ComunicacaoSegmentos.Update(segmento);
        await _context.SaveChangesAsync();
        return segmento;
    }
}
