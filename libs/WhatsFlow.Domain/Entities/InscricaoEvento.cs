using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class InscricaoEvento : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    // Relacionamento com Evento
    [Required]
    public int EventoId { get; set; }
    public virtual Evento Evento { get; set; } = null!;

    // Dados do Participante
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string WhatsApp { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    // Status e Informações Adicionais
    [Required]
    public StatusInscricao Status { get; set; } = StatusInscricao.Pendente;

    public int QuantidadeAcompanhantes { get; set; } = 0;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    /// <summary>
    /// JSON com valores dos campos dinâmicos do formulário (ex.: {"rg":"12.345.678-9","quantidadeAcompanhantes":2}).
    /// Colunas fixas são apenas Nome, WhatsApp, Email, Observações.
    /// </summary>
    [MaxLength(2000)]
    public string? DadosInscricao { get; set; }

    [MaxLength(500)]
    public string? ObservacoesInternas { get; set; }

    public DateTime DataInscricao { get; set; } = DateTime.Now;
    public DateTime? DataConfirmacao { get; set; }
    public DateTime? DataCancelamento { get; set; }
}





