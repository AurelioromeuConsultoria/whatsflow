using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Visitante : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;
    
    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;
    
    [Required]
    public DateTime DataVisita { get; set; }
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }
    
    [Required]
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    
    // Relacionamento com mensagens agendadas
    public virtual ICollection<MensagemAgendada> MensagensAgendadas { get; set; } = new List<MensagemAgendada>();
}
