namespace WhatsFlow.Application.DTOs;

public class NoticiaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Texto { get; set; }
    public DateTime Data { get; set; }
    public string? Url { get; set; }
    public string? Imagem { get; set; }
    public int CategoriaNoticiaId { get; set; }
    public string? CategoriaNoticiaNome { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarNoticiaDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Texto { get; set; }
    public DateTime Data { get; set; }
    public string? Url { get; set; }
    public string? Imagem { get; set; }
    public int CategoriaNoticiaId { get; set; }
}

public class AtualizarNoticiaDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Texto { get; set; }
    public DateTime Data { get; set; }
    public string? Url { get; set; }
    public string? Imagem { get; set; }
    public int CategoriaNoticiaId { get; set; }
}



