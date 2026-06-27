using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class KidsPreCheckin : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int CriancaPessoaId { get; set; }
    public virtual Pessoa Crianca { get; set; } = null!;

    [Required]
    public int ResponsavelPessoaId { get; set; }
    public virtual Pessoa Responsavel { get; set; } = null!;

    public int? EventoOcorrenciaId { get; set; }
    public virtual EventoOcorrencia? EventoOcorrencia { get; set; }

    public int? CheckinId { get; set; }
    public virtual KidsCheckin? Checkin { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    [Required]
    [MaxLength(80)]
    public string QrToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CodigoCurto { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Expired, Cancelled

    [Required]
    public DateTime ExpiraEm { get; set; }

    [MaxLength(500)]
    public string? ObservacoesResponsavel { get; set; }

    [Required]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmadoEm { get; set; }

    public int? ConfirmadoPorPessoaId { get; set; }
    public virtual Pessoa? ConfirmadoPor { get; set; }

    public DateTime? CanceladoEm { get; set; }

    public int? CanceladoPorPessoaId { get; set; }
    public virtual Pessoa? CanceladoPor { get; set; }

    [MaxLength(500)]
    public string? CancelamentoMotivo { get; set; }
}
