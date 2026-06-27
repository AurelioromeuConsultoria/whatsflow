using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class CargoRepository : ICargoRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CargoRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public CargoRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Cargo>> GetAllAsync()
    {
        return await _context.Set<Cargo>()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<Cargo?> GetByIdAsync(int id)
    {
        return await _context.Set<Cargo>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Cargo> CreateAsync(Cargo cargo)
    {
        cargo.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<Cargo>().Add(cargo);
        await _context.SaveChangesAsync();
        return cargo;
    }

    public async Task<Cargo> UpdateAsync(Cargo cargo)
    {
        _context.Set<Cargo>().Update(cargo);
        await _context.SaveChangesAsync();
        return cargo;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<Cargo>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Cargo>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
