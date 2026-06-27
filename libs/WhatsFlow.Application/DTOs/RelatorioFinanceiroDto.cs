namespace WhatsFlow.Application.DTOs;

public class RelatorioFluxoCaixaDto
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public List<MovimentacaoDiariaDto> MovimentacoesDiarias { get; set; } = new();
}

public class FluxoCaixaDiarioDto
{
    public DateTime Data { get; set; }
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public decimal SaldoAcumulado { get; set; }
}

public class MovimentacaoDiariaDto
{
    public DateTime Data { get; set; }
    public decimal Receitas { get; set; }
    public decimal Despesas { get; set; }
    public decimal SaldoDia { get; set; }
    public decimal SaldoAcumulado { get; set; }
}

public class RelatorioPorCategoriaDto
{
    public int CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; }
    public decimal Percentual { get; set; }
}

public class RelatorioPorCategoriaCompletoDto
{
    public List<RelatorioPorCategoriaDto> Receitas { get; set; } = new();
    public List<RelatorioPorCategoriaDto> Despesas { get; set; } = new();
}

public class RelatorioPorCentroCustoDto
{
    public string CentroCusto { get; set; } = string.Empty;
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
}

public class ItemCentroCustoDto
{
    public int CentroCustoId { get; set; }
    public string CentroCustoNome { get; set; } = string.Empty;
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public int QuantidadeReceitas { get; set; }
    public int QuantidadeDespesas { get; set; }
}

public class RelatorioPorProjetoDto
{
    public string Projeto { get; set; } = string.Empty;
    public decimal? Orcamento { get; set; }
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public decimal? PercentualUtilizado { get; set; }
}

public class ItemProjetoDto
{
    public int ProjetoId { get; set; }
    public string ProjetoNome { get; set; } = string.Empty;
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public int QuantidadeReceitas { get; set; }
    public int QuantidadeDespesas { get; set; }
}
