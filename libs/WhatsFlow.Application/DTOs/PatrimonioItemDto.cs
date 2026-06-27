using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Application.DTOs;

public class PatrimonioItemDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int CategoriaPatrimonioId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
    public int Quantidade { get; set; }
    public string? Campus { get; set; }
    public string? Localizacao { get; set; }
    public string? MinisterioArea { get; set; }
    public int? ResponsavelPessoaId { get; set; }
    public string? ResponsavelNome { get; set; }
    public string TipoAquisicao { get; set; } = string.Empty;
    public DateTime? DataAquisicao { get; set; }
    public decimal? ValorAquisicao { get; set; }
    public int? FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public string? NumeroNotaFiscal { get; set; }
    public int? DespesaId { get; set; }
    public int? CentroCustoId { get; set; }
    public string? CentroCustoNome { get; set; }
    public int? ProjetoId { get; set; }
    public string? ProjetoNome { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EstadoConservacao { get; set; } = string.Empty;
    public DateTime? DataUltimaAvaliacao { get; set; }
    public bool PossuiGarantia { get; set; }
    public DateTime? GarantiaAte { get; set; }
    public DateTime? DataUltimaManutencao { get; set; }
    public DateTime? DataProximaManutencao { get; set; }
    public string? FotoUrl { get; set; }
    public string? DocumentoUrl { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarPatrimonioItemDto
{
    [Required(ErrorMessage = "Código é obrigatório")]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Categoria é obrigatória")]
    public int CategoriaPatrimonioId { get; set; }

    [MaxLength(100)]
    public string? Marca { get; set; }

    [MaxLength(100)]
    public string? Modelo { get; set; }

    [MaxLength(100)]
    public string? NumeroSerie { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public int Quantidade { get; set; } = 1;

    [MaxLength(100)]
    public string? Campus { get; set; }

    [MaxLength(150)]
    public string? Localizacao { get; set; }

    [MaxLength(100)]
    public string? MinisterioArea { get; set; }

    public int? ResponsavelPessoaId { get; set; }

    [MaxLength(30)]
    public string TipoAquisicao { get; set; } = "Comprado";

    public DateTime? DataAquisicao { get; set; }
    public decimal? ValorAquisicao { get; set; }
    public int? FornecedorId { get; set; }

    [MaxLength(100)]
    public string? NumeroNotaFiscal { get; set; }

    public int? DespesaId { get; set; }
    public int? CentroCustoId { get; set; }
    public int? ProjetoId { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "EmUso";

    [MaxLength(30)]
    public string EstadoConservacao { get; set; } = "Bom";

    public DateTime? DataUltimaAvaliacao { get; set; }
    public bool PossuiGarantia { get; set; }
    public DateTime? GarantiaAte { get; set; }
    public DateTime? DataUltimaManutencao { get; set; }
    public DateTime? DataProximaManutencao { get; set; }

    [MaxLength(500)]
    public string? FotoUrl { get; set; }

    [MaxLength(500)]
    public string? DocumentoUrl { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}

public class AtualizarPatrimonioItemDto : CriarPatrimonioItemDto
{
    public bool Ativo { get; set; } = true;
}
