using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Voluntario : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    [Required]
    public int EquipeId { get; set; }
    public virtual Equipe Equipe { get; set; } = null!;

    [Required]
    public int CargoId { get; set; }
    public virtual Cargo Cargo { get; set; } = null!;

    /// <summary>Máximo de escalas no mês (null = sem limite). Usado na geração automática.</summary>
    public int? MaxEscalasPorMes { get; set; }

    [Required]
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public virtual ICollection<IndisponibilidadeVoluntario> Indisponibilidades { get; set; } = new List<IndisponibilidadeVoluntario>();
}
