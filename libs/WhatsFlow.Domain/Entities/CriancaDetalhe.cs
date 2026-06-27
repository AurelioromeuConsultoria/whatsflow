using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class CriancaDetalhe : ITenantEntity
{
    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    [MaxLength(500)]
    public string? Alergias { get; set; }

    [MaxLength(500)]
    public string? RestricoesAlimentares { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    [Required]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
}
