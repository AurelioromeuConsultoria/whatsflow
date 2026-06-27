using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class ConfiguracaoMensagem : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string TextoMensagem { get; set; } = string.Empty;
    
    public int DiasAposVisita { get; set; }
    
    public TimeSpan HorarioEnvio { get; set; }
    
    public bool Ativo { get; set; } = true;
    
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    
    // Relacionamento com mensagens agendadas
    public virtual ICollection<MensagemAgendada> MensagensAgendadas { get; set; } = new List<MensagemAgendada>();
}
