using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class EventoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemDestaque { get; set; }
    public string? Url { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public int Tipo { get; set; }
    public string TipoDescricao { get; set; } = string.Empty;
    public bool EhRecorrente { get; set; }
    public bool Ativo { get; set; }
    public bool AceitaInscricoes { get; set; }
    /// <summary>JSON: array de { slug, label, tipo, obrigatorio }. Colunas fixas: nome, whatsApp, email, observacoes.</summary>
    public string? ConfiguracaoFormularioInscricao { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarEventoDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemDestaque { get; set; }
    public string? Url { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public int Tipo { get; set; } = (int)TipoEvento.Evento;
    public bool EhRecorrente { get; set; }
    public bool Ativo { get; set; } = true;
    public bool AceitaInscricoes { get; set; } = false;
    /// <summary>JSON: array de { slug, label, tipo, obrigatorio }.</summary>
    public string? ConfiguracaoFormularioInscricao { get; set; }
}

public class AtualizarEventoDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemDestaque { get; set; }
    public string? Url { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public int Tipo { get; set; }
    public bool EhRecorrente { get; set; }
    public bool Ativo { get; set; }
    public bool AceitaInscricoes { get; set; }
    public string? ConfiguracaoFormularioInscricao { get; set; }
}



