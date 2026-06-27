using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class KidsOcorrencia : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int CriancaPessoaId { get; set; }
    public virtual Pessoa Crianca { get; set; } = null!;

    public int? CheckinId { get; set; }
    public virtual KidsCheckin? Checkin { get; set; }

    [Required]
    [MaxLength(40)]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Aberta";

    public bool RequerContatoResponsavel { get; set; }
    public DateTime? ContatoResponsavelRealizadoEm { get; set; }
    public int? ContatoResponsavelPorPessoaId { get; set; }
    public virtual Pessoa? ContatoResponsavelPor { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    [Required]
    public int RegistradoPorPessoaId { get; set; }
    public virtual Pessoa RegistradoPor { get; set; } = null!;

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }
    public DateTime? EncerradoEm { get; set; }
    public int? EncerradoPorPessoaId { get; set; }
    public virtual Pessoa? EncerradoPor { get; set; }
    public bool VisivelAoResponsavel { get; set; }
}
