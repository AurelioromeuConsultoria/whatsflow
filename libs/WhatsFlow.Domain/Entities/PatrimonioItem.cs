using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class PatrimonioItem : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    public int CategoriaPatrimonioId { get; set; }
    public virtual CategoriaPatrimonio? CategoriaPatrimonio { get; set; }

    [MaxLength(100)]
    public string? Marca { get; set; }

    [MaxLength(100)]
    public string? Modelo { get; set; }

    [MaxLength(100)]
    public string? NumeroSerie { get; set; }

    public int Quantidade { get; set; } = 1;

    [MaxLength(100)]
    public string? Campus { get; set; }

    [MaxLength(150)]
    public string? Localizacao { get; set; }

    [MaxLength(100)]
    public string? MinisterioArea { get; set; }

    public int? ResponsavelPessoaId { get; set; }
    public virtual Pessoa? ResponsavelPessoa { get; set; }

    [MaxLength(30)]
    public string TipoAquisicao { get; set; } = "Comprado";

    public DateTime? DataAquisicao { get; set; }

    public decimal? ValorAquisicao { get; set; }

    public int? FornecedorId { get; set; }
    public virtual Fornecedor? Fornecedor { get; set; }

    [MaxLength(100)]
    public string? NumeroNotaFiscal { get; set; }

    public int? DespesaId { get; set; }
    public virtual Despesa? Despesa { get; set; }

    public int? CentroCustoId { get; set; }
    public virtual CentroCusto? CentroCusto { get; set; }

    public int? ProjetoId { get; set; }
    public virtual Projeto? Projeto { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "EmUso";

    [MaxLength(30)]
    public string EstadoConservacao { get; set; } = "Bom";

    public DateTime? DataUltimaAvaliacao { get; set; }

    public bool PossuiGarantia { get; set; }

    public DateTime? GarantiaAte { get; set; }

    public DateTime? DataUltimaManutencao { get; set; }

    public DateTime? DataProximaManutencao { get; set; }

    [MaxLength(500)]
    public string? FotoUrl { get; set; }

    [MaxLength(500)]
    public string? DocumentoUrl { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public virtual ICollection<PatrimonioMovimentacao> Movimentacoes { get; set; } = new List<PatrimonioMovimentacao>();
}
