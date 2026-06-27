using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class FinanceiroQueryService : IFinanceiroQueryService
{
    private readonly WhatsFlowDbContext _context;

    public FinanceiroQueryService(WhatsFlowDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalReceitasAsync(DateTime dataInicio, DateTime dataFim, StatusReceita? status = null)
    {
        var query = _context.Receitas.Where(r => r.DataRecebimento >= dataInicio && r.DataRecebimento <= dataFim);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        
        return await query.SumAsync(r => r.Valor);
    }

    public async Task<decimal> GetTotalDespesasAsync(DateTime dataInicio, DateTime dataFim, StatusDespesa? status = null)
    {
        var query = _context.Despesas.Where(d => d.DataVencimento >= dataInicio && d.DataVencimento <= dataFim);
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        
        return await query.SumAsync(d => d.Valor);
    }

    public async Task<List<FluxoCaixaMensalDto>> GetFluxoCaixaMensalAsync(int meses)
    {
        var hoje = DateTime.Now;
        var dataInicio = hoje.AddMonths(-meses + 1);
        var primeiroDiaFluxo = new DateTime(dataInicio.Year, dataInicio.Month, 1);

        var receitasMensais = await _context.Receitas
            .Where(r => r.DataRecebimento >= primeiroDiaFluxo && r.Status == StatusReceita.Recebida)
            .GroupBy(r => new { r.DataRecebimento.Year, r.DataRecebimento.Month })
            .Select(g => new
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Total = g.Sum(r => r.Valor)
            })
            .ToListAsync();

        var despesasMensais = await _context.Despesas
            .Where(d => d.DataVencimento >= primeiroDiaFluxo && d.Status == StatusDespesa.Paga)
            .GroupBy(d => new { d.DataVencimento.Year, d.DataVencimento.Month })
            .Select(g => new
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Total = g.Sum(d => d.Valor)
            })
            .ToListAsync();

        var fluxoCaixaMensal = new List<FluxoCaixaMensalDto>();
        for (int i = meses - 1; i >= 0; i--)
        {
            var dataRef = hoje.AddMonths(-i);
            var mes = dataRef.Month;
            var ano = dataRef.Year;
            var mesAno = dataRef.ToString("MM/yyyy");

            var receitaMes = receitasMensais.FirstOrDefault(r => r.Ano == ano && r.Mes == mes)?.Total ?? 0;
            var despesaMes = despesasMensais.FirstOrDefault(d => d.Ano == ano && d.Mes == mes)?.Total ?? 0;

            fluxoCaixaMensal.Add(new FluxoCaixaMensalDto
            {
                Mes = mes,
                Ano = ano,
                MesAno = mesAno,
                TotalReceitas = receitaMes,
                TotalDespesas = despesaMes,
                Saldo = receitaMes - despesaMes
            });
        }

        return fluxoCaixaMensal;
    }

    public async Task<List<ReceitaPorCategoriaDto>> GetReceitasPorCategoriaAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = await _context.Receitas
            .Include(r => r.CategoriaReceita)
            .Where(r => r.DataRecebimento >= dataInicio && r.DataRecebimento <= dataFim && r.Status == StatusReceita.Recebida)
            .ToListAsync();

        var total = receitas.Sum(r => r.Valor);

        var receitasPorCategoria = receitas
            .GroupBy(r => new { r.CategoriaReceitaId, Nome = r.CategoriaReceita != null ? r.CategoriaReceita.Nome : null })
            .Select(g => new ReceitaPorCategoriaDto
            {
                CategoriaId = g.Key.CategoriaReceitaId ?? 0,
                CategoriaNome = g.Key.Nome ?? "Sem categoria",
                Total = g.Sum(r => r.Valor),
                Quantidade = g.Count(),
                Percentual = total > 0 ? (g.Sum(r => r.Valor) / total) * 100 : 0
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return receitasPorCategoria;
    }

    public async Task<List<DespesaPorCategoriaDto>> GetDespesasPorCategoriaAsync(DateTime dataInicio, DateTime dataFim)
    {
        var despesas = await _context.Despesas
            .Include(d => d.CategoriaDespesa)
            .Where(d => d.DataVencimento >= dataInicio && d.DataVencimento <= dataFim && d.Status == StatusDespesa.Paga)
            .ToListAsync();

        var total = despesas.Sum(d => d.Valor);

        var despesasPorCategoria = despesas
            .GroupBy(d => new { d.CategoriaDespesaId, Nome = d.CategoriaDespesa != null ? d.CategoriaDespesa.Nome : null })
            .Select(g => new DespesaPorCategoriaDto
            {
                CategoriaId = g.Key.CategoriaDespesaId ?? 0,
                CategoriaNome = g.Key.Nome ?? "Sem categoria",
                Total = g.Sum(d => d.Valor),
                Quantidade = g.Count(),
                Percentual = total > 0 ? (g.Sum(d => d.Valor) / total) * 100 : 0
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return despesasPorCategoria;
    }

    public async Task<List<UltimaMovimentacaoDto>> GetUltimasMovimentacoesAsync(int quantidade)
    {
        var ultimasReceitas = await _context.Receitas
            .OrderByDescending(r => r.DataRecebimento)
            .Take(quantidade / 2)
            .Select(r => new UltimaMovimentacaoDto
            {
                Id = r.Id,
                Tipo = "Receita",
                Descricao = r.Descricao,
                Valor = r.Valor,
                Data = r.DataRecebimento,
                Status = r.Status == StatusReceita.Recebida ? "Recebida" : r.Status == StatusReceita.Pendente ? "Pendente" : "Cancelada"
            })
            .ToListAsync();

        var ultimasDespesas = await _context.Despesas
            .OrderByDescending(d => d.DataVencimento)
            .Take(quantidade / 2)
            .Select(d => new UltimaMovimentacaoDto
            {
                Id = d.Id,
                Tipo = "Despesa",
                Descricao = d.Descricao,
                Valor = d.Valor,
                Data = d.DataVencimento,
                Status = d.Status == StatusDespesa.Paga ? "Paga" : d.Status == StatusDespesa.Pendente ? "Pendente" : "Cancelada"
            })
            .ToListAsync();

        return ultimasReceitas
            .Concat(ultimasDespesas)
            .OrderByDescending(m => m.Data)
            .Take(quantidade)
            .ToList();
    }

    public async Task<List<MovimentacaoDiariaDto>> GetMovimentacoesDiariasAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = await _context.Receitas
            .Where(r => r.DataRecebimento >= dataInicio && r.DataRecebimento <= dataFim && r.Status == StatusReceita.Recebida)
            .ToListAsync();

        var despesas = await _context.Despesas
            .Where(d => d.DataVencimento >= dataInicio && d.DataVencimento <= dataFim && d.Status == StatusDespesa.Paga)
            .ToListAsync();

        var receitasPorDia = receitas
            .GroupBy(r => r.DataRecebimento.Date)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Valor));

        var despesasPorDia = despesas
            .GroupBy(d => d.DataVencimento.Date)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Valor));

        var todasAsDatas = receitasPorDia.Keys
            .Union(despesasPorDia.Keys)
            .OrderBy(d => d)
            .ToList();

        var movimentacoes = new List<MovimentacaoDiariaDto>();
        decimal saldoAcumulado = 0;

        foreach (var data in todasAsDatas)
        {
            var receitaDia = receitasPorDia.GetValueOrDefault(data, 0);
            var despesaDia = despesasPorDia.GetValueOrDefault(data, 0);
            var saldoDia = receitaDia - despesaDia;
            saldoAcumulado += saldoDia;

            movimentacoes.Add(new MovimentacaoDiariaDto
            {
                Data = data,
                Receitas = receitaDia,
                Despesas = despesaDia,
                SaldoDia = saldoDia,
                SaldoAcumulado = saldoAcumulado
            });
        }

        return movimentacoes;
    }

    public async Task<List<RelatorioPorCategoriaDto>> GetRelatorioReceitasPorCategoriaAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = await _context.Receitas
            .Include(r => r.CategoriaReceita)
            .Where(r => r.DataRecebimento >= dataInicio && r.DataRecebimento <= dataFim && r.Status == StatusReceita.Recebida)
            .ToListAsync();

        var total = receitas.Sum(r => r.Valor);

        return receitas
            .GroupBy(r => new { r.CategoriaReceitaId, Nome = r.CategoriaReceita != null ? r.CategoriaReceita.Nome : null })
            .Select(g => new RelatorioPorCategoriaDto
            {
                CategoriaId = g.Key.CategoriaReceitaId ?? 0,
                CategoriaNome = g.Key.Nome ?? "Sem categoria",
                Valor = g.Sum(r => r.Valor),
                Quantidade = g.Count(),
                Percentual = total > 0 ? (g.Sum(r => r.Valor) / total) * 100 : 0
            })
            .OrderByDescending(x => x.Valor)
            .ToList();
    }

    public async Task<List<RelatorioPorCategoriaDto>> GetRelatorioDespesasPorCategoriaAsync(DateTime dataInicio, DateTime dataFim)
    {
        var despesas = await _context.Despesas
            .Include(d => d.CategoriaDespesa)
            .Where(d => d.DataVencimento >= dataInicio && d.DataVencimento <= dataFim && d.Status == StatusDespesa.Paga)
            .ToListAsync();

        var total = despesas.Sum(d => d.Valor);

        return despesas
            .GroupBy(d => new { d.CategoriaDespesaId, Nome = d.CategoriaDespesa != null ? d.CategoriaDespesa.Nome : null })
            .Select(g => new RelatorioPorCategoriaDto
            {
                CategoriaId = g.Key.CategoriaDespesaId ?? 0,
                CategoriaNome = g.Key.Nome ?? "Sem categoria",
                Valor = g.Sum(d => d.Valor),
                Quantidade = g.Count(),
                Percentual = total > 0 ? (g.Sum(d => d.Valor) / total) * 100 : 0
            })
            .OrderByDescending(x => x.Valor)
            .ToList();
    }

    public async Task<List<RelatorioPorCentroCustoDto>> GetRelatorioPorCentroCustoAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = await _context.Receitas
            .Include(r => r.CentroCusto)
            .Where(r => r.DataRecebimento >= dataInicio && r.DataRecebimento <= dataFim && r.Status == StatusReceita.Recebida)
            .ToListAsync();

        var despesas = await _context.Despesas
            .Include(d => d.CentroCusto)
            .Where(d => d.DataVencimento >= dataInicio && d.DataVencimento <= dataFim && d.Status == StatusDespesa.Paga)
            .ToListAsync();

        var receitasPorCentroCusto = receitas
            .GroupBy(r => new { r.CentroCustoId, Nome = r.CentroCusto != null ? r.CentroCusto.Nome : null })
            .ToDictionary(g => g.Key.CentroCustoId ?? 0, g => g.Sum(r => r.Valor));

        var despesasPorCentroCusto = despesas
            .GroupBy(d => new { d.CentroCustoId, Nome = d.CentroCusto != null ? d.CentroCusto.Nome : null })
            .ToDictionary(g => g.Key.CentroCustoId ?? 0, g => new { Nome = g.Key.Nome ?? "Sem centro de custo", Total = g.Sum(d => d.Valor) });

        var todosCentrosCusto = receitasPorCentroCusto.Keys.Union(despesasPorCentroCusto.Keys).Distinct();

        return todosCentrosCusto.Select(ccId =>
        {
            var receitaTotal = receitasPorCentroCusto.GetValueOrDefault(ccId, 0);
            var despesaInfo = despesasPorCentroCusto.GetValueOrDefault(ccId);
            var despesaTotal = despesaInfo?.Total ?? 0;
            var nome = despesaInfo?.Nome ?? receitas.FirstOrDefault(r => r.CentroCustoId == ccId)?.CentroCusto?.Nome ?? "Sem centro de custo";

            return new RelatorioPorCentroCustoDto
            {
                CentroCusto = nome,
                TotalReceitas = receitaTotal,
                TotalDespesas = despesaTotal,
                Saldo = receitaTotal - despesaTotal
            };
        })
        .OrderByDescending(x => x.Saldo)
        .ToList();
    }

    public async Task<List<RelatorioPorProjetoDto>> GetRelatorioPorProjetoAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = await _context.Receitas
            .Include(r => r.Projeto)
            .Where(r => r.DataRecebimento >= dataInicio && r.DataRecebimento <= dataFim && r.Status == StatusReceita.Recebida)
            .ToListAsync();

        var despesas = await _context.Despesas
            .Include(d => d.Projeto)
            .Where(d => d.DataVencimento >= dataInicio && d.DataVencimento <= dataFim && d.Status == StatusDespesa.Paga)
            .ToListAsync();

        var receitasPorProjeto = receitas
            .GroupBy(r => new { r.ProjetoId, Nome = r.Projeto != null ? r.Projeto.Nome : null, Orcamento = r.Projeto != null ? r.Projeto.Orcamento : null })
            .ToDictionary(g => g.Key.ProjetoId ?? 0, g => new { Nome = g.Key.Nome ?? "Sem projeto", Orcamento = g.Key.Orcamento, Total = g.Sum(r => r.Valor) });

        var despesasPorProjeto = despesas
            .GroupBy(d => new { d.ProjetoId, Nome = d.Projeto != null ? d.Projeto.Nome : null, Orcamento = d.Projeto != null ? d.Projeto.Orcamento : null })
            .ToDictionary(g => g.Key.ProjetoId ?? 0, g => new { Nome = g.Key.Nome ?? "Sem projeto", Orcamento = g.Key.Orcamento, Total = g.Sum(d => d.Valor) });

        var todosProjetos = receitasPorProjeto.Keys.Union(despesasPorProjeto.Keys).Distinct();

        return todosProjetos.Select(projId =>
        {
            var receitaInfo = receitasPorProjeto.GetValueOrDefault(projId);
            var despesaInfo = despesasPorProjeto.GetValueOrDefault(projId);
            var receitaTotal = receitaInfo?.Total ?? 0;
            var despesaTotal = despesaInfo?.Total ?? 0;
            var nome = receitaInfo?.Nome ?? despesaInfo?.Nome ?? "Sem projeto";
            var orcamento = receitaInfo?.Orcamento ?? despesaInfo?.Orcamento;

            return new RelatorioPorProjetoDto
            {
                Projeto = nome,
                Orcamento = orcamento,
                TotalReceitas = receitaTotal,
                TotalDespesas = despesaTotal,
                Saldo = receitaTotal - despesaTotal,
                PercentualUtilizado = orcamento.HasValue && orcamento.Value > 0 ? (despesaTotal / orcamento.Value) * 100 : null
            };
        })
        .OrderByDescending(x => x.Saldo)
        .ToList();
    }
}
