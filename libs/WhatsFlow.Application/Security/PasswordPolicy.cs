using System.Linq;

namespace WhatsFlow.Application.Security;

/// <summary>
/// Política mínima de senha, aplicada em todos os pontos de entrada que recebem
/// uma senha escolhida pelo usuário (signup, criação de usuário, troca de senha).
/// Baseline de segurança: 8+ caracteres com maiúscula, minúscula e número.
/// </summary>
public static class PasswordPolicy
{
    public const int ComprimentoMinimo = 8;

    /// <summary>
    /// Lança <see cref="ArgumentException"/> (traduzida pelos controllers para
    /// HTTP 400 com { message }) se a senha não atende à política.
    /// </summary>
    public static void Validar(string? senha)
    {
        var erro = Avaliar(senha);
        if (erro is not null)
        {
            throw new ArgumentException(erro);
        }
    }

    /// <summary>Retorna a mensagem de erro, ou null se a senha é válida.</summary>
    public static string? Avaliar(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < ComprimentoMinimo)
        {
            return $"A senha deve ter ao menos {ComprimentoMinimo} caracteres.";
        }

        if (!senha.Any(char.IsUpper) || !senha.Any(char.IsLower) || !senha.Any(char.IsDigit))
        {
            return "A senha deve conter letra maiúscula, letra minúscula e número.";
        }

        return null;
    }
}
