using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum TipoOrcamento
{
    Receita = 1,
    Despesa = 2
}

public class OrcamentoCategoria : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int Ano { get; set; }

    [Required]
    public TipoOrcamento Tipo { get; set; }

    public int? CategoriaReceitaId { get; set; }
    public virtual CategoriaReceita? CategoriaReceita { get; set; }

    public int? CategoriaDespesaId { get; set; }
    public virtual CategoriaDespesa? CategoriaDespesa { get; set; }

    [Required]
    public decimal ValorOrcado { get; set; }
}
