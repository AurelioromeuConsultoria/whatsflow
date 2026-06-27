using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

/// <summary>Uma “vaga” no modelo: quantas pessoas deste cargo (ou qualquer) são necessárias.</summary>
public class EscalaModeloItem : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    [Required]
    public int EscalaModeloId { get; set; }
    public virtual EscalaModelo EscalaModelo { get; set; } = null!;

    /// <summary>Null = qualquer cargo da equipe; senão, só voluntários com este cargo.</summary>
    public int? CargoId { get; set; }
    public virtual Cargo? Cargo { get; set; }

    [Required]
    public int Quantidade { get; set; } = 1;

    [Required]
    public int Ordem { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
