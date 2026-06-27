using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class Enquete : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [Required]
    public DateTime DataInicio { get; set; }

    [Required]
    public DateTime DataFim { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    public bool PermitirMultiplaEscolha { get; set; } = false;

    [Required]
    public bool PermitirVotoAnonimo { get; set; } = true;

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    // Relacionamento com opções da enquete
    public virtual ICollection<EnqueteOpcao> Opcoes { get; set; } = new List<EnqueteOpcao>();

    // Relacionamento com votos
    public virtual ICollection<EnqueteVoto> Votos { get; set; } = new List<EnqueteVoto>();
}

public class EnqueteOpcao : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EnqueteId { get; set; }
    public virtual Enquete Enquete { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Texto { get; set; } = string.Empty;

    public int Ordem { get; set; } = 0;

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    // Relacionamento com votos
    public virtual ICollection<EnqueteVoto> Votos { get; set; } = new List<EnqueteVoto>();
}

public class EnqueteVoto : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EnqueteId { get; set; }
    public virtual Enquete Enquete { get; set; } = null!;

    [Required]
    public int EnqueteOpcaoId { get; set; }
    public virtual EnqueteOpcao Opcao { get; set; } = null!;

    // Usuário que votou (opcional se permitir voto anônimo)
    public int? UsuarioId { get; set; }
    public virtual Usuario? Usuario { get; set; }

    [MaxLength(100)]
    public string? NomeAnonimo { get; set; }

    [Required]
    public DateTime DataVoto { get; set; } = DateTime.Now;
}
