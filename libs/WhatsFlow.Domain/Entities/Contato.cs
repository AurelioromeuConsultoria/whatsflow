using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>
/// Contato é a entidade central de destinatário do WhatsFlow (substitui Pessoa/Visitante do legado).
/// Telefone armazenado preferencialmente em formato internacional (E.164). Único por tenant.
/// </summary>
public class Contato : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Telefone WhatsApp em formato internacional (E.164), ex: 5511999998888.</summary>
    [Required]
    [MaxLength(20)]
    public string TelefoneWhatsApp { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Documento { get; set; }

    /// <summary>Empresa/organização do contato.</summary>
    [MaxLength(150)]
    public string? Organizacao { get; set; }

    [MaxLength(2000)]
    public string? Observacoes { get; set; }

    /// <summary>Origem do cadastro (ex: import, api, manual, landing).</summary>
    [MaxLength(60)]
    public string? Origem { get; set; }

    [Required]
    public ContatoStatus Status { get; set; } = ContatoStatus.Ativo;

    /// <summary>Consentimento para receber campanhas ativas.</summary>
    [Required]
    public bool OptIn { get; set; }

    public DateTime? DataOptIn { get; set; }
    public DateTime? DataOptOut { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    public virtual ICollection<ContatoTag> ContatoTags { get; set; } = new List<ContatoTag>();
}

public enum ContatoStatus
{
    Ativo = 1,
    Inativo = 2,
    Bloqueado = 3
}
