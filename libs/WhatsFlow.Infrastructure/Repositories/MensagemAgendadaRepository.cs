using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs.MensagensAgendadas;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class MensagemAgendadaRepository : IMensagemAgendadaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public MensagemAgendadaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public MensagemAgendadaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<MensagemAgendada>> GetAllAsync()
    {
        return await _context.MensagensAgendadas
            .Include(m => m.Visitante)
                .ThenInclude(v => v.Pessoa)
            .Include(m => m.ConfiguracaoMensagem)
            .OrderByDescending(m => m.DataCriacao)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<MensagemAgendada> Items, int Total)> GetPagedAsync(MensagemAgendadaPagedQuery query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);

        var q = _context.MensagensAgendadas
            .AsNoTracking()
            .Include(m => m.Visitante)
                .ThenInclude(v => v.Pessoa)
            .Include(m => m.ConfiguracaoMensagem)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            q = q.Where(m => m.Status == status);
        }

        if (query.VisitanteId.HasValue)
        {
            var visitanteId = query.VisitanteId.Value;
            q = q.Where(m => m.VisitanteId == visitanteId);
        }

        if (query.DataEnvioFrom.HasValue)
        {
            var from = query.DataEnvioFrom.Value;
            q = q.Where(m => m.DataEnvio >= from);
        }

        if (query.DataEnvioTo.HasValue)
        {
            var to = query.DataEnvioTo.Value;
            q = q.Where(m => m.DataEnvio <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.Texto))
        {
            var t = query.Texto.Trim().ToLower();
            q = q.Where(m =>
                (m.TextoFinal != null && m.TextoFinal.ToLower().Contains(t)) ||
                (m.ConfiguracaoMensagem != null && m.ConfiguracaoMensagem.Nome != null && m.ConfiguracaoMensagem.Nome.ToLower().Contains(t)) ||
                (m.Visitante != null && m.Visitante.Pessoa != null && m.Visitante.Pessoa.Nome.ToLower().Contains(t)));
        }

        var sort = (query.Sort ?? "dataenvio").Trim().ToLowerInvariant();
        var desc = string.Equals(query.Direction, "desc", StringComparison.OrdinalIgnoreCase);

        q = sort switch
        {
            "datacriacao" => desc ? q.OrderByDescending(m => m.DataCriacao) : q.OrderBy(m => m.DataCriacao),
            _ => desc ? q.OrderByDescending(m => m.DataEnvio) : q.OrderBy(m => m.DataEnvio),
        };

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<MensagemAgendadaStatsDto> GetStatsAsync()
    {
        var total = await _context.MensagensAgendadas.CountAsync();
        var enviadas = await _context.MensagensAgendadas.CountAsync(m => m.Status == StatusMensagem.Enviada);
        var erro = await _context.MensagensAgendadas.CountAsync(m => m.Status == StatusMensagem.Erro);
        var agendadas = await _context.MensagensAgendadas.CountAsync(m =>
            m.Status == StatusMensagem.Agendada ||
            m.Status == StatusMensagem.ProntaParaEnvio ||
            m.Status == StatusMensagem.EmProcessamento);

        return new MensagemAgendadaStatsDto
        {
            Total = total,
            Agendadas = agendadas,
            Enviadas = enviadas,
            Erro = erro
        };
    }

    public async Task<MensagemAgendada?> GetByIdAsync(int id)
    {
        return await _context.MensagensAgendadas
            .Include(m => m.Visitante)
                .ThenInclude(v => v.Pessoa)
            .Include(m => m.ConfiguracaoMensagem)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<MensagemAgendada> CreateAsync(MensagemAgendada mensagem)
    {
        mensagem.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.MensagensAgendadas.Add(mensagem);
        await _context.SaveChangesAsync();
        return mensagem;
    }

    public async Task<MensagemAgendada> UpdateAsync(MensagemAgendada mensagem)
    {
        _context.MensagensAgendadas.Update(mensagem);
        await _context.SaveChangesAsync();
        return mensagem;
    }

    public async Task DeleteAsync(int id)
    {
        var mensagem = await _context.MensagensAgendadas.FindAsync(id);
        if (mensagem != null)
        {
            _context.MensagensAgendadas.Remove(mensagem);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<MensagemAgendada>> GetMensagensProntasParaEnvioAsync()
    {
        var agora = DateTime.Now;
        return await _context.MensagensAgendadas
            .Include(m => m.Visitante)
                .ThenInclude(v => v.Pessoa)
            .Include(m => m.ConfiguracaoMensagem)
            .Where(m => m.Status == StatusMensagem.Agendada && m.DataEnvio <= agora)
            .OrderBy(m => m.DataEnvio)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MensagemAgendada>> ReservarProntasParaEnvioAsync(int limit)
    {
        var agora = DateTime.Now;
        var statusAgendada = (int)StatusMensagem.Agendada;
        var tenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        List<int> ids = new();

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (_context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                {
                    ids = await _context.MensagensAgendadas
                        .FromSqlRaw(
                            "SELECT * FROM \"MensagensAgendadas\" WHERE \"TenantId\" = {0} AND \"Status\" = {1} AND \"DataEnvio\" <= {2} ORDER BY \"DataEnvio\" FOR UPDATE SKIP LOCKED",
                            tenantId,
                            statusAgendada,
                            agora)
                        .Take(limit)
                        .Select(m => m.Id)
                        .ToListAsync();
                }
                else
                {
                    ids = await _context.MensagensAgendadas
                        .FromSqlRaw(
                            "SELECT * FROM MensagensAgendadas WITH (UPDLOCK, ROWLOCK) WHERE TenantId = {0} AND Status = {1} AND DataEnvio <= {2} ORDER BY DataEnvio",
                            tenantId,
                            statusAgendada,
                            agora)
                        .Take(limit)
                        .Select(m => m.Id)
                        .ToListAsync();
                }

                if (ids.Count == 0)
                {
                    await transaction.CommitAsync();
                    return;
                }

                var now = DateTime.Now;
                await _context.MensagensAgendadas
                    .Where(m => ids.Contains(m.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.Status, StatusMensagem.EmProcessamento)
                        .SetProperty(m => m.DataProcessamento, now));

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return await _context.MensagensAgendadas
            .Include(m => m.Visitante!)
                .ThenInclude(v => v.Pessoa)
            .Include(m => m.ConfiguracaoMensagem!)
            .Where(m => ids.Contains(m.Id))
            .OrderBy(m => m.DataEnvio)
            .ToListAsync();
    }

    public async Task<IEnumerable<MensagemAgendada>> GetMensagensPorVisitanteAsync(int visitanteId)
    {
        return await _context.MensagensAgendadas
            .Include(m => m.Visitante)
                .ThenInclude(v => v.Pessoa)
            .Include(m => m.ConfiguracaoMensagem)
            .Where(m => m.VisitanteId == visitanteId)
            .OrderBy(m => m.DataEnvio)
            .ToListAsync();
    }

    public async Task<IEnumerable<MensagemAgendada>> GetMensagensPorStatusAsync(StatusMensagem status)
    {
        return await _context.MensagensAgendadas
            .Include(m => m.Visitante)
                .ThenInclude(v => v.Pessoa)
            .Include(m => m.ConfiguracaoMensagem)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.DataCriacao)
            .ToListAsync();
    }

    public async Task<int> CancelarPendentesPorVisitanteAsync(int visitanteId, string motivo)
    {
        var now = DateTime.Now;
        var motivoFinal = string.IsNullOrWhiteSpace(motivo)
            ? "Cancelada por regeneração"
            : motivo;

        return await _context.MensagensAgendadas
            .Where(m => m.VisitanteId == visitanteId && m.Status != StatusMensagem.Enviada)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, StatusMensagem.Cancelada)
                .SetProperty(m => m.DataProcessamento, now)
                .SetProperty(m => m.LogErro, motivoFinal));
    }
}
