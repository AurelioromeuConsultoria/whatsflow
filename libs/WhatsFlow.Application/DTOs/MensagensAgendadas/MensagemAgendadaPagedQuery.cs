using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs.MensagensAgendadas;

public class MensagemAgendadaPagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Sort { get; init; }
    public string? Direction { get; init; }

    public string? Texto { get; init; }
    public int? VisitanteId { get; init; }
    public StatusMensagem? Status { get; init; }
    public DateTime? DataEnvioFrom { get; init; }
    public DateTime? DataEnvioTo { get; init; }
}

