using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Escala : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EventoOcorrenciaId { get; set; }
    public virtual EventoOcorrencia EventoOcorrencia { get; set; } = null!;

    [Required]
    public int EquipeId { get; set; }
    public virtual Equipe Equipe { get; set; } = null!;

    [Required]
    public StatusEscala Status { get; set; } = StatusEscala.Rascunho;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public int? CriadoPorUsuarioId { get; set; }
    public virtual Usuario? CriadoPorUsuario { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? DataPublicacao { get; set; }

    public virtual ICollection<EscalaItem> Itens { get; set; } = new List<EscalaItem>();
}

public enum StatusEscala
{
    Rascunho = 1,
    Publicada = 2,
    Fechada = 3
}
