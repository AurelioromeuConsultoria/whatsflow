using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class MensagemAgendada : ITenantEntity
{
    public int Id { get; set; }

    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;
    
    // Destinatário migrado de Visitante para Contato (Etapa 4).
    public int ContatoId { get; set; }
    public virtual Contato Contato { get; set; } = null!;
    
    public int ConfiguracaoMensagemId { get; set; }
    public virtual ConfiguracaoMensagem ConfiguracaoMensagem { get; set; } = null!;
    
    public DateTime DataAgendamento { get; set; }
    
    public DateTime DataEnvio { get; set; }
    
    public StatusMensagem Status { get; set; } = StatusMensagem.Agendada;
    
    [MaxLength(1000)]
    public string TextoFinal { get; set; } = string.Empty;
    
    public DateTime? DataProcessamento { get; set; }
    
    [MaxLength(500)]
    public string? LogErro { get; set; }
    
    public DateTime DataCriacao { get; set; } = DateTime.Now;
}

public enum StatusMensagem
{
    Agendada = 1,
    ProntaParaEnvio = 2,
    Enviada = 3,
    Erro = 4,
    Cancelada = 5,
    /// <summary>Reservada para processamento; evita dupla execução entre instâncias.</summary>
    EmProcessamento = 6
}
