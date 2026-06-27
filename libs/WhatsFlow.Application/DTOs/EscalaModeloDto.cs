namespace WhatsFlow.Application.DTOs;

public class EscalaModeloDto
{
    public int Id { get; set; }
    public int? EventoId { get; set; }
    public string? EventoNome { get; set; }
    public int EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public int? DiasFolgaAposEscala { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<EscalaModeloItemDto> Itens { get; set; } = new();
}

public class EscalaModeloItemDto
{
    public int Id { get; set; }
    public int EscalaModeloId { get; set; }
    public int? CargoId { get; set; }
    public string? CargoNome { get; set; }
    public int Quantidade { get; set; }
    public int Ordem { get; set; }
}

public class CriarEscalaModeloDto
{
    public int? EventoId { get; set; }
    public int EquipeId { get; set; }
    public string? Nome { get; set; }
    public int? DiasFolgaAposEscala { get; set; }
    public bool Ativo { get; set; } = true;
    public List<CriarEscalaModeloItemDto> Itens { get; set; } = new();
}

public class CriarEscalaModeloItemDto
{
    public int? CargoId { get; set; }
    public int Quantidade { get; set; } = 1;
    public int Ordem { get; set; }
}

public class AtualizarEscalaModeloDto
{
    public string? Nome { get; set; }
    public int? DiasFolgaAposEscala { get; set; }
    public bool Ativo { get; set; }
    public List<CriarEscalaModeloItemDto>? Itens { get; set; }
}
