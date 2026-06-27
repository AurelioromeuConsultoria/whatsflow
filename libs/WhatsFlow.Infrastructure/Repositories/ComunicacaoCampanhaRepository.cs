using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ComunicacaoCampanhaRepository : IComunicacaoCampanhaRepository
{
    private readonly WhatsFlowDbContext _context;

    public ComunicacaoCampanhaRepository(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<ComunicacaoCampanha> Items, int Total)> GetPagedAsync(ComunicacaoCampanhaPagedQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);

        var q = _context.ComunicacaoCampanhas
            .AsNoTracking()
            .Include(c => c.Canais)
                .ThenInclude(cc => cc.Template)
            .Include(c => c.Entregas)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            q = q.Where(c => c.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PublicoAlvo))
        {
            var publico = query.PublicoAlvo.Trim().ToLowerInvariant();
            q = q.Where(c => c.PublicoAlvo.ToLower() == publico);
        }

        if (!string.IsNullOrWhiteSpace(query.Texto))
        {
            var texto = query.Texto.Trim().ToLowerInvariant();
            q = q.Where(c => c.Nome.ToLower().Contains(texto) || c.Objetivo.ToLower().Contains(texto));
        }

        q = q.OrderByDescending(c => c.DataCriacao);

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<ComunicacaoCampanha?> GetByIdAsync(int id)
    {
        return await _context.ComunicacaoCampanhas
            .Include(c => c.Canais)
                .ThenInclude(cc => cc.Template)
            .Include(c => c.Entregas.OrderByDescending(e => e.DataCriacao).Take(50))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ComunicacaoCampanha> CreateAsync(ComunicacaoCampanha campanha)
    {
        _context.ComunicacaoCampanhas.Add(campanha);
        await _context.SaveChangesAsync();
        return campanha;
    }

    public async Task<ComunicacaoCampanha> UpdateAsync(ComunicacaoCampanha campanha)
    {
        _context.ComunicacaoCampanhas.Update(campanha);
        await _context.SaveChangesAsync();
        return campanha;
    }

    public async Task AtualizarStatusPorEntregasAsync(int campanhaId)
    {
        var campanha = await _context.ComunicacaoCampanhas
            .Include(c => c.Entregas)
            .FirstOrDefaultAsync(c => c.Id == campanhaId);

        if (campanha == null || campanha.Status == StatusComunicacaoCampanha.Cancelada)
        {
            return;
        }

        var status = CalcularStatusOperacional(campanha);
        if (campanha.Status == status)
        {
            return;
        }

        campanha.Status = status;
        campanha.DataAtualizacao = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<ComunicacaoStatsDto> GetStatsAsync()
    {
        var campanhas = await _context.ComunicacaoCampanhas
            .AsNoTracking()
            .Include(c => c.Entregas)
            .ToListAsync();
        var statusCampanhas = campanhas.Select(CalcularStatusOperacional).ToList();
        var totalCampanhas = campanhas.Count;
        var campanhasRascunho = statusCampanhas.Count(s => s == StatusComunicacaoCampanha.Rascunho);
        var campanhasAgendadas = statusCampanhas.Count(s => s == StatusComunicacaoCampanha.Agendada);
        var entregasPendentes = await _context.ComunicacaoEntregas.CountAsync(e =>
            e.Status == StatusComunicacaoEntrega.Pendente || e.Status == StatusComunicacaoEntrega.Reservado);
        var entregasEnviadas = await _context.ComunicacaoEntregas.CountAsync(e =>
            e.Status == StatusComunicacaoEntrega.Enviado || e.Status == StatusComunicacaoEntrega.Entregue);
        var entregasComFalha = await _context.ComunicacaoEntregas.CountAsync(e => e.Status == StatusComunicacaoEntrega.Falhou);

        return new ComunicacaoStatsDto
        {
            TotalCampanhas = totalCampanhas,
            CampanhasRascunho = campanhasRascunho,
            CampanhasAgendadas = campanhasAgendadas,
            EntregasPendentes = entregasPendentes,
            EntregasEnviadas = entregasEnviadas,
            EntregasComFalha = entregasComFalha
        };
    }

    private static StatusComunicacaoCampanha CalcularStatusOperacional(ComunicacaoCampanha campanha)
    {
        var entregas = campanha.Entregas.ToList();
        if (campanha.Status == StatusComunicacaoCampanha.Cancelada)
        {
            return StatusComunicacaoCampanha.Cancelada;
        }

        if (entregas.Count == 0)
        {
            return campanha.DataAgendamento.HasValue
                ? StatusComunicacaoCampanha.Agendada
                : StatusComunicacaoCampanha.Rascunho;
        }

        if (entregas.Any(e => e.Status == StatusComunicacaoEntrega.Pendente || e.Status == StatusComunicacaoEntrega.Reservado))
        {
            return campanha.DataAgendamento.HasValue && campanha.DataAgendamento.Value > DateTime.Now
                ? StatusComunicacaoCampanha.Agendada
                : StatusComunicacaoCampanha.Processando;
        }

        return entregas.Any(e => e.Status == StatusComunicacaoEntrega.Falhou)
            ? StatusComunicacaoCampanha.ConcluidaComFalhas
            : StatusComunicacaoCampanha.Concluida;
    }
}
