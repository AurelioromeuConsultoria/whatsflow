using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Application.DTOs;

public class PatrimonioMovimentacaoDto
{
    public int Id { get; set; }
    public int PatrimonioItemId { get; set; }
    public string TipoMovimentacao { get; set; } = string.Empty;
    public DateTime DataMovimentacao { get; set; }
    public string? Origem { get; set; }
    public string? Destino { get; set; }
    public string? ResponsavelOrigem { get; set; }
    public string? ResponsavelDestino { get; set; }
    public string? Observacoes { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarPatrimonioMovimentacaoDto
{
    [Required(ErrorMessage = "Tipo de movimentação é obrigatório")]
    [MaxLength(40)]
    public string TipoMovimentacao { get; set; } = string.Empty;

    public DateTime? DataMovimentacao { get; set; }

    [MaxLength(150)]
    public string? Origem { get; set; }

    [MaxLength(150)]
    public string? Destino { get; set; }

    [MaxLength(150)]
    public string? ResponsavelOrigem { get; set; }

    [MaxLength(150)]
    public string? ResponsavelDestino { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}
