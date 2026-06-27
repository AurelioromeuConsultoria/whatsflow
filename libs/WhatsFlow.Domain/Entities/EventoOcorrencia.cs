using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class EventoOcorrencia : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EventoId { get; set; }
    public virtual Evento Evento { get; set; } = null!;

    public int? EventoRecorrenciaId { get; set; }
    public virtual EventoRecorrencia? EventoRecorrencia { get; set; }

    [Required]
    public DateTime DataHoraInicio { get; set; }

    public DateTime? DataHoraFim { get; set; }

    [Required]
    public StatusEventoOcorrencia Status { get; set; } = StatusEventoOcorrencia.Confirmado;

    [Required]
    public bool GeradaAutomaticamente { get; set; } = false;

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    /// <summary>Uma escala por equipe para esta ocorrência.</summary>
    public virtual ICollection<Escala> Escalas { get; set; } = new List<Escala>();
}

public enum StatusEventoOcorrencia
{
    Confirmado = 1,
    Cancelado = 2,
    Realizado = 3
}
