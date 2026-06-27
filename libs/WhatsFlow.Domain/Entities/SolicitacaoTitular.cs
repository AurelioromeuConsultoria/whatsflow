using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public enum TipoSolicitacaoTitular
{
    Acesso = 1,
    Exportacao = 2,
    Correcao = 3,
    Eliminacao = 4,
    RevogacaoConsentimento = 5,
    Outro = 99
}

public enum StatusSolicitacaoTitular
{
    Aberta = 1,
    EmAtendimento = 2,
    Concluida = 3,
    Recusada = 4
}

/// <summary>
/// Requisição de um titular de dados (LGPD, Art. 18/19). Registra o pedido, o prazo
/// legal de resposta (15 dias) e o desfecho, criando trilha de conformidade.
/// </summary>
public class SolicitacaoTitular : ITenantEntity
{
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>Titular registrado, quando aplicável (pode ser nulo para não-cadastrados).</summary>
    public int? PessoaId { get; set; }
    public virtual Pessoa? Pessoa { get; set; }

    [MaxLength(150)]
    public string? NomeSolicitante { get; set; }

    [MaxLength(150)]
    public string? ContatoSolicitante { get; set; }

    [Required]
    public TipoSolicitacaoTitular Tipo { get; set; }

    [Required]
    public StatusSolicitacaoTitular Status { get; set; } = StatusSolicitacaoTitular.Aberta;

    /// <summary>Canal de entrada: "area_membro", "email", "whatsapp", "admin", etc.</summary>
    [MaxLength(40)]
    public string? Canal { get; set; }

    [MaxLength(2000)]
    public string? Descricao { get; set; }

    [Required]
    public DateTime SolicitadoEm { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime PrazoLimite { get; set; }

    public DateTime? AtendidoEm { get; set; }

    /// <summary>Usuário que concluiu/recusou a solicitação.</summary>
    public int? AtendidoPorUsuarioId { get; set; }

    [MaxLength(2000)]
    public string? ResultadoObservacao { get; set; }
}
