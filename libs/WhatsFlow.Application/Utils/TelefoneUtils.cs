namespace WhatsFlow.Application.Utils;

/// <summary>
/// Utilitários para formatação e validação de telefones
/// </summary>
public static class TelefoneUtils
{
    /// <summary>
    /// Normaliza telefone removendo caracteres não numéricos
    /// </summary>
    public static string NormalizarTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return string.Empty;

        return new string(telefone.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Formata número para o formato internacional exigido pela Evolution API
    /// Exemplo: 11999999999 -> 5511999999999
    /// </summary>
    /// <param name="telefone">Número de telefone (pode ter ou não formatação)</param>
    /// <param name="codigoPaisPadrao">Código do país padrão (padrão: 55 para Brasil)</param>
    /// <returns>Número formatado no padrão internacional</returns>
    public static string FormatarParaEvolutionApi(string? telefone, string codigoPaisPadrao = "55")
    {
        if (string.IsNullOrWhiteSpace(telefone))
            throw new ArgumentException("Telefone não pode ser vazio", nameof(telefone));

        // Remove tudo exceto dígitos
        var numeroLimpo = NormalizarTelefone(telefone);

        if (string.IsNullOrEmpty(numeroLimpo))
            throw new ArgumentException("Telefone inválido: não contém dígitos", nameof(telefone));

        // Se já começa com código do país, retorna como está
        if (numeroLimpo.StartsWith(codigoPaisPadrao))
        {
            return numeroLimpo;
        }

        // Se começa com 0, remove o zero e adiciona código do país
        if (numeroLimpo.StartsWith("0"))
        {
            numeroLimpo = numeroLimpo.Substring(1);
        }

        // Se tem 11 dígitos (ex: 11999999999), adiciona código do país
        if (numeroLimpo.Length == 11)
        {
            return $"{codigoPaisPadrao}{numeroLimpo}";
        }

        // Se tem 10 dígitos (ex: 1999999999), assume DDD 11 e adiciona código do país
        if (numeroLimpo.Length == 10)
        {
            return $"{codigoPaisPadrao}11{numeroLimpo}";
        }

        // Se tem menos de 10 dígitos, assume que falta DDD e código do país
        // Adiciona DDD 11 (São Paulo) como padrão
        if (numeroLimpo.Length == 8 || numeroLimpo.Length == 9)
        {
            return $"{codigoPaisPadrao}11{numeroLimpo}";
        }

        // Se não se encaixa em nenhum padrão, tenta adicionar código do país
        return $"{codigoPaisPadrao}{numeroLimpo}";
    }

    /// <summary>
    /// Valida se o número está em formato válido
    /// </summary>
    public static bool ValidarNumero(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return false;

        var numeroLimpo = NormalizarTelefone(telefone);
        
        // Número deve ter pelo menos 10 dígitos (com código do país pode ter mais)
        return numeroLimpo.Length >= 10 && numeroLimpo.Length <= 15;
    }
}
