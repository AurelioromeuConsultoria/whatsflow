using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class KidsCheckinRepository : IKidsCheckinRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public KidsCheckinRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public KidsCheckinRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<KidsCheckin?> GetByIdAsync(int id)
    {
        return await _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Include(c => c.CheckoutBy)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<KidsCheckin?> GetCheckinAtivoPorCriancaAsync(int criancaPessoaId)
    {
        return await _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Include(c => c.CheckoutBy)
            .FirstOrDefaultAsync(c => c.CriancaPessoaId == criancaPessoaId && c.Status == "CheckedIn");
    }

    public async Task<KidsCheckin?> GetByCodigoSessaoAsync(string codigoSessao)
    {
        return await _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Include(c => c.CheckoutBy)
            .FirstOrDefaultAsync(c => c.CodigoSessao == codigoSessao);
    }

    public async Task<KidsCheckin?> GetByTokenRetiradaAsync(string tokenRetirada)
    {
        return await _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Include(c => c.CheckoutBy)
            .FirstOrDefaultAsync(c => c.TokenRetirada == tokenRetirada);
    }

    public async Task<KidsCheckin?> GetByPinRetiradaAsync(string pinRetirada)
    {
        return await _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Include(c => c.CheckoutBy)
            .FirstOrDefaultAsync(c => c.PinRetirada == pinRetirada);
    }

    public async Task<IEnumerable<KidsCheckin>> GetByPeriodoAsync(DateTime dataInicioUtc, DateTime dataFimUtc)
    {
        return await _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Include(c => c.CheckoutBy)
            .Where(c => c.CheckinTime >= dataInicioUtc && c.CheckinTime <= dataFimUtc)
            .OrderByDescending(c => c.CheckinTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<KidsCheckin>> GetHistoricoPorCriancaAsync(int criancaPessoaId, int? limit = null)
    {
        var query = _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Include(c => c.CheckoutBy)
            .Where(c => c.CriancaPessoaId == criancaPessoaId)
            .OrderByDescending(c => c.CheckinTime);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<KidsCheckin>)query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<(IReadOnlyList<KidsCheckin> Items, int Total)> GetHistoricoPagedAsync(
        IEnumerable<int> criancaIds, int page, int pageSize)
    {
        var ids = criancaIds.ToList();
        var query = _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Where(c => ids.Contains(c.CriancaPessoaId))
            .OrderByDescending(c => c.CheckinTime);

        var total = await query.CountAsync();
        var skip = (page - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync();
        return ((IReadOnlyList<KidsCheckin>)items, total);
    }

    public async Task<IEnumerable<KidsCheckin>> GetCheckinsAtivosAsync()
    {
        return await _context.Set<KidsCheckin>()
            .Include(c => c.Crianca)
            .Include(c => c.CheckinBy)
            .Where(c => c.Status == "CheckedIn")
            .OrderByDescending(c => c.CheckinTime)
            .ToListAsync();
    }

    public async Task<KidsCheckin> CreateAsync(KidsCheckin checkin)
    {
        checkin.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<KidsCheckin>().Add(checkin);
        await _context.SaveChangesAsync();
        return checkin;
    }

    public Task<KidsCheckin> CreateWithoutSaveAsync(KidsCheckin checkin)
    {
        checkin.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.Set<KidsCheckin>().Add(checkin);
        return Task.FromResult(checkin);
    }

    public async Task<KidsCheckin> UpdateAsync(KidsCheckin checkin)
    {
        _context.Set<KidsCheckin>().Update(checkin);
        await _context.SaveChangesAsync();
        return checkin;
    }

    public Task UpdateWithoutSaveAsync(KidsCheckin checkin)
    {
        _context.Set<KidsCheckin>().Update(checkin);
        return Task.CompletedTask;
    }
}
