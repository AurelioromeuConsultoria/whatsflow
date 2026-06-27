using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Pessoa : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [MaxLength(500)]
    public string? FotoUrl { get; set; }

    public DateTime? DataNascimento { get; set; }

    [Required]
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Adulto;

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    // Relacionamentos
    public virtual Usuario? Usuario { get; set; }
    public virtual ICollection<Visitante> Visitantes { get; set; } = new List<Visitante>();
    // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
}
