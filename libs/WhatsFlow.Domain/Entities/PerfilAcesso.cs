using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class PerfilAcesso : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descricao { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public virtual ICollection<PerfilAcessoPermissao> Permissoes { get; set; } = new List<PerfilAcessoPermissao>();
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

public class PerfilAcessoPermissao : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;

    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int PerfilAcessoId { get; set; }

    public virtual PerfilAcesso PerfilAcesso { get; set; } = null!;

    [Required]
    [MaxLength(80)]
    public string Recurso { get; set; } = string.Empty;

    public bool PodeVer { get; set; }
    public bool PodeEditar { get; set; }
    public bool PodeExcluir { get; set; }
}
