namespace WhatsFlow.Application.DTOs;

public class DashboardDto
{
    public int TotalContatos { get; set; }
    public int ContatosAtivos { get; set; }
    public int ContatosOptIn { get; set; }
    public int MensagensAgendadas { get; set; }
    public int MensagensEnviadas { get; set; }
    public int ConfiguracoesAtivas { get; set; }
}

/// <summary>
/// Ponto da série temporal mensal do dashboard. Contagens cumulativas (total até o
/// fim do mês) para as entidades; mensagens enviadas é por mês.
/// </summary>
public class DashboardSeriePontoDto
{
    public string Mes { get; set; } = string.Empty;
    public int Contatos { get; set; }
    public int MensagensEnviadas { get; set; }
}
