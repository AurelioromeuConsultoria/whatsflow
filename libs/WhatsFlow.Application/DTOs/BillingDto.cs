using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class AssinarTenantDto
{
    public int? TenantId { get; set; } // se null, usa o tenant do contexto

    [Required]
    public int PlanoId { get; set; }

    public CicloCobranca Ciclo { get; set; } = CicloCobranca.Mensal;

    public MetodoPagamentoAssinatura? MetodoPagamento { get; set; }

    // Dados do cliente para o gateway (a igreja como cliente da plataforma)
    [Required]
    [MaxLength(150)]
    public string NomeCliente { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? CpfCnpj { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }
}

public class AssinaturaDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? MetodoPagamento { get; set; }
    public DateTime? TrialFim { get; set; }
    public DateTime? VigenciaInicio { get; set; }
    public DateTime? ProximaCobranca { get; set; }
    public DateTime? CanceladaEm { get; set; }
    public string? GatewaySubscriptionId { get; set; }
    public DateTime DataCriacao { get; set; }

    public bool EmTrial { get; set; }
    public int? DiasTrialRestantes { get; set; }

    /// <summary>Preenchido apenas nas listagens de admin de plataforma.</summary>
    public string? TenantNome { get; set; }
}

public class CicloBillingResultado
{
    public int TrialsExpirados { get; set; }
    public int Suspensos { get; set; }
    public int AvisosTrialEnviados { get; set; }
}

public class PlanoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal PrecoMensal { get; set; }
    public decimal? PrecoAnual { get; set; }
    public int Ordem { get; set; }
}

public class FaturaDto
{
    public int Id { get; set; }
    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Vencimento { get; set; }
    public DateTime? PagaEm { get; set; }
    public string? LinkPagamento { get; set; }
    public string? PixCopiaECola { get; set; }
}
