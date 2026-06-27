using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Application.DTOs;

/// <summary>
/// DTO para cadastro público de membros (formulário web sem autenticação)
/// </summary>
public class CadastroMembroDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "WhatsApp é obrigatório")]
    [MaxLength(20)]
    public string WhatsApp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data de nascimento é obrigatória")]
    public DateTime? DataNascimento { get; set; }

    /// <summary>
    /// Versão dos Termos de Uso / Política de Privacidade aceitos pelo titular (ex.: "v1").
    /// Obrigatório para LGPD — sem aceite não há cadastro.
    /// </summary>
    [Required(ErrorMessage = "É necessário aceitar os Termos de Uso e a Política de Privacidade")]
    [MaxLength(20)]
    public string AceiteTermosVersao { get; set; } = string.Empty;
}
