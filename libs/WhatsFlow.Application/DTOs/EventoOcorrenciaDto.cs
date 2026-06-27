using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class EventoOcorrenciaDto
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public string EventoTitulo { get; set; } = string.Empty;
    public int? EventoRecorrenciaId { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public StatusEventoOcorrencia Status { get; set; }
    public bool GeradaAutomaticamente { get; set; }
    public DateTime DataCriacao { get; set; }
    public bool PossuiEscala { get; set; }
    public int? EscalaId { get; set; }
}

public class CoberturaVoluntariadoOcorrenciaDto
{
    public int OcorrenciaId { get; set; }
    public int EventoId { get; set; }
    public string EventoTitulo { get; set; } = string.Empty;
    public DateTime DataHoraInicio { get; set; }
    public StatusEventoOcorrencia StatusOcorrencia { get; set; }
    public int TotalEscalas { get; set; }
    public int TotalVagas { get; set; }
    public int Confirmados { get; set; }
    public int Pendentes { get; set; }
    public int Recusados { get; set; }
    public int Substituidos { get; set; }
    public int Faltas { get; set; }
    public int Rascunhos { get; set; }
    public string NivelRisco { get; set; } = "ok";
    public string RotuloRisco { get; set; } = "Coberta";
    public int OrdemRisco { get; set; }
    public List<CoberturaVoluntariadoEquipeDto> Equipes { get; set; } = new();
}

public class CoberturaVoluntariadoEquipeDto
{
    public int EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public int TotalVagas { get; set; }
    public int Confirmados { get; set; }
    public int Pendentes { get; set; }
    public int Recusados { get; set; }
    public int Substituidos { get; set; }
    public int Faltas { get; set; }
    public StatusEscala StatusEscala { get; set; }
}

public class CriarEventoOcorrenciaDto
{
    public int EventoId { get; set; }
    public int? EventoRecorrenciaId { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public StatusEventoOcorrencia Status { get; set; } = StatusEventoOcorrencia.Confirmado;
    public bool GeradaAutomaticamente { get; set; } = false;
}

public class AtualizarEventoOcorrenciaDto
{
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public StatusEventoOcorrencia Status { get; set; } = StatusEventoOcorrencia.Confirmado;
}
