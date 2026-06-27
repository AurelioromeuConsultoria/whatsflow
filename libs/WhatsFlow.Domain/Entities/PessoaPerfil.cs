using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class PessoaPerfil : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    [Required]
    public PerfilPessoa Perfil { get; set; }

    [Required]
    public DateTime DataInicio { get; set; } = DateTime.Now;

    public DateTime? DataFim { get; set; }
}

