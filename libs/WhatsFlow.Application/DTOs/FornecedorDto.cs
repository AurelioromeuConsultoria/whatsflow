namespace WhatsFlow.Application.DTOs;

public class FornecedorDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public string? Site { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoCpf { get; set; }
    public string? ContatoWhatsApp { get; set; }
    public string? ContatoEmail { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarFornecedorDto
{
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public string? Site { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoCpf { get; set; }
    public string? ContatoWhatsApp { get; set; }
    public string? ContatoEmail { get; set; }
}

public class AtualizarFornecedorDto
{
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? CnpjCpf { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
    public string? Site { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoCpf { get; set; }
    public string? ContatoWhatsApp { get; set; }
    public string? ContatoEmail { get; set; }
}
