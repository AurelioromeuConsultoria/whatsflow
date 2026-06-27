using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class EscalaItem : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EscalaId { get; set; }
    public virtual Escala Escala { get; set; } = null!;

    [Required]
    public int EquipeId { get; set; }
    public virtual Equipe Equipe { get; set; } = null!;

    public int? CargoId { get; set; }
    public virtual Cargo? Cargo { get; set; }

    // Ancora histórica: mantém quem serviu mesmo após remoção do vínculo de equipe
    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    // Nullable: quando o voluntário é removido da equipe, vira null via ON DELETE SET NULL
    public int? VoluntarioId { get; set; }
    public virtual Voluntario? Voluntario { get; set; }

    [Required]
    public int Ordem { get; set; } = 0;

    [Required]
    public bool ConflitoAprovado { get; set; } = false;

    [MaxLength(500)]
    public string? MotivoExcecao { get; set; }

    public int? AprovadoPorUsuarioId { get; set; }
    public virtual Usuario? AprovadoPorUsuario { get; set; }

    public DateTime? AprovadoEm { get; set; }
    [Required]
    public StatusEscalaItem Status { get; set; } = StatusEscalaItem.Pendente;
    public DateTime? DataConvite { get; set; }
    public DateTime? DataConfirmacao { get; set; }
    public DateTime? DataRecusa { get; set; }
    public DateTime? DataLembrete7DiasEnviado { get; set; }
    public DateTime? DataLembrete24HorasEnviado { get; set; }

    [MaxLength(500)]
    public string? MotivoRecusa { get; set; }

    public int? RespondidoPorUsuarioId { get; set; }
    public virtual Usuario? RespondidoPorUsuario { get; set; }

    [MaxLength(500)]
    public string? ObservacaoOperacional { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.Now;
}

public enum StatusEscalaItem
{
    Pendente = 1,
    Confirmado = 2,
    Recusado = 3,
    Substituido = 4,
    Serviu = 5,
    Faltou = 6
}
