namespace WhatsFlow.Application.DTOs;

public class EquipeDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Area { get; set; }
    public int? LiderUsuarioId { get; set; }
    public string? LiderNome { get; set; }
    public int QuantidadeMembros { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarEquipeDto
{
    public string Nome { get; set; } = string.Empty;
    public int Area { get; set; }
    public int? LiderUsuarioId { get; set; }
}

public class AtualizarEquipeDto
{
    public string Nome { get; set; } = string.Empty;
    public int Area { get; set; }
    public int? LiderUsuarioId { get; set; }
}
