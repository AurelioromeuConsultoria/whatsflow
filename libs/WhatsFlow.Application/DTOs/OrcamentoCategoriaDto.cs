using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class OrcamentoCategoriaDto
{
    public int Id { get; set; }
    public int Ano { get; set; }
    public TipoOrcamento Tipo { get; set; }
    public int? CategoriaReceitaId { get; set; }
    public string? CategoriaReceitaNome { get; set; }
    public int? CategoriaDespesaId { get; set; }
    public string? CategoriaDespesaNome { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal ValorOrcado { get; set; }
}

public class SalvarOrcamentoCategoriaDto
{
    [Required]
    public int Ano { get; set; }

    [Required]
    public TipoOrcamento Tipo { get; set; }

    public int? CategoriaReceitaId { get; set; }
    public int? CategoriaDespesaId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal ValorOrcado { get; set; }
}

public class OrcamentoComparacaoItemDto
{
    public int? CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public TipoOrcamento Tipo { get; set; }
    public decimal ValorOrcado { get; set; }
    public decimal ValorRealizado { get; set; }
    public decimal Diferenca => ValorOrcado - ValorRealizado;
    public decimal PercentualUtilizado => ValorOrcado > 0 ? (ValorRealizado / ValorOrcado) * 100 : 0;
}

public class OrcamentoComparacaoDto
{
    public int Ano { get; set; }
    public decimal TotalOrcadoReceitas { get; set; }
    public decimal TotalRealizadoReceitas { get; set; }
    public decimal TotalOrcadoDespesas { get; set; }
    public decimal TotalRealizadoDespesas { get; set; }
    public List<OrcamentoComparacaoItemDto> Receitas { get; set; } = new();
    public List<OrcamentoComparacaoItemDto> Despesas { get; set; } = new();
}
