using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IRelatorioFinanceiroService
{
    Task<RelatorioFluxoCaixaDto> GetFluxoCaixaAsync(DateTime dataInicio, DateTime dataFim);
    Task<RelatorioPorCategoriaCompletoDto> GetRelatorioPorCategoriaAsync(DateTime dataInicio, DateTime dataFim);
    Task<IEnumerable<RelatorioPorCentroCustoDto>> GetRelatorioPorCentroCustoAsync(DateTime dataInicio, DateTime dataFim);
    Task<IEnumerable<RelatorioPorProjetoDto>> GetRelatorioPorProjetoAsync(DateTime dataInicio, DateTime dataFim);
    Task<DreDto> GetDreAsync(int ano);
}

public class RelatorioFinanceiroService : IRelatorioFinanceiroService
{
    private readonly IReceitaRepository _receitaRepository;
    private readonly IDespesaRepository _despesaRepository;

    private static readonly string[] NomesMeses =
    [
        "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    ];

    public RelatorioFinanceiroService(IReceitaRepository receitaRepository, IDespesaRepository despesaRepository)
    {
        _receitaRepository = receitaRepository;
        _despesaRepository = despesaRepository;
    }

    public async Task<RelatorioFluxoCaixaDto> GetFluxoCaixaAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = (await _receitaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();
        var despesas = (await _despesaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();

        var totalReceitas = receitas.Sum(r => r.Valor);
        var totalDespesas = despesas.Sum(d => d.Valor);

        // Gerar movimentações diárias
        var dias = new Dictionary<DateTime, (decimal rec, decimal desp)>();
        foreach (var r in receitas)
        {
            var d = r.DataRecebimento.Date;
            dias.TryGetValue(d, out var v);
            dias[d] = (v.rec + r.Valor, v.desp);
        }
        foreach (var d in despesas)
        {
            var dt = d.DataVencimento.Date;
            dias.TryGetValue(dt, out var v);
            dias[dt] = (v.rec, v.desp + d.Valor);
        }

        var saldoAcumulado = 0m;
        var movimentacoes = dias
            .OrderBy(kv => kv.Key)
            .Select(kv =>
            {
                saldoAcumulado += kv.Value.rec - kv.Value.desp;
                return new MovimentacaoDiariaDto
                {
                    Data = kv.Key,
                    Receitas = kv.Value.rec,
                    Despesas = kv.Value.desp,
                    SaldoDia = kv.Value.rec - kv.Value.desp,
                    SaldoAcumulado = saldoAcumulado,
                };
            })
            .ToList();

        return new RelatorioFluxoCaixaDto
        {
            DataInicio = dataInicio,
            DataFim = dataFim,
            TotalReceitas = totalReceitas,
            TotalDespesas = totalDespesas,
            Saldo = totalReceitas - totalDespesas,
            MovimentacoesDiarias = movimentacoes,
        };
    }

    public async Task<RelatorioPorCategoriaCompletoDto> GetRelatorioPorCategoriaAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = (await _receitaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();
        var despesas = (await _despesaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();

        var totalReceitas = receitas.Sum(r => r.Valor);
        var totalDespesas = despesas.Sum(d => d.Valor);

        var recPorCat = receitas
            .GroupBy(r => new { r.CategoriaReceitaId, Nome = r.CategoriaReceita?.Nome ?? "Sem categoria" })
            .Select(g => new RelatorioPorCategoriaDto
            {
                CategoriaId = g.Key.CategoriaReceitaId ?? 0,
                CategoriaNome = g.Key.Nome,
                Valor = g.Sum(r => r.Valor),
                Quantidade = g.Count(),
                Percentual = totalReceitas > 0 ? g.Sum(r => r.Valor) / totalReceitas * 100 : 0,
            })
            .OrderByDescending(c => c.Valor)
            .ToList();

        var despPorCat = despesas
            .GroupBy(d => new { d.CategoriaDespesaId, Nome = d.CategoriaDespesa?.Nome ?? "Sem categoria" })
            .Select(g => new RelatorioPorCategoriaDto
            {
                CategoriaId = g.Key.CategoriaDespesaId ?? 0,
                CategoriaNome = g.Key.Nome,
                Valor = g.Sum(d => d.Valor),
                Quantidade = g.Count(),
                Percentual = totalDespesas > 0 ? g.Sum(d => d.Valor) / totalDespesas * 100 : 0,
            })
            .OrderByDescending(c => c.Valor)
            .ToList();

        return new RelatorioPorCategoriaCompletoDto { Receitas = recPorCat, Despesas = despPorCat };
    }

    public async Task<IEnumerable<RelatorioPorCentroCustoDto>> GetRelatorioPorCentroCustoAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = (await _receitaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();
        var despesas = (await _despesaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();

        var centros = receitas.Select(r => new { Id = r.CentroCustoId, Nome = r.CentroCusto?.Nome ?? "Sem centro de custo" })
            .Union(despesas.Select(d => new { Id = d.CentroCustoId, Nome = d.CentroCusto?.Nome ?? "Sem centro de custo" }))
            .Distinct()
            .ToList();

        return centros.Select(c => new RelatorioPorCentroCustoDto
        {
            CentroCusto = c.Nome,
            TotalReceitas = receitas.Where(r => r.CentroCustoId == c.Id).Sum(r => r.Valor),
            TotalDespesas = despesas.Where(d => d.CentroCustoId == c.Id).Sum(d => d.Valor),
            Saldo = receitas.Where(r => r.CentroCustoId == c.Id).Sum(r => r.Valor)
                  - despesas.Where(d => d.CentroCustoId == c.Id).Sum(d => d.Valor),
        }).OrderByDescending(c => c.TotalReceitas + c.TotalDespesas);
    }

    public async Task<IEnumerable<RelatorioPorProjetoDto>> GetRelatorioPorProjetoAsync(DateTime dataInicio, DateTime dataFim)
    {
        var receitas = (await _receitaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();
        var despesas = (await _despesaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();

        var projetos = receitas.Select(r => new { Id = r.ProjetoId, Nome = r.Projeto?.Nome ?? "Sem projeto" })
            .Union(despesas.Select(d => new { Id = d.ProjetoId, Nome = d.Projeto?.Nome ?? "Sem projeto" }))
            .Distinct()
            .ToList();

        return projetos.Select(p =>
        {
            var totalRec = receitas.Where(r => r.ProjetoId == p.Id).Sum(r => r.Valor);
            var totalDesp = despesas.Where(d => d.ProjetoId == p.Id).Sum(d => d.Valor);
            return new RelatorioPorProjetoDto
            {
                Projeto = p.Nome,
                TotalReceitas = totalRec,
                TotalDespesas = totalDesp,
                Saldo = totalRec - totalDesp,
            };
        }).OrderByDescending(p => p.TotalReceitas + p.TotalDespesas);
    }

    public async Task<DreDto> GetDreAsync(int ano)
    {
        var dataInicio = new DateTime(ano, 1, 1);
        var dataFim = new DateTime(ano, 12, 31, 23, 59, 59);

        var receitas = (await _receitaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();
        var despesas = (await _despesaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();

        var meses = new List<DreMesDto>();
        for (int mes = 1; mes <= 12; mes++)
        {
            var recMes = receitas.Where(r => r.DataRecebimento.Month == mes).ToList();
            var despMes = despesas.Where(d => d.DataVencimento.Month == mes).ToList();

            meses.Add(new DreMesDto
            {
                Mes = mes,
                MesNome = NomesMeses[mes - 1],
                TotalReceitas = recMes.Sum(r => r.Valor),
                TotalDespesas = despMes.Sum(d => d.Valor),
                Receitas = recMes
                    .GroupBy(r => new { r.CategoriaReceitaId, Nome = r.CategoriaReceita?.Nome ?? "Sem categoria" })
                    .Select(g => new DreCategoriaDto { CategoriaId = g.Key.CategoriaReceitaId, CategoriaNome = g.Key.Nome, Valor = g.Sum(r => r.Valor), Quantidade = g.Count() })
                    .OrderByDescending(c => c.Valor).ToList(),
                Despesas = despMes
                    .GroupBy(d => new { d.CategoriaDespesaId, Nome = d.CategoriaDespesa?.Nome ?? "Sem categoria" })
                    .Select(g => new DreCategoriaDto { CategoriaId = g.Key.CategoriaDespesaId, CategoriaNome = g.Key.Nome, Valor = g.Sum(d => d.Valor), Quantidade = g.Count() })
                    .OrderByDescending(c => c.Valor).ToList(),
            });
        }

        return new DreDto
        {
            Ano = ano,
            TotalReceitas = receitas.Sum(r => r.Valor),
            TotalDespesas = despesas.Sum(d => d.Valor),
            Meses = meses,
            TotalPorCategoriaReceita = receitas
                .GroupBy(r => new { r.CategoriaReceitaId, Nome = r.CategoriaReceita?.Nome ?? "Sem categoria" })
                .Select(g => new DreCategoriaDto { CategoriaId = g.Key.CategoriaReceitaId, CategoriaNome = g.Key.Nome, Valor = g.Sum(r => r.Valor), Quantidade = g.Count() })
                .OrderByDescending(c => c.Valor).ToList(),
            TotalPorCategoriaDespesa = despesas
                .GroupBy(d => new { d.CategoriaDespesaId, Nome = d.CategoriaDespesa?.Nome ?? "Sem categoria" })
                .Select(g => new DreCategoriaDto { CategoriaId = g.Key.CategoriaDespesaId, CategoriaNome = g.Key.Nome, Valor = g.Sum(d => d.Valor), Quantidade = g.Count() })
                .OrderByDescending(c => c.Valor).ToList(),
        };
    }
}
