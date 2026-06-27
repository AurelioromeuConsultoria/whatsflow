using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum CicloCobranca
{
    Mensal = 1,
    Anual = 2
}

/// <summary>
/// Catálogo de planos de assinatura da plataforma (VerboPlus). É global — NÃO é
/// ITenantEntity, pois os planos são os mesmos para todos os tenants.
/// </summary>
public class Plano
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Required]
    public decimal PrecoMensal { get; set; }

    public decimal? PrecoAnual { get; set; }

    /// <summary>Limites opcionais (null = ilimitado). Sem limites duros no MVP.</summary>
    public int? MaxUsuarios { get; set; }
    public int? MaxMembros { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    public int Ordem { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
