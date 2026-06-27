using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class SolicitacaoTrocaEscalaDto
{
    public int Id { get; set; }
    public int EscalaItemId { get; set; }
    public int EscalaId { get; set; }
    public int EventoOcorrenciaId { get; set; }
    public string EventoTitulo { get; set; } = string.Empty;
    public DateTime? EventoDataHoraInicio { get; set; }
    public int EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public int VoluntarioSolicitanteId { get; set; }
    public string VoluntarioSolicitanteNome { get; set; } = string.Empty;
    public int? VoluntarioSubstitutoId { get; set; }
    public string? VoluntarioSubstitutoNome { get; set; }
    public StatusSolicitacaoTrocaEscala Status { get; set; }
    public string? Motivo { get; set; }
    public string? ObservacaoResposta { get; set; }
    public int? RespondidoPorUsuarioId { get; set; }
    public string? RespondidoPorUsuarioNome { get; set; }
    public DateTime DataSolicitacao { get; set; }
    public DateTime? DataResposta { get; set; }
}

public class CriarSolicitacaoTrocaEscalaDto
{
    public string? Motivo { get; set; }
}

public class AprovarSolicitacaoTrocaEscalaDto
{
    public int VoluntarioSubstitutoId { get; set; }
    public string? ObservacaoResposta { get; set; }
}

public class RejeitarSolicitacaoTrocaEscalaDto
{
    public string? ObservacaoResposta { get; set; }
}
