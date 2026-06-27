using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class EscalaDto
{
    public int Id { get; set; }
    public int EventoOcorrenciaId { get; set; }
    public int EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public DateTime EventoDataHoraInicio { get; set; }
    public string EventoTitulo { get; set; } = string.Empty;
    public StatusEscala Status { get; set; }
    public string? Observacoes { get; set; }
    public int? CriadoPorUsuarioId { get; set; }
    public string? CriadoPorUsuarioNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataPublicacao { get; set; }
    public List<EscalaItemDto> Itens { get; set; } = new();
}

public class EscalaItemDto
{
    public int Id { get; set; }
    public int EscalaId { get; set; }
    public int EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public int? CargoId { get; set; }
    public string? CargoNome { get; set; }
    public int? VoluntarioId { get; set; }
    public int VoluntarioPessoaId { get; set; }
    public string VoluntarioNome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool ConflitoAprovado { get; set; }
    public string? MotivoExcecao { get; set; }
    public int? AprovadoPorUsuarioId { get; set; }
    public string? AprovadoPorUsuarioNome { get; set; }
    public DateTime? AprovadoEm { get; set; }
    public StatusEscalaItem Status { get; set; }
    public DateTime? DataConvite { get; set; }
    public DateTime? DataConfirmacao { get; set; }
    public DateTime? DataRecusa { get; set; }
    public DateTime? DataLembrete7DiasEnviado { get; set; }
    public DateTime? DataLembrete24HorasEnviado { get; set; }
    public string? MotivoRecusa { get; set; }
    public int? RespondidoPorUsuarioId { get; set; }
    public string? RespondidoPorUsuarioNome { get; set; }
    public string? ObservacaoOperacional { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarEscalaDto
{
    public int EventoOcorrenciaId { get; set; }
    public int EquipeId { get; set; }
    public string? Observacoes { get; set; }
}

public class AtualizarEscalaDto
{
    public StatusEscala Status { get; set; }
    public string? Observacoes { get; set; }
}

public class CriarEscalaItemDto
{
    public int EquipeId { get; set; }
    public int? CargoId { get; set; }
    public int VoluntarioId { get; set; }
    public int Ordem { get; set; } = 0;
    public bool ForcarConflito { get; set; } = false;
    public string? MotivoExcecao { get; set; }
}

public class AtualizarEscalaItemDto
{
    public int EquipeId { get; set; }
    public int? CargoId { get; set; }
    public int VoluntarioId { get; set; }
    public int Ordem { get; set; } = 0;
    public bool ForcarConflito { get; set; } = false;
    public string? MotivoExcecao { get; set; }
}

public class RecusarEscalaItemDto
{
    public string? MotivoRecusa { get; set; }
}

public class RegistrarPresencaEscalaItemDto
{
    public bool Compareceu { get; set; }
    public string? ObservacaoOperacional { get; set; }
}

public class HistoricoVoluntarioDto
{
    public int PessoaId { get; set; }
    public string VoluntarioNome { get; set; } = string.Empty;
    public List<string> Equipes { get; set; } = new();
    public int TotalEscalas { get; set; }
    public int Confirmados { get; set; }
    public int Recusados { get; set; }
    public int Substituidos { get; set; }
    public int Presencas { get; set; }
    public int Faltas { get; set; }
    public int Pendentes { get; set; }
    public int CargaMesAtual { get; set; }
    public DateTime? UltimaEscalaEm { get; set; }
    public DateTime? ProximaEscalaEm { get; set; }
}

public class SugestaoEscalaVoluntarioDto
{
    public int VoluntarioId { get; set; }
    public int PessoaId { get; set; }
    public string VoluntarioNome { get; set; } = string.Empty;
    public int EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public int CargoId { get; set; }
    public string CargoNome { get; set; } = string.Empty;
    public bool Disponivel { get; set; }
    public int CargaRecente { get; set; }
    public string? MotivoBloqueio { get; set; }
}

public class PlanejamentoMensalEscalaDto
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public int? EventoId { get; set; }
    public int? EquipeId { get; set; }
    public List<PlanejamentoMensalOcorrenciaDto> Ocorrencias { get; set; } = new();
    public List<PlanejamentoMensalVoluntarioDto> Voluntarios { get; set; } = new();
    public PlanejamentoMensalResumoDto Resumo { get; set; } = new();
}

public class PlanejamentoMensalOcorrenciaDto
{
    public int OcorrenciaId { get; set; }
    public int EventoId { get; set; }
    public string EventoTitulo { get; set; } = string.Empty;
    public DateTime DataHoraInicio { get; set; }
    public int TotalEscalados { get; set; }
}

public class PlanejamentoMensalVoluntarioDto
{
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public List<string> Equipes { get; set; } = new();
    public List<string> Cargos { get; set; } = new();
    public int TotalEscalas { get; set; }
    public int Confirmados { get; set; }
    public int Pendentes { get; set; }
    public int Recusados { get; set; }
    public int Faltas { get; set; }
    public bool TemDomingosConsecutivos { get; set; }
    public List<PlanejamentoMensalAlocacaoDto> Alocacoes { get; set; } = new();
}

public class PlanejamentoMensalAlocacaoDto
{
    public int EscalaId { get; set; }
    public int EscalaItemId { get; set; }
    public int OcorrenciaId { get; set; }
    public int EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public int? CargoId { get; set; }
    public string? CargoNome { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public StatusEscalaItem Status { get; set; }
}

public class PlanejamentoMensalResumoDto
{
    public int TotalVoluntarios { get; set; }
    public int TotalEscalas { get; set; }
    public int VoluntariosSemEscala { get; set; }
    public int VoluntariosComMaisDeDuasEscalas { get; set; }
    public int VoluntariosComDomingosConsecutivos { get; set; }
}

public class GerarPlanejamentoMensalDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public int EquipeId { get; set; }
    public int? EventoId { get; set; }
}

public class GerarPlanejamentoMensalResultadoDto
{
    public int OcorrenciasProcessadas { get; set; }
    public int EscalasGeradas { get; set; }
    public List<string> Avisos { get; set; } = new();
}

public class CriarAlocacaoPlanejamentoMensalDto
{
    public int EventoOcorrenciaId { get; set; }
    public int EquipeId { get; set; }
    public int VoluntarioId { get; set; }
    public int? CargoId { get; set; }
    public bool ForcarConflito { get; set; } = false;
    public string? MotivoExcecao { get; set; }
}

public class DispararPlanejamentoMensalWhatsAppDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public int EquipeId { get; set; }
    public int? EventoId { get; set; }
    public string ImagemUrl { get; set; } = string.Empty;
    public string? Mensagem { get; set; }
    public string? WhatsAppTeste { get; set; }
}

public class DispararPlanejamentoMensalWhatsAppResultadoDto
{
    public int TotalDestinatarios { get; set; }
    public int TotalEnviados { get; set; }
    public int TotalFalhas { get; set; }
    public List<string> Falhas { get; set; } = new();
}
