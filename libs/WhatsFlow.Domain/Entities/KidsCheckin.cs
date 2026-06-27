using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class KidsCheckin : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int CriancaPessoaId { get; set; }
    public virtual Pessoa Crianca { get; set; } = null!;

    [Required]
    public DateTime CheckinTime { get; set; } = DateTime.UtcNow;

    public DateTime? CheckoutTime { get; set; }

    public int? CheckinByPessoaId { get; set; }
    public virtual Pessoa? CheckinBy { get; set; }

    public int? CheckoutByPessoaId { get; set; }
    public virtual Pessoa? CheckoutBy { get; set; }

    [Required]
    [MaxLength(20)]
    public string Metodo { get; set; } = "ADMIN"; // "QR", "PIN", "ADMIN"

    [Required]
    [MaxLength(50)]
    public string CodigoSessao { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? TokenRetirada { get; set; }

    [MaxLength(10)]
    public string? PinRetirada { get; set; }

    public DateTime? TokenRetiradaExpiraEm { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "CheckedIn"; // "CheckedIn", "CheckedOut"

    public int? RetiradaConfirmadaPorPessoaId { get; set; }

    [MaxLength(20)]
    public string? RetiradaMetodo { get; set; }

    public bool RetiradaEmModoExcecao { get; set; }

    [MaxLength(500)]
    public string? RetiradaMotivoExcecao { get; set; }

    [MaxLength(200)]
    public string? RetiradaPessoaNome { get; set; }

    [MaxLength(50)]
    public string? RetiradaPessoaDocumento { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }
}
