using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class IndisponibilidadeVoluntarioRepository : IIndisponibilidadeVoluntarioRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public IndisponibilidadeVoluntarioRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public IndisponibilidadeVoluntarioRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IndisponibilidadeVoluntario?> GetByIdAsync(int id)
    {
        return await _context.IndisponibilidadesVoluntarios
            .Include(i => i.Voluntario)
                .ThenInclude(v => v.Pessoa)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<IndisponibilidadeVoluntario>> GetByVoluntarioAsync(int voluntarioId, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var query = _context.IndisponibilidadesVoluntarios
            .Include(i => i.Voluntario)
                .ThenInclude(v => v.Pessoa)
            .Where(i => i.VoluntarioId == voluntarioId);

        if (dataInicio.HasValue)
            query = query.Where(i => i.Data >= dataInicio.Value);
        if (dataFim.HasValue)
            query = query.Where(i => i.Data <= dataFim.Value);

        return await query.OrderBy(i => i.Data).ToListAsync();
    }

    public async Task<HashSet<int>> GetVoluntarioIdsIndisponiveisNaDataAsync(IEnumerable<int> voluntarioIds, DateTime data)
    {
        var ids = voluntarioIds.ToList();
        if (ids.Count == 0) return new HashSet<int>();

        var dataDate = data.Date;
        var idsIndisponiveis = await _context.IndisponibilidadesVoluntarios
            .Where(i => ids.Contains(i.VoluntarioId) && i.Data.Date == dataDate)
            .Select(i => i.VoluntarioId)
            .Distinct()
            .ToListAsync();

        return idsIndisponiveis.ToHashSet();
    }

    public async Task<IndisponibilidadeVoluntario> CreateAsync(IndisponibilidadeVoluntario indisponibilidade)
    {
        indisponibilidade.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.IndisponibilidadesVoluntarios.Add(indisponibilidade);
        await _context.SaveChangesAsync();
        return indisponibilidade;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.IndisponibilidadesVoluntarios.FindAsync(id);
        if (entity == null) return;
        _context.IndisponibilidadesVoluntarios.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
