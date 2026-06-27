using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class FinalidadeDoacaoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DescricaoPublica { get; set; }
    public string? ImagemUrl { get; set; }
    public string? CorHex { get; set; }
    public decimal[] ValoresSugeridos { get; set; } = [];
    public decimal? ValorMinimo { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; }
    public bool VisivelPortal { get; set; }
    public bool PermiteAnonimo { get; set; }
    public bool PermitePix { get; set; }
    public bool PermiteCartaoCredito { get; set; }
    public int? CategoriaReceitaId { get; set; }
    public string? CategoriaReceitaNome { get; set; }
    public int? ContaBancariaId { get; set; }
    public string? ContaBancariaNome { get; set; }
    public int? CentroCustoId { get; set; }
    public string? CentroCustoNome { get; set; }
    public int? ProjetoId { get; set; }
    public string? ProjetoNome { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class SalvarFinalidadeDoacaoDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(140)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? DescricaoPublica { get; set; }

    [MaxLength(500)]
    public string? ImagemUrl { get; set; }

    [MaxLength(40)]
    public string? CorHex { get; set; }

    public decimal[] ValoresSugeridos { get; set; } = [];

    public decimal? ValorMinimo { get; set; }

    public int Ordem { get; set; }

    public bool Ativo { get; set; } = true;

    public bool VisivelPortal { get; set; } = true;

    public bool PermiteAnonimo { get; set; } = true;

    public bool PermitePix { get; set; } = true;

    public bool PermiteCartaoCredito { get; set; }

    public int? CategoriaReceitaId { get; set; }

    public int? ContaBancariaId { get; set; }

    public int? CentroCustoId { get; set; }

    public int? ProjetoId { get; set; }
}

public class CriarDoacaoOnlineDto
{
    public int? FinalidadeDoacaoId { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(120)]
    public string NomeDoador { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? WhatsApp { get; set; }

    [MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Documento { get; set; }

    public bool Anonima { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    public MetodoPagamentoDoacao MetodoPagamento { get; set; } = MetodoPagamentoDoacao.Pix;
}

public class DoacaoOnlineDto
{
    public int Id { get; set; }
    public int? FinalidadeDoacaoId { get; set; }
    public string? FinalidadeNome { get; set; }
    public string NomeDoador { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public bool Anonima { get; set; }
    public decimal Valor { get; set; }
    public MetodoPagamentoDoacao MetodoPagamento { get; set; }
    public string MetodoPagamentoDescricao { get; set; } = string.Empty;
    public StatusDoacaoOnline Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ReciboToken { get; set; }
    public bool ReciboDisponivel { get; set; }
    public string? PixCopiaECola { get; set; }
    public string? PixQrCodeUrl { get; set; }
    public DateTime? DataVencimento { get; set; }
    public DateTime? DataConfirmacao { get; set; }
    public int? ReceitaId { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class DoacaoReciboDto
{
    public string Token { get; set; } = string.Empty;
    public int DoacaoId { get; set; }
    public string FinalidadeNome { get; set; } = string.Empty;
    public string NomeDoador { get; set; } = string.Empty;
    public bool Anonima { get; set; }
    public decimal Valor { get; set; }
    public MetodoPagamentoDoacao MetodoPagamento { get; set; }
    public string MetodoPagamentoDescricao { get; set; } = string.Empty;
    public StatusDoacaoOnline Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime DataConfirmacao { get; set; }
    public int? ReceitaId { get; set; }
}
