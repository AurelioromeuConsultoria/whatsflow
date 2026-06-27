using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class KidsNotificacao : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    public int? CriancaPessoaId { get; set; }
    public virtual Pessoa? Crianca { get; set; }

    [Required]
    public int ResponsavelPessoaId { get; set; }
    public virtual Pessoa Responsavel { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; } = string.Empty; // "CHECKIN", "CHECKOUT", "ALERTA", "AVISO_GERAL", "AVISO_CRIANCA", "AVISO_RESPONSAVEL"

    [Required]
    [MaxLength(20)]
    public string Origem { get; set; } = "AUTOMATICA"; // "AUTOMATICA" ou "MANUAL"

    [Required]
    [MaxLength(1000)]
    public string Mensagem { get; set; } = string.Empty;

    public DateTime? EnviadoEm { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Enviado"; // "Enviado", "Falhou"

    public DateTime? LidoEm { get; set; }

    public int? CriadoByPessoaId { get; set; }

    [Required]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
