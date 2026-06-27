namespace WhatsFlow.Application.DTOs;

public class DestaqueSiteDto
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Url { get; set; }
    public string? Imagem { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarDestaqueSiteDto
{
    public string Texto { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Url { get; set; }
    public string? Imagem { get; set; }
}

public class AtualizarDestaqueSiteDto
{
    public string Texto { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Url { get; set; }
    public string? Imagem { get; set; }
}



