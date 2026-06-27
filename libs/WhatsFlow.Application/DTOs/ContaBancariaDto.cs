using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Application.DTOs;

public class ContaBancariaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Conta { get; set; }
    public string? TipoConta { get; set; }
    public decimal SaldoInicial { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarContaBancariaDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Banco { get; set; }

    [MaxLength(20)]
    public string? Agencia { get; set; }

    [MaxLength(20)]
    public string? Conta { get; set; }

    [MaxLength(10)]
    public string? TipoConta { get; set; }

    public decimal SaldoInicial { get; set; } = 0;
}

public class AtualizarContaBancariaDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Banco { get; set; }

    [MaxLength(20)]
    public string? Agencia { get; set; }

    [MaxLength(20)]
    public string? Conta { get; set; }

    [MaxLength(10)]
    public string? TipoConta { get; set; }

    public decimal SaldoInicial { get; set; }

    public bool Ativo { get; set; }
}
