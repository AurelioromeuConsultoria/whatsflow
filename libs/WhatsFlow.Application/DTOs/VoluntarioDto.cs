namespace WhatsFlow.Application.DTOs;

public class VoluntarioDto
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public int EquipeId { get; set; }
    public string NomeEquipe { get; set; } = string.Empty;
    public int CargoId { get; set; }
    public string NomeCargo { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; }
}

public class CriarVoluntarioDto
{
    public int PessoaId { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public DateTime? DataNascimento { get; set; }
    public int EquipeId { get; set; }
    public int CargoId { get; set; }
}

public class AtualizarVoluntarioDto
{
    public int PessoaId { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public DateTime? DataNascimento { get; set; }
    public int EquipeId { get; set; }
    public int CargoId { get; set; }
}
