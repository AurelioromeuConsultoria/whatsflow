using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsFlow.Domain.Entities;

/// <summary>
/// Representa uma foto dentro de uma galeria. Persistido no banco para que a listagem
/// funcione mesmo quando o backend roda localmente e os arquivos estão em outro servidor (ex.: produção).
/// </summary>
public class GaleriaFotoItem : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    public int GaleriaFotoId { get; set; }
    [ForeignKey("GaleriaFotoId")]
    public virtual GaleriaFoto GaleriaFoto { get; set; } = null!;

    [Required]
    [MaxLength(260)]
    public string NomeArquivo { get; set; } = string.Empty;

    public bool Destaque { get; set; }

    public int Ordem { get; set; }
}
