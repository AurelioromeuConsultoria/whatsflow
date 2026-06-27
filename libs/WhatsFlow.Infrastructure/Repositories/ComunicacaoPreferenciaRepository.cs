using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ComunicacaoPreferenciaRepository : IComunicacaoPreferenciaRepository
{
    private readonly WhatsFlowDbContext _context;

    public ComunicacaoPreferenciaRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ComunicacaoPreferencia>> GetByPessoaIdAsync(int pessoaId)
    {
        return await _context.ComunicacaoPreferencias
            .Where(x => x.PessoaId == pessoaId)
            .OrderBy(x => x.Canal)
            .ToListAsync();
    }

    public Task<ComunicacaoPreferencia?> GetByPessoaCanalAsync(int pessoaId, CanalComunicacao canal)
    {
        return _context.ComunicacaoPreferencias
            .FirstOrDefaultAsync(x => x.PessoaId == pessoaId && x.Canal == canal);
    }

    public async Task<ComunicacaoPreferencia> CreateAsync(ComunicacaoPreferencia preferencia)
    {
        _context.ComunicacaoPreferencias.Add(preferencia);
        await _context.SaveChangesAsync();
        return preferencia;
    }

    public async Task<ComunicacaoPreferencia> UpdateAsync(ComunicacaoPreferencia preferencia)
    {
        _context.ComunicacaoPreferencias.Update(preferencia);
        await _context.SaveChangesAsync();
        return preferencia;
    }
}
