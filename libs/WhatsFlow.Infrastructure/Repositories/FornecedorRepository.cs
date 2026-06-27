using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class FornecedorRepository : IFornecedorRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public FornecedorRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public FornecedorRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<Fornecedor>> GetAllAsync()
    {
        return await _context.Set<Fornecedor>()
            .OrderBy(f => f.Nome)
            .ToListAsync();
    }

    public async Task<Fornecedor?> GetByIdAsync(int id)
    {
        return await _context.Set<Fornecedor>()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Fornecedor> CreateAsync(Fornecedor fornecedor)
    {
        fornecedor.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<Fornecedor>().Add(fornecedor);
        await _context.SaveChangesAsync();
        return fornecedor;
    }

    public async Task<Fornecedor> UpdateAsync(Fornecedor fornecedor)
    {
        _context.Set<Fornecedor>().Update(fornecedor);
        await _context.SaveChangesAsync();
        return fornecedor;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<Fornecedor>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Fornecedor>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
