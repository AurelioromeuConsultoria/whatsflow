using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Application.DTOs;

public class ProjetoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public decimal? Orcamento { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarProjetoDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    public DateTime? DataInicio { get; set; }

    public DateTime? DataFim { get; set; }

    public decimal? Orcamento { get; set; }
}

public class AtualizarProjetoDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    public DateTime? DataInicio { get; set; }

    public DateTime? DataFim { get; set; }

    public decimal? Orcamento { get; set; }

    public bool Ativo { get; set; }
}
