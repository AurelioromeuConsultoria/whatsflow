namespace WhatsFlow.Application.DTOs;

public class DreCategoriaDto
{
    public int? CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; }
}

public class DreMesDto
{
    public int Mes { get; set; }
    public string MesNome { get; set; } = string.Empty;
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Resultado => TotalReceitas - TotalDespesas;
    public List<DreCategoriaDto> Receitas { get; set; } = new();
    public List<DreCategoriaDto> Despesas { get; set; } = new();
}

public class DreDto
{
    public int Ano { get; set; }
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Resultado => TotalReceitas - TotalDespesas;
    public List<DreMesDto> Meses { get; set; } = new();
    public List<DreCategoriaDto> TotalPorCategoriaReceita { get; set; } = new();
    public List<DreCategoriaDto> TotalPorCategoriaDespesa { get; set; } = new();
}
