using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class EventoRecorrencia : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EventoId { get; set; }
    public virtual Evento Evento { get; set; } = null!;

    [Required]
    public DayOfWeek DiaSemana { get; set; }

    [Required]
    public TimeSpan HoraInicio { get; set; }

    public TimeSpan? HoraFim { get; set; }

    [Required]
    public PeriodicidadeRecorrencia Periodicidade { get; set; } = PeriodicidadeRecorrencia.Semanal;

    [Required]
    public DateTime DataInicioVigencia { get; set; }

    public DateTime? DataFimVigencia { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public virtual ICollection<EventoOcorrencia> Ocorrencias { get; set; } = new List<EventoOcorrencia>();
}

public enum PeriodicidadeRecorrencia
{
    Semanal = 1,
    Quinzenal = 2,
    Mensal = 3
}
