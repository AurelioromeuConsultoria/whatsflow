using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class NotificacaoUsuarioDto
{
    public int Id { get; set; }
    public TipoNotificacaoUsuario Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? Link { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataLeitura { get; set; }
    public bool Lida => DataLeitura.HasValue;
}

public class CriarNotificacaoUsuarioDto
{
    public int UsuarioId { get; set; }
    public TipoNotificacaoUsuario Tipo { get; set; } = TipoNotificacaoUsuario.Geral;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? Link { get; set; }
}
