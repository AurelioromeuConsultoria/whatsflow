using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class PatrimonioMovimentacao : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    public int PatrimonioItemId { get; set; }
    public virtual PatrimonioItem? PatrimonioItem { get; set; }

    [Required]
    [MaxLength(40)]
    public string TipoMovimentacao { get; set; } = string.Empty;

    public DateTime DataMovimentacao { get; set; } = DateTime.Now;

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

    public int? UsuarioId { get; set; }
    public virtual Usuario? Usuario { get; set; }

    [MaxLength(150)]
    public string? UsuarioNome { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
