namespace WhatsFlow.Application.DTOs;

public class CategoriaNoticiaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}

public class CriarCategoriaNoticiaDto
{
    public string Nome { get; set; } = string.Empty;
}

public class AtualizarCategoriaNoticiaDto
{
    public string Nome { get; set; } = string.Empty;
}



