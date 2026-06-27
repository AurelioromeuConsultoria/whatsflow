namespace WhatsFlow.Application.DTOs;

/// <summary>
/// Um campo do formulário de inscrição configurado no evento.
/// </summary>
public class EventoCampoFormularioDto
{
    public string Slug { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Tipo { get; set; } = "texto";
    public bool Obrigatorio { get; set; }
}
