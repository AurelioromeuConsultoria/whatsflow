using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ContaBancariaRepository : IContaBancariaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ContaBancariaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public ContaBancariaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<ContaBancaria>> GetAllAsync()
    {
        return await _context.Set<ContaBancaria>()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<ContaBancaria?> GetByIdAsync(int id)
    {
        return await _context.Set<ContaBancaria>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ContaBancaria> CreateAsync(ContaBancaria contaBancaria)
    {
        contaBancaria.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<ContaBancaria>().Add(contaBancaria);
        await _context.SaveChangesAsync();
        return contaBancaria;
    }

    public async Task<ContaBancaria> UpdateAsync(ContaBancaria contaBancaria)
    {
        _context.Set<ContaBancaria>().Update(contaBancaria);
        await _context.SaveChangesAsync();
        return contaBancaria;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<ContaBancaria>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<ContaBancaria>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
