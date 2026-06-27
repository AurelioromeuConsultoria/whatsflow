using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ComunicacaoEntregaRepository : IComunicacaoEntregaRepository
{
    private readonly WhatsFlowDbContext _context;

    public ComunicacaoEntregaRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<ComunicacaoEntrega> Items, int Total)> GetPagedAsync(ComunicacaoEntregaPagedQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);

        var q = _context.ComunicacaoEntregas
            .AsNoTracking()
            .AsQueryable();

        if (query.CampanhaId.HasValue)
        {
            q = q.Where(e => e.ComunicacaoCampanhaId == query.CampanhaId.Value);
        }

        if (query.Canal.HasValue)
        {
            q = q.Where(e => e.Canal == query.Canal.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(e => e.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Texto))
        {
            var texto = query.Texto.Trim().ToLowerInvariant();
            q = q.Where(e =>
                e.DestinoResolvido.ToLower().Contains(texto) ||
                e.ConteudoFinal.ToLower().Contains(texto) ||
                (e.Erro != null && e.Erro.ToLower().Contains(texto)));
        }

        q = q.OrderByDescending(e => e.DataCriacao);

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<IReadOnlyList<ComunicacaoEntrega>> GetByCampanhaIdAsync(int campanhaId)
    {
        return await _context.ComunicacaoEntregas
            .AsNoTracking()
            .Where(e => e.ComunicacaoCampanhaId == campanhaId)
            .OrderByDescending(e => e.DataCriacao)
            .ToListAsync();
    }

    public async Task<ComunicacaoEntrega?> GetByIdAsync(int id)
    {
        return await _context.ComunicacaoEntregas.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<ComunicacaoEntrega?> GetByProviderMessageIdAsync(string providerMessageId)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            return null;
        }

        return await _context.ComunicacaoEntregas
            .FirstOrDefaultAsync(e => e.ProviderMessageId == providerMessageId);
    }

    public async Task<int> CountCriadasNoMesAsync(DateTime referencia)
    {
        var inicio = new DateTime(referencia.Year, referencia.Month, 1, 0, 0, 0, referencia.Kind);
        var fim = inicio.AddMonths(1);
        return await _context.ComunicacaoEntregas
            .CountAsync(e => e.DataCriacao >= inicio && e.DataCriacao < fim);
    }

    public async Task<int> CancelarPendentesPorCampanhaAsync(int campanhaId)
    {
        var now = DateTime.UtcNow;
        return await _context.ComunicacaoEntregas
            .Where(e => e.ComunicacaoCampanhaId == campanhaId && e.Status == StatusComunicacaoEntrega.Pendente)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, StatusComunicacaoEntrega.Cancelado)
                .SetProperty(e => e.AtualizadoEm, now));
    }

    public async Task<ComunicacaoEntrega> CreateAsync(ComunicacaoEntrega entrega)
    {
        _context.ComunicacaoEntregas.Add(entrega);
        await _context.SaveChangesAsync();
        return entrega;
    }

    public async Task<IReadOnlyList<ComunicacaoEntrega>> CreateManyAsync(IEnumerable<ComunicacaoEntrega> entregas)
    {
        var lista = entregas.ToList();
        _context.ComunicacaoEntregas.AddRange(lista);
        await _context.SaveChangesAsync();
        return lista;
    }

    public async Task<ComunicacaoEntrega> UpdateAsync(ComunicacaoEntrega entrega)
    {
        _context.ComunicacaoEntregas.Update(entrega);
        await _context.SaveChangesAsync();
        return entrega;
    }

    public async Task<IReadOnlyList<ComunicacaoEntrega>> ReservarPendentesAsync(int limit)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        List<int> ids = [];

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Pendentes de campanhas Pausadas/Canceladas não são reservadas (Feature 3: pausa honrada).
                ids = await _context.ComunicacaoEntregas
                    .Where(e => e.Status == StatusComunicacaoEntrega.Pendente)
                    // Honra backoff de retentativa (AgendadoPara) por entrega.
                    .Where(e => !e.AgendadoPara.HasValue || e.AgendadoPara.Value <= DateTime.UtcNow)
                    .Where(e => e.ComunicacaoCampanha == null || !e.ComunicacaoCampanha.DataAgendamento.HasValue || e.ComunicacaoCampanha.DataAgendamento.Value <= DateTime.Now)
                    .Where(e => e.ComunicacaoCampanha == null ||
                        (e.ComunicacaoCampanha.Status != StatusComunicacaoCampanha.Pausada &&
                         e.ComunicacaoCampanha.Status != StatusComunicacaoCampanha.Cancelada))
                    .OrderBy(e => e.DataCriacao)
                    .Take(limit)
                    .Select(e => e.Id)
                    .ToListAsync();

                if (ids.Count == 0)
                {
                    await transaction.CommitAsync();
                    return;
                }

                var now = DateTime.Now;
                await _context.ComunicacaoEntregas
                    .Where(e => ids.Contains(e.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(e => e.Status, StatusComunicacaoEntrega.Reservado)
                        .SetProperty(e => e.ProcessadoEm, now));

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return await _context.ComunicacaoEntregas
            .Where(e => ids.Contains(e.Id))
            .OrderBy(e => e.DataCriacao)
            .ToListAsync();
    }
}
