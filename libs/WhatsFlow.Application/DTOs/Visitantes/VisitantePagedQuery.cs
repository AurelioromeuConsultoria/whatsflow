namespace WhatsFlow.Application.DTOs.Visitantes;

public class VisitantePagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Sort { get; init; }
    public string? Direction { get; init; }

    public string? Nome { get; init; }
    public string? Email { get; init; }
    public string? Telefone { get; init; }
    public string? WhatsApp { get; init; }

    public DateTime? DataVisitaFrom { get; init; }
    public DateTime? DataVisitaTo { get; init; }
}

