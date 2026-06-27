using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ProjetoRepository : IProjetoRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ProjetoRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public ProjetoRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<Projeto>> GetAllAsync()
    {
        return await _context.Set<Projeto>()
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<Projeto?> GetByIdAsync(int id)
    {
        return await _context.Set<Projeto>()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Projeto> CreateAsync(Projeto projeto)
    {
        projeto.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<Projeto>().Add(projeto);
        await _context.SaveChangesAsync();
        return projeto;
    }

    public async Task<Projeto> UpdateAsync(Projeto projeto)
    {
        _context.Set<Projeto>().Update(projeto);
        await _context.SaveChangesAsync();
        return projeto;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<Projeto>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Projeto>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
