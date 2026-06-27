namespace WhatsFlow.Application.DTOs;

/// <summary>
/// Request para enviar mensagem de texto via Evolution API
/// </summary>
public class EvolutionApiSendTextRequest
{
    /// <summary>
    /// Número do destinatário no formato internacional (ex: 5511999999999)
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Texto da mensagem
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Delay em milissegundos antes de enviar (opcional)
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// Desabilitar preview de links (opcional)
    /// </summary>
    public bool? LinkPreview { get; set; }
}

public class EvolutionApiSendMediaRequest
{
    public string Number { get; set; } = string.Empty;
    public string Media { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Mimetype { get; set; }
    public string? Caption { get; set; }
    public string? Mediatype { get; set; } = "image";
    public int? Delay { get; set; }
}

public class EvolutionApiSendMediaV1Request
{
    public string Number { get; set; } = string.Empty;
    public string Mediatype { get; set; } = "image";
    public string? FileName { get; set; }
    public string? Mimetype { get; set; }
    public string? Caption { get; set; }
    public string? Media { get; set; }
    public EvolutionApiMediaMessageV1 MediaMessage { get; set; } = new();
    public EvolutionApiMediaOptionsV1 Options { get; set; } = new();
}

public class EvolutionApiMediaMessageV1
{
    public string MediaType { get; set; } = "image";
    public string FileName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Media { get; set; } = string.Empty;
}

public class EvolutionApiMediaOptionsV1
{
    public int? Delay { get; set; }
    public string Presence { get; set; } = "composing";
}

/// <summary>
/// Response da Evolution API
/// </summary>
public class EvolutionApiResponse
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? MensagemErro { get; set; }

    /// <summary>
    /// Código de status HTTP
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// ID da mensagem enviada (se disponível)
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Resposta completa da API (para debug)
    /// </summary>
    public string? RespostaCompleta { get; set; }
}

/// <summary>
/// Resposta da Evolution API quando há erro
/// </summary>
public class EvolutionApiErrorResponse
{
    public string? Error { get; set; }
    public string? Message { get; set; }
    public int? StatusCode { get; set; }
}
