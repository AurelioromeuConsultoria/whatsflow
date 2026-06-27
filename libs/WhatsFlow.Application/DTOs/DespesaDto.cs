using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class DespesaDto
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public StatusDespesa Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public string? ComprovanteUrl { get; set; }
    public int? FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public int? CategoriaDespesaId { get; set; }
    public string? CategoriaDespesaNome { get; set; }
    public int? ContaBancariaId { get; set; }
    public string? ContaBancariaNome { get; set; }
    public int? CentroCustoId { get; set; }
    public string? CentroCustoNome { get; set; }
    public int? ProjetoId { get; set; }
    public string? ProjetoNome { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public bool Recorrente { get; set; }
    public TipoRecorrencia? TipoRecorrencia { get; set; }
    public string? TipoRecorrenciaDescricao { get; set; }
    public int? RecorrenciaOriginalId { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class VencimentosResumoDto
{
    public decimal TotalVencido { get; set; }
    public decimal TotalHoje { get; set; }
    public decimal TotalProximos7Dias { get; set; }
    public decimal TotalProximos30Dias { get; set; }
    public List<DespesaDto> Vencidas { get; set; } = new();
    public List<DespesaDto> Hoje { get; set; } = new();
    public List<DespesaDto> Proximos7Dias { get; set; } = new();
    public List<DespesaDto> Proximos30Dias { get; set; } = new();
}

public class CriarDespesaDto
{
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "Data de vencimento é obrigatória")]
    public DateTime DataVencimento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public StatusDespesa Status { get; set; } = StatusDespesa.Pendente;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    [MaxLength(500)]
    public string? ComprovanteUrl { get; set; }

    public int? FornecedorId { get; set; }

    public int? CategoriaDespesaId { get; set; }

    public int? ContaBancariaId { get; set; }

    public int? CentroCustoId { get; set; }

    public int? ProjetoId { get; set; }

    public int? UsuarioId { get; set; }
    public bool Recorrente { get; set; } = false;
    public TipoRecorrencia? TipoRecorrencia { get; set; }
}

public class AtualizarDespesaDto
{
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "Data de vencimento é obrigatória")]
    public DateTime DataVencimento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public StatusDespesa Status { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    [MaxLength(500)]
    public string? ComprovanteUrl { get; set; }

    public int? FornecedorId { get; set; }

    public int? CategoriaDespesaId { get; set; }

    public int? ContaBancariaId { get; set; }

    public int? CentroCustoId { get; set; }

    public int? ProjetoId { get; set; }

    public int? UsuarioId { get; set; }
    public bool Recorrente { get; set; } = false;
    public TipoRecorrencia? TipoRecorrencia { get; set; }
}
