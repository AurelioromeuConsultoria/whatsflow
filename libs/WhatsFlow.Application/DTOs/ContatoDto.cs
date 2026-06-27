namespace WhatsFlow.Application.DTOs;

public class ContatoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Membro { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}

public class CriarContatoDto
{
    public string Nome { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Membro { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public class AtualizarContatoDto
{
    public string Nome { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Membro { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}






