using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class FinalidadeDoacao : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(140)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? DescricaoPublica { get; set; }

    [MaxLength(500)]
    public string? ImagemUrl { get; set; }

    [MaxLength(40)]
    public string? CorHex { get; set; }

    [MaxLength(300)]
    public string? ValoresSugeridos { get; set; }

    public decimal? ValorMinimo { get; set; }

    public int Ordem { get; set; }

    public bool Ativo { get; set; } = true;

    public bool VisivelPortal { get; set; } = true;

    public bool PermiteAnonimo { get; set; } = true;

    public bool PermitePix { get; set; } = true;

    public bool PermiteCartaoCredito { get; set; }

    public int? CategoriaReceitaId { get; set; }
    public virtual CategoriaReceita? CategoriaReceita { get; set; }

    public int? ContaBancariaId { get; set; }
    public virtual ContaBancaria? ContaBancaria { get; set; }

    public int? CentroCustoId { get; set; }
    public virtual CentroCusto? CentroCusto { get; set; }

    public int? ProjetoId { get; set; }
    public virtual Projeto? Projeto { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
