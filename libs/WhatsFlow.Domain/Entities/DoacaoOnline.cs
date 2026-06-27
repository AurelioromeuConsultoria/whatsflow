using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum StatusDoacaoOnline
{
    Pendente = 1,
    AguardandoPagamento = 2,
    Confirmada = 3,
    Expirada = 4,
    Cancelada = 5,
    Falhou = 6,
    Estornada = 7
}

public enum MetodoPagamentoDoacao
{
    Pix = 1,
    CartaoCredito = 2,
    Boleto = 3
}

public class DoacaoOnline : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    public int? FinalidadeDoacaoId { get; set; }
    public virtual FinalidadeDoacao? FinalidadeDoacao { get; set; }

    public int? PessoaId { get; set; }
    public virtual Pessoa? Pessoa { get; set; }

    [Required]
    [MaxLength(120)]
    public string NomeDoador { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? WhatsApp { get; set; }

    [MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Documento { get; set; }

    public bool Anonima { get; set; }

    [Required]
    public decimal Valor { get; set; }

    [Required]
    public MetodoPagamentoDoacao MetodoPagamento { get; set; } = MetodoPagamentoDoacao.Pix;

    [Required]
    public StatusDoacaoOnline Status { get; set; } = StatusDoacaoOnline.Pendente;

    [MaxLength(40)]
    public string Provider { get; set; } = "manual";

    [MaxLength(120)]
    public string? ExternalPaymentId { get; set; }

    [MaxLength(64)]
    public string? ReciboToken { get; set; }

    [MaxLength(2000)]
    public string? PixCopiaECola { get; set; }

    public string? PixQrCodeUrl { get; set; }

    public DateTime? DataVencimento { get; set; }

    public DateTime? DataConfirmacao { get; set; }

    public int? ReceitaId { get; set; }
    public virtual Receita? Receita { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
