using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class WhatsAppAccountRepository : IWhatsAppAccountRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public WhatsAppAccountRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public WhatsAppAccountRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<WhatsAppAccount>> GetAllAsync()
    {
        return await _context.WhatsAppAccounts
            .OrderBy(a => a.Nome)
            .ToListAsync();
    }

    public async Task<WhatsAppAccount?> GetByIdAsync(int id)
    {
        return await _context.WhatsAppAccounts.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<WhatsAppAccount> CreateAsync(WhatsAppAccount account)
    {
        account.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.WhatsAppAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<WhatsAppAccount> UpdateAsync(WhatsAppAccount account)
    {
        _context.WhatsAppAccounts.Update(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.WhatsAppAccounts.FindAsync(id);
        if (entity != null)
        {
            _context.WhatsAppAccounts.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
