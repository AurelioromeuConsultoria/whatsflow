using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class KidsConteudoAula : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Tema { get; set; }

    [MaxLength(300)]
    public string? Versiculo { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Resumo { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? AtividadeEmCasa { get; set; }

    [MaxLength(1000)]
    public string? ObservacaoResponsavel { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Published, Archived

    [Required]
    public DateTime DataReferencia { get; set; }

    public int? EventoOcorrenciaId { get; set; }
    public virtual EventoOcorrencia? EventoOcorrencia { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    public DateTime? PublicadoEm { get; set; }

    public int? PublicadoPorPessoaId { get; set; }
    public virtual Pessoa? PublicadoPor { get; set; }

    [Required]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? AtualizadoEm { get; set; }

    public virtual ICollection<KidsConteudoAulaAnexo> Anexos { get; set; } = new List<KidsConteudoAulaAnexo>();
}
