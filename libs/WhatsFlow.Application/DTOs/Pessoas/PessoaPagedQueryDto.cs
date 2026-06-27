namespace WhatsFlow.Application.DTOs.Pessoas;

public class PessoaPagedQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Sort { get; init; }
    public string? Direction { get; init; }

    public string? Nome { get; init; }
    public string? Email { get; init; }
    public string? Telefone { get; init; }
    public string? WhatsApp { get; init; }

    public string? Perfil { get; init; }
    public string? TipoPessoa { get; init; }
    public bool? Ativo { get; init; }
}

