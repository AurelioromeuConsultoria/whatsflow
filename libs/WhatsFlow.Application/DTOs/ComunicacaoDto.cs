using WhatsFlow.Domain.Entities;
using WhatsFlow.Application.DTOs.Auditoria;

namespace WhatsFlow.Application.DTOs;

public class ComunicacaoTemplateResumoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public CanalComunicacao Canal { get; set; }
    public StatusComunicacaoTemplate Status { get; set; }
    public int Versao { get; set; }
}

public class ComunicacaoTemplateDetalheDto : ComunicacaoTemplateResumoDto
{
    public string? Assunto { get; set; }
    public string Corpo { get; set; } = string.Empty;
    public string? CorpoHtml { get; set; }
    public string VariaveisPermitidas { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class ComunicacaoCampanhaResumoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string PublicoAlvo { get; set; } = string.Empty;
    public StatusComunicacaoCampanha Status { get; set; }
    public DateTime? DataAgendamento { get; set; }
    public DateTime DataCriacao { get; set; }
    public int TotalEntregas { get; set; }
    public int TotalFalhas { get; set; }
}

public class ComunicacaoCampanhaDetalheDto : ComunicacaoCampanhaResumoDto
{
    public TipoOrigemComunicacao Origem { get; set; }
    public int? CriadoPorUsuarioId { get; set; }
    public IReadOnlyList<ComunicacaoCampanhaCanalDto> Canais { get; set; } = [];
    public IReadOnlyList<ComunicacaoEntregaResumoDto> UltimasEntregas { get; set; } = [];
}

public class ComunicacaoCampanhaCanalDto
{
    public CanalComunicacao Canal { get; set; }
    public int? TemplateId { get; set; }
    public string? NomeTemplate { get; set; }
    public int Prioridade { get; set; }

    /// <summary>Nome do provedor ativo do canal (ex.: "WhatsApp"). Preenchido no detalhe da campanha.</summary>
    public string? NomeProvedor { get; set; }

    /// <summary>Prontidão do canal segundo o IComunicacaoCanalProvider (conta ativa/config válida).</summary>
    public bool Configurado { get; set; } = true;

    /// <summary>Mensagem de diagnóstico do canal quando não configurado.</summary>
    public string? Diagnostico { get; set; }
}

public class ComunicacaoEntregaResumoDto
{
    public int Id { get; set; }
    public CanalComunicacao Canal { get; set; }
    public string DestinoResolvido { get; set; } = string.Empty;
    public StatusComunicacaoEntrega Status { get; set; }
    public int Tentativas { get; set; }
    public DateTime? ProcessadoEm { get; set; }
    public DateTime? EntregueEm { get; set; }
    public string? Erro { get; set; }
    public string? MidiaUrl { get; set; }
    public bool PodeReprocessar { get; set; }
}

public class ComunicacaoAutomacaoExecucaoResumoDto
{
    public string Gatilho { get; set; } = string.Empty;
    public int TotalCriadas { get; set; }
    public int TotalProcessadas { get; set; }
    public int TotalFalhas { get; set; }
    public int TotalIgnoradas { get; set; }
}

public class ComunicacaoLembreteOperacionalRequest
{
    public string ChaveEvento { get; set; } = string.Empty;
    public int ContatoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
}

public class ComunicacaoAvisoContextualKidsRequest
{
    public string ChaveEvento { get; set; } = string.Empty;
    public int? CriancaContatoId { get; set; }
    public IReadOnlyList<int> ResponsavelContatoIds { get; set; } = [];
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}

public class ComunicacaoAutomacaoHistoricoQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Gatilho { get; init; }
    public string? ChaveEvento { get; init; }
}

public class ComunicacaoAutomacaoHistoricoItemDto
{
    public int Id { get; set; }
    public string Gatilho { get; set; } = string.Empty;
    public string ChaveEvento { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public DateTime ExecutadoEm { get; set; }
    public string? PayloadJson { get; set; }
}

public class ComunicacaoEntregaPagedQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? CampanhaId { get; init; }
    public CanalComunicacao? Canal { get; init; }
    public StatusComunicacaoEntrega? Status { get; init; }
    public string? Texto { get; init; }
}

public class ComunicacaoCampanhaPagedQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public StatusComunicacaoCampanha? Status { get; init; }
    public string? Texto { get; init; }
    public string? PublicoAlvo { get; init; }
}

public class ComunicacaoStatsDto
{
    public int TotalCampanhas { get; set; }
    public int CampanhasRascunho { get; set; }
    public int CampanhasAgendadas { get; set; }
    public int EntregasPendentes { get; set; }
    public int EntregasEnviadas { get; set; }
    public int EntregasComFalha { get; set; }
}

public class ComunicacaoSegmentoResumoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string PublicoAlvo { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public bool Padrao { get; set; }
}

public class ComunicacaoSegmentoDetalheDto : ComunicacaoSegmentoResumoDto
{
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class CriarComunicacaoSegmentoDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string PublicoAlvo { get; set; } = string.Empty;
}

public class AtualizarComunicacaoSegmentoDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string PublicoAlvo { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}

public class ComunicacaoEstimativaAudienciaDto
{
    public string PublicoAlvo { get; set; } = string.Empty;
    public int TotalDestinatarios { get; set; }
    public int ComWhatsApp { get; set; }
    public int ComEmail { get; set; }
    public int ComPush { get; set; }
    public int ComNotificacaoInterna { get; set; }
}

public class ComunicacaoPreferenciaResumoDto
{
    public int Id { get; set; }
    public int ContatoId { get; set; }
    public CanalComunicacao Canal { get; set; }
    public StatusPreferenciaCanal Status { get; set; }
    public string? OrigemConsentimento { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class AtualizarComunicacaoPreferenciaDto
{
    public StatusPreferenciaCanal Status { get; set; } = StatusPreferenciaCanal.Permitido;
    public string? OrigemConsentimento { get; set; }
}

public class CriarComunicacaoCampanhaDto
{
    public string Nome { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string PublicoAlvo { get; set; } = string.Empty;
    public DateTime? DataAgendamento { get; set; }
    public List<CriarComunicacaoCampanhaCanalDto> Canais { get; set; } = new();
}

public class AtualizarComunicacaoCampanhaDto
{
    public string Nome { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string PublicoAlvo { get; set; } = string.Empty;
    public DateTime? DataAgendamento { get; set; }
    public StatusComunicacaoCampanha Status { get; set; } = StatusComunicacaoCampanha.Rascunho;
    public List<CriarComunicacaoCampanhaCanalDto> Canais { get; set; } = new();
}

public class CriarComunicacaoCampanhaCanalDto
{
    public CanalComunicacao Canal { get; set; }
    public int? TemplateId { get; set; }
    public int Prioridade { get; set; }
}

public class CriarComunicacaoTemplateDto
{
    public string Nome { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public CanalComunicacao Canal { get; set; }
    public string? Assunto { get; set; }
    public string Corpo { get; set; } = string.Empty;
    public string? CorpoHtml { get; set; }
    public string VariaveisPermitidas { get; set; } = string.Empty;
}

public class AtualizarComunicacaoTemplateDto
{
    public string Nome { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string? Assunto { get; set; }
    public string Corpo { get; set; } = string.Empty;
    public string? CorpoHtml { get; set; }
    public string VariaveisPermitidas { get; set; } = string.Empty;
    public StatusComunicacaoTemplate Status { get; set; } = StatusComunicacaoTemplate.Rascunho;
}
