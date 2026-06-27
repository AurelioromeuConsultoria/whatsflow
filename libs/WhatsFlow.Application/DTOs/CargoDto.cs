namespace WhatsFlow.Application.DTOs;

public class CargoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}

public class CriarCargoDto
{
    public string Nome { get; set; } = string.Empty;
}

public class AtualizarCargoDto
{
    public string Nome { get; set; } = string.Empty;
}
