using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class SignupDto
{
    [Required(ErrorMessage = "Nome da igreja é obrigatório")]
    [MaxLength(150)]
    public string NomeIgreja { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome do responsável é obrigatório")]
    [MaxLength(150)]
    public string AdminNome { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(8, ErrorMessage = "A senha deve ter ao menos 8 caracteres")]
    public string Senha { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefone { get; set; }

    /// <summary>Plano escolhido (slug). Se vazio, usa o plano padrão.</summary>
    [MaxLength(60)]
    public string? PlanoSlug { get; set; }

    public CicloCobranca Ciclo { get; set; } = CicloCobranca.Mensal;

    [Required(ErrorMessage = "É necessário aceitar os Termos de Uso e a Política de Privacidade")]
    [MaxLength(20)]
    public string AceiteTermosVersao { get; set; } = string.Empty;
}

public class SignupResultDto
{
    public string Status { get; set; } = "pendente_verificacao";
    public string Email { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Preenchido só quando o envio de e-mail está desabilitado (ambiente de testes).</summary>
    public string? LinkConfirmacao { get; set; }
}

public class ConfirmacaoEmailResultDto
{
    public bool Confirmado { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}
