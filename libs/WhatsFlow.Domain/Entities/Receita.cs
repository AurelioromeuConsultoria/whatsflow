using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum StatusReceita
{
    Pendente = 1,
    Recebida = 2,
    Cancelada = 3
}

public class Receita : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    public decimal Valor { get; set; }

    [Required]
    public DateTime DataRecebimento { get; set; }

    public DateTime? DataConfirmacao { get; set; }

    [Required]
    public StatusReceita Status { get; set; } = StatusReceita.Pendente;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    [MaxLength(500)]
    public string? ComprovanteUrl { get; set; }

    // Relacionamentos
    public int? CategoriaReceitaId { get; set; }
    public virtual CategoriaReceita? CategoriaReceita { get; set; }

    public int? ContaBancariaId { get; set; }
    public virtual ContaBancaria? ContaBancaria { get; set; }

    public int? CentroCustoId { get; set; }
    public virtual CentroCusto? CentroCusto { get; set; }

    public int? ProjetoId { get; set; }
    public virtual Projeto? Projeto { get; set; }

    public int? UsuarioId { get; set; }
    public virtual Usuario? Usuario { get; set; }

    public int? PessoaId { get; set; }
    public virtual Pessoa? Pessoa { get; set; }

    public bool Recorrente { get; set; } = false;
    public TipoRecorrencia? TipoRecorrencia { get; set; }
    public int? RecorrenciaOriginalId { get; set; }
    public virtual Receita? RecorrenciaOriginal { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
