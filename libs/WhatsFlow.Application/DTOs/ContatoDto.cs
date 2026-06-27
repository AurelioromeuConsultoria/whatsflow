using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class ContatoTagDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Cor { get; set; }
}

public class ContatoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string TelefoneWhatsApp { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Documento { get; set; }
    public string? Organizacao { get; set; }
    public string? Observacoes { get; set; }
    public string? Origem { get; set; }
    public ContatoStatus Status { get; set; }
    public bool OptIn { get; set; }
    public DateTime? DataOptIn { get; set; }
    public DateTime? DataOptOut { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public List<ContatoTagDto> Tags { get; set; } = new();
}

public class CriarContatoDto
{
    public string Nome { get; set; } = string.Empty;
    public string TelefoneWhatsApp { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Documento { get; set; }
    public string? Organizacao { get; set; }
    public string? Observacoes { get; set; }
    public string? Origem { get; set; }
    public ContatoStatus Status { get; set; } = ContatoStatus.Ativo;
    public bool OptIn { get; set; }
    public List<int> TagIds { get; set; } = new();
}

public class AtualizarContatoDto
{
    public string Nome { get; set; } = string.Empty;
    public string TelefoneWhatsApp { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Documento { get; set; }
    public string? Organizacao { get; set; }
    public string? Observacoes { get; set; }
    public string? Origem { get; set; }
    public ContatoStatus Status { get; set; } = ContatoStatus.Ativo;
    public bool OptIn { get; set; }
    public List<int> TagIds { get; set; } = new();
}

public class ContatoPagedQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Texto { get; init; }
    public ContatoStatus? Status { get; init; }
    public bool? OptIn { get; init; }
    public int? TagId { get; init; }
    public string? Sort { get; init; }
    public string? Direction { get; init; }
}
