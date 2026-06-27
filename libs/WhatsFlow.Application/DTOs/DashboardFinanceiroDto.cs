namespace WhatsFlow.Application.DTOs;

public class DashboardFinanceiroDto
{
    public decimal TotalReceitasMes { get; set; }
    public decimal TotalDespesasMes { get; set; }
    public decimal SaldoMes { get; set; }
    public decimal TotalReceitasAno { get; set; }
    public decimal TotalDespesasAno { get; set; }
    public decimal SaldoAno { get; set; }
    public List<FluxoCaixaMensalDto> FluxoCaixaMensal { get; set; } = new();
    public List<ReceitaPorCategoriaDto> ReceitasPorCategoria { get; set; } = new();
    public List<DespesaPorCategoriaDto> DespesasPorCategoria { get; set; } = new();
    public List<UltimaMovimentacaoDto> UltimasMovimentacoes { get; set; } = new();
}

public class FluxoCaixaMensalDto
{
    public int Mes { get; set; }
    public int Ano { get; set; }
    public string MesAno { get; set; } = string.Empty;
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
}

public class ReceitaPorCategoriaDto
{
    public int CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
    public decimal Percentual { get; set; }
}

public class DespesaPorCategoriaDto
{
    public int CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
    public decimal Percentual { get; set; }
}

public class UltimaMovimentacaoDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Receita" ou "Despesa"
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public string Status { get; set; } = string.Empty;
}
