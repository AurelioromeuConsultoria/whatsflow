using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum TipoConsentimento
{
    PoliticaPrivacidade = 1,
    TermosDeUso = 2,
    ConsentimentoParental = 3
}

/// <summary>
/// Trilha de consentimento (append-only) para LGPD: registra qual documento foi aceito,
/// em qual versão, quando, por qual canal e — no caso de menores — por qual responsável.
/// Revogações são marcadas em <see cref="RevogadoEm"/>, sem apagar o histórico.
/// </summary>
public class ConsentimentoRegistro : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>Titular do dado. No consentimento parental, é a criança.</summary>
    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    [Required]
    public TipoConsentimento Tipo { get; set; }

    [Required]
    [MaxLength(20)]
    public string VersaoDocumento { get; set; } = string.Empty;

    [Required]
    public DateTime AceitoEm { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? IpOrigem { get; set; }

    /// <summary>Canal/origem do aceite, ex.: "cadastro_publico", "kids_cadastro".</summary>
    [MaxLength(60)]
    public string? Origem { get; set; }

    /// <summary>
    /// No consentimento parental, qual responsável concedeu (diferente de <see cref="PessoaId"/>).
    /// No autoconsentimento, aponta para o próprio titular.
    /// </summary>
    public int? ConcedidoPorPessoaId { get; set; }
    public virtual Pessoa? ConcedidoPor { get; set; }

    public DateTime? RevogadoEm { get; set; }
}
