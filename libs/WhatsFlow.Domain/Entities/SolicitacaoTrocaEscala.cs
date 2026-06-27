using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class SolicitacaoTrocaEscala : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EscalaItemId { get; set; }
    public virtual EscalaItem EscalaItem { get; set; } = null!;

    [Required]
    public int VoluntarioSolicitanteId { get; set; }
    public virtual Voluntario VoluntarioSolicitante { get; set; } = null!;

    public int? VoluntarioSubstitutoId { get; set; }
    public virtual Voluntario? VoluntarioSubstituto { get; set; }

    [Required]
    public StatusSolicitacaoTrocaEscala Status { get; set; } = StatusSolicitacaoTrocaEscala.Pendente;

    [MaxLength(500)]
    public string? Motivo { get; set; }

    [MaxLength(500)]
    public string? ObservacaoResposta { get; set; }

    public int? RespondidoPorUsuarioId { get; set; }
    public virtual Usuario? RespondidoPorUsuario { get; set; }

    public DateTime DataSolicitacao { get; set; } = DateTime.Now;
    public DateTime? DataResposta { get; set; }
}

public enum StatusSolicitacaoTrocaEscala
{
    Pendente = 1,
    Aprovada = 2,
    Rejeitada = 3,
    Cancelada = 4
}
