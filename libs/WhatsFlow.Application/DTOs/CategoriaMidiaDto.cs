namespace WhatsFlow.Application.DTOs;

public class CategoriaMidiaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarCategoriaMidiaDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}

public class AtualizarCategoriaMidiaDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}





