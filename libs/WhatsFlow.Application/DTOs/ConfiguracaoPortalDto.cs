namespace WhatsFlow.Application.DTOs;

public class ConfiguracaoPortalDto
{
    public int Id { get; set; }
    public int TempoTransicaoCarrossel { get; set; } = 5;
    public DateTime DataAtualizacao { get; set; }
}

public class AtualizarConfiguracaoPortalDto
{
    public int TempoTransicaoCarrossel { get; set; } = 5;
}
