namespace WhatsFlow.Application.DTOs;

public class DashboardDto
{
    public int TotalVisitantes { get; set; }
    public int MensagensAgendadas { get; set; }
    public int MensagensEnviadas { get; set; }
    public int ConfiguracoesAtivas { get; set; }
    public int TotalPessoas { get; set; }
    public int TotalEventos { get; set; }
    public int TotalInscricoes { get; set; }
    public int TotalVoluntarios { get; set; }
    public int TotalAniversariantesProximos { get; set; }
    public List<AniversarianteDto> ProximosAniversariantes { get; set; } = new();
}

/// <summary>
/// Ponto da série temporal mensal do dashboard. Contagens cumulativas (total até o
/// fim do mês) para as entidades; mensagens enviadas é por mês.
/// </summary>
public class DashboardSeriePontoDto
{
    public string Mes { get; set; } = string.Empty;
    public int Pessoas { get; set; }
    public int Visitantes { get; set; }
    public int Voluntarios { get; set; }
    public int Eventos { get; set; }
    public int Inscricoes { get; set; }
    public int MensagensEnviadas { get; set; }
}
