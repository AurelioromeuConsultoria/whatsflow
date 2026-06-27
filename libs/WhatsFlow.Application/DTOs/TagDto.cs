namespace WhatsFlow.Application.DTOs;

public class TagDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
    public DateTime CriadoEm { get; set; }
    public int TotalContatos { get; set; }
}

public class CriarTagDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
}

public class AtualizarTagDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
}
