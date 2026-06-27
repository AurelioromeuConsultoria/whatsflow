using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class ResponsavelCrianca : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int CriancaPessoaId { get; set; }
    public virtual Pessoa Crianca { get; set; } = null!;

    [Required]
    public int ResponsavelPessoaId { get; set; }
    public virtual Pessoa Responsavel { get; set; } = null!;

    [Required]
    public bool PodeRetirar { get; set; } = true;

    [MaxLength(50)]
    public string? Parentesco { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
}

