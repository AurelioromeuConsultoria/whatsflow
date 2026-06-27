using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ConfiguracaoMensagemRepository : IConfiguracaoMensagemRepository
{
    private readonly WhatsFlowDbContext _context;

    public ConfiguracaoMensagemRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ConfiguracaoMensagem>> GetAllAsync()
    {
        return await _context.ConfiguracoesMensagens
            .OrderBy(c => c.DiasAposVisita)
            .ToListAsync();
    }

    public async Task<ConfiguracaoMensagem?> GetByIdAsync(int id)
    {
        return await _context.ConfiguracoesMensagens
            .Include(c => c.MensagensAgendadas)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ConfiguracaoMensagem> CreateAsync(ConfiguracaoMensagem configuracao)
    {
        _context.ConfiguracoesMensagens.Add(configuracao);
        await _context.SaveChangesAsync();
        return configuracao;
    }

    public async Task<ConfiguracaoMensagem> UpdateAsync(ConfiguracaoMensagem configuracao)
    {
        _context.ConfiguracoesMensagens.Update(configuracao);
        await _context.SaveChangesAsync();
        return configuracao;
    }

    public async Task DeleteAsync(int id)
    {
        var configuracao = await _context.ConfiguracoesMensagens.FindAsync(id);
        if (configuracao != null)
        {
            _context.ConfiguracoesMensagens.Remove(configuracao);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ConfiguracaoMensagem>> GetAtivasAsync()
    {
        return await _context.ConfiguracoesMensagens
            .Where(c => c.Ativo)
            .OrderBy(c => c.DiasAposVisita)
            .ToListAsync();
    }
}

