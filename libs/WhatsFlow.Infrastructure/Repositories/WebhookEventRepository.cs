using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public WebhookEventRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public WebhookEventRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<WebhookEvent?> GetByIdAsync(int id)
    {
        return await _context.WebhookEvents.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<WebhookEvent?> GetByProviderMessageIdAsync(WhatsAppProviderType provider, string providerMessageId)
    {
        return await _context.WebhookEvents
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderMessageId == providerMessageId);
    }

    public async Task<WebhookEvent> CreateAsync(WebhookEvent webhookEvent)
    {
        webhookEvent.TenantId = webhookEvent.TenantId == 0
            ? (_tenantContext.TenantId ?? Tenant.InitialTenantId)
            : webhookEvent.TenantId;
        _context.WebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync();
        return webhookEvent;
    }

    public async Task<WebhookEvent> UpdateAsync(WebhookEvent webhookEvent)
    {
        _context.WebhookEvents.Update(webhookEvent);
        await _context.SaveChangesAsync();
        return webhookEvent;
    }

    public async Task<IReadOnlyList<WebhookEvent>> GetPendentesAsync(int limit)
    {
        var take = limit <= 0 ? 50 : Math.Min(limit, 500);
        return await _context.WebhookEvents
            .Where(e => e.Status == WebhookEventStatus.Recebido)
            .OrderBy(e => e.CriadoEm)
            .Take(take)
            .ToListAsync();
    }
}
