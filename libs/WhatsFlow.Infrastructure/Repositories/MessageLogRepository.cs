using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class MessageLogRepository : IMessageLogRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public MessageLogRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public MessageLogRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<MessageLog> CreateAsync(MessageLog log)
    {
        log.TenantId = log.TenantId == 0
            ? (_tenantContext.TenantId ?? Tenant.InitialTenantId)
            : log.TenantId;
        _context.MessageLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<IReadOnlyList<MessageLog>> GetByEntregaIdAsync(int comunicacaoEntregaId)
    {
        return await _context.MessageLogs
            .Where(l => l.ComunicacaoEntregaId == comunicacaoEntregaId)
            .OrderBy(l => l.CriadoEm)
            .ToListAsync();
    }
}
