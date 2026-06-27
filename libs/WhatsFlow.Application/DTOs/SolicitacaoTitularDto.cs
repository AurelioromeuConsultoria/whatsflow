using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class SolicitacaoTitularDto
{
    public int Id { get; set; }
    public int? PessoaId { get; set; }
    public string? NomePessoa { get; set; }
    public string? NomeSolicitante { get; set; }
    public string? ContatoSolicitante { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Canal { get; set; }
    public string? Descricao { get; set; }
    public DateTime SolicitadoEm { get; set; }
    public DateTime PrazoLimite { get; set; }
    public DateTime? AtendidoEm { get; set; }
    public int? AtendidoPorUsuarioId { get; set; }
    public string? ResultadoObservacao { get; set; }

    /// <summary>True quando a solicitação ainda está em aberto e já passou do prazo legal.</summary>
    public bool PrazoVencido { get; set; }

    /// <summary>Dias restantes até o prazo (negativo se vencido).</summary>
    public int DiasRestantes { get; set; }
}

public class CriarSolicitacaoTitularDto
{
    public int? PessoaId { get; set; }

    [MaxLength(150)]
    public string? NomeSolicitante { get; set; }

    [MaxLength(150)]
    public string? ContatoSolicitante { get; set; }

    [Required]
    public TipoSolicitacaoTitular Tipo { get; set; }

    [MaxLength(40)]
    public string? Canal { get; set; }

    [MaxLength(2000)]
    public string? Descricao { get; set; }
}

public class ConcluirSolicitacaoTitularDto
{
    [MaxLength(2000)]
    public string? Observacao { get; set; }
}

public class RecusarSolicitacaoTitularDto
{
    [Required(ErrorMessage = "O motivo da recusa é obrigatório")]
    [MaxLength(2000)]
    public string Motivo { get; set; } = string.Empty;
}
