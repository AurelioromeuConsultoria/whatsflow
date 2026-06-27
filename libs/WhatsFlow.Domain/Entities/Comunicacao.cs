using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class ComunicacaoTemplate : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Objetivo { get; set; } = string.Empty;

    [Required]
    public CanalComunicacao Canal { get; set; } = CanalComunicacao.WhatsApp;

    [MaxLength(200)]
    public string? Assunto { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Corpo { get; set; } = string.Empty;

    [MaxLength(12000)]
    public string? CorpoHtml { get; set; }

    [Required]
    public StatusComunicacaoTemplate Status { get; set; } = StatusComunicacaoTemplate.Rascunho;

    [MaxLength(2000)]
    public string VariaveisPermitidas { get; set; } = string.Empty;

    public int Versao { get; set; } = 1;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
}

public class ComunicacaoCampanha : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Objetivo { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string PublicoAlvo { get; set; } = string.Empty;

    [Required]
    public StatusComunicacaoCampanha Status { get; set; } = StatusComunicacaoCampanha.Rascunho;

    [Required]
    public TipoOrigemComunicacao Origem { get; set; } = TipoOrigemComunicacao.Manual;

    public DateTime? DataAgendamento { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }

    public int? CriadoPorUsuarioId { get; set; }
    public virtual Usuario? CriadoPorUsuario { get; set; }

    public virtual ICollection<ComunicacaoCampanhaCanal> Canais { get; set; } = new List<ComunicacaoCampanhaCanal>();
    public virtual ICollection<ComunicacaoEntrega> Entregas { get; set; } = new List<ComunicacaoEntrega>();
}

public class ComunicacaoCampanhaCanal : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ComunicacaoCampanhaId { get; set; }
    public virtual ComunicacaoCampanha ComunicacaoCampanha { get; set; } = null!;

    public CanalComunicacao Canal { get; set; }
    public int? TemplateId { get; set; }
    public virtual ComunicacaoTemplate? Template { get; set; }
    public int Prioridade { get; set; }
}

public class ComunicacaoEntrega : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int? ComunicacaoCampanhaId { get; set; }
    public virtual ComunicacaoCampanha? ComunicacaoCampanha { get; set; }

    public int? DestinatarioPessoaId { get; set; }
    public virtual Pessoa? DestinatarioPessoa { get; set; }

    public int? DestinatarioVisitanteId { get; set; }
    public virtual Visitante? DestinatarioVisitante { get; set; }

    [Required]
    public CanalComunicacao Canal { get; set; }

    [Required]
    [MaxLength(300)]
    public string DestinoResolvido { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RemetenteResolvido { get; set; }

    [Required]
    [MaxLength(4000)]
    public string ConteudoFinal { get; set; } = string.Empty;

    [MaxLength(12000)]
    public string? ConteudoHtmlFinal { get; set; }

    [MaxLength(1000)]
    public string? MidiaUrl { get; set; }

    [Required]
    public StatusComunicacaoEntrega Status { get; set; } = StatusComunicacaoEntrega.Pendente;

    public int Tentativas { get; set; }
    public DateTime? ProcessadoEm { get; set; }
    public DateTime? EntregueEm { get; set; }

    [MaxLength(1000)]
    public string? Erro { get; set; }

    [MaxLength(100)]
    public string? ChaveDedupe { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}

public class ComunicacaoAutomacao : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Gatilho { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SegmentoAlvo { get; set; }

    public CanalComunicacao Canal { get; set; }
    public int? TemplateId { get; set; }
    public virtual ComunicacaoTemplate? Template { get; set; }
    public int DelayMinutos { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}

public class ComunicacaoPreferencia : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    [Required]
    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    [Required]
    public CanalComunicacao Canal { get; set; }

    [Required]
    public StatusPreferenciaCanal Status { get; set; } = StatusPreferenciaCanal.Permitido;

    [MaxLength(60)]
    public string? OrigemConsentimento { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
}

public class ComunicacaoSegmento : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    [Required]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descricao { get; set; }

    [Required]
    [MaxLength(40)]
    public string PublicoAlvo { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
    public bool Padrao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
}

public enum CanalComunicacao
{
    WhatsApp = 1,
    Email = 2,
    Push = 3,
    NotificacaoInterna = 4
}

public enum StatusComunicacaoTemplate
{
    Rascunho = 1,
    Ativo = 2,
    Arquivado = 3
}

public enum StatusComunicacaoCampanha
{
    Rascunho = 1,
    Agendada = 2,
    Processando = 3,
    Concluida = 4,
    ConcluidaComFalhas = 5,
    Cancelada = 6
}

public enum StatusComunicacaoEntrega
{
    Pendente = 1,
    Reservado = 2,
    Enviado = 3,
    Entregue = 4,
    Falhou = 5,
    Cancelado = 6,
    IgnoradoPorPreferencia = 7
}

public enum StatusPreferenciaCanal
{
    Permitido = 1,
    Bloqueado = 2
}

public enum TipoOrigemComunicacao
{
    Manual = 1,
    Automatica = 2
}
