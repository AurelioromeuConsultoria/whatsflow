using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class MensagemAgendadaDto
{
    public int Id { get; set; }
    public int ContatoId { get; set; }
    public string NomeContato { get; set; } = string.Empty;
    public string TelefoneContato { get; set; } = string.Empty;
    public int ConfiguracaoMensagemId { get; set; }
    public string NomeConfiguracao { get; set; } = string.Empty;
    public DateTime DataAgendamento { get; set; }
    public DateTime DataEnvio { get; set; }
    public StatusMensagem Status { get; set; }
    public string TextoFinal { get; set; } = string.Empty;
    public DateTime? DataProcessamento { get; set; }
    public string? LogErro { get; set; }
    public DateTime DataCriacao { get; set; }
}
