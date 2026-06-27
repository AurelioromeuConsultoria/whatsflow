using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class KidsEstruturaRepository : IKidsEstruturaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public KidsEstruturaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public KidsEstruturaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<KidsSala>> GetSalasAsync(bool incluirInativas = false)
    {
        var query = _context.KidsSalas.AsQueryable();
        if (!incluirInativas)
        {
            query = query.Where(s => s.Ativo);
        }

        return await query
            .OrderBy(s => s.Nome)
            .ToListAsync();
    }

    public Task<KidsSala?> GetSalaByIdAsync(string id)
    {
        return _context.KidsSalas.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<KidsSala> CreateSalaAsync(KidsSala sala)
    {
        sala.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.KidsSalas.Add(sala);
        await _context.SaveChangesAsync();
        return sala;
    }

    public async Task<KidsSala> UpdateSalaAsync(KidsSala sala)
    {
        _context.KidsSalas.Update(sala);
        await _context.SaveChangesAsync();
        return sala;
    }

    public async Task<IEnumerable<KidsTurma>> GetTurmasAsync(string? salaId = null, bool incluirInativas = false)
    {
        var query = _context.KidsTurmas
            .Include(t => t.Sala)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(salaId))
        {
            query = query.Where(t => t.SalaId == salaId);
        }

        if (!incluirInativas)
        {
            query = query.Where(t => t.Ativo);
        }

        return await query
            .OrderBy(t => t.SalaId)
            .ThenBy(t => t.Nome)
            .ToListAsync();
    }

    public Task<KidsTurma?> GetTurmaByIdAsync(string id)
    {
        return _context.KidsTurmas
            .Include(t => t.Sala)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<KidsTurma> CreateTurmaAsync(KidsTurma turma)
    {
        turma.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.KidsTurmas.Add(turma);
        await _context.SaveChangesAsync();
        return turma;
    }

    public async Task<KidsTurma> UpdateTurmaAsync(KidsTurma turma)
    {
        _context.KidsTurmas.Update(turma);
        await _context.SaveChangesAsync();
        return turma;
    }
}
