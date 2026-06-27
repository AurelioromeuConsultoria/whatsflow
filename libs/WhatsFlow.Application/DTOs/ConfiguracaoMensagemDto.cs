namespace WhatsFlow.Application.DTOs;

public class ConfiguracaoMensagemDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string TextoMensagem { get; set; } = string.Empty;
    public int DiasAposVisita { get; set; }
    public TimeSpan HorarioEnvio { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarConfiguracaoMensagemDto
{
    public string Nome { get; set; } = string.Empty;
    public string TextoMensagem { get; set; } = string.Empty;
    public int DiasAposVisita { get; set; }
    public TimeSpan HorarioEnvio { get; set; }
    public bool Ativo { get; set; } = true;
}

public class AtualizarConfiguracaoMensagemDto
{
    public string Nome { get; set; } = string.Empty;
    public string TextoMensagem { get; set; } = string.Empty;
    public int DiasAposVisita { get; set; }
    public TimeSpan HorarioEnvio { get; set; }
    public bool Ativo { get; set; }
}

