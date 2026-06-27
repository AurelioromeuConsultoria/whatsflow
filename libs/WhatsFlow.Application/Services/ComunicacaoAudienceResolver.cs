using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoAudienceResolver
{
    Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveAsync(string publicoAlvo);
}

public class ComunicacaoAudienceResolver : IComunicacaoAudienceResolver
{
    private readonly IVisitanteRepository _visitanteRepository;
    private readonly IPessoaRepository _pessoaRepository;

    public ComunicacaoAudienceResolver(
        IVisitanteRepository visitanteRepository,
        IPessoaRepository pessoaRepository)
    {
        _visitanteRepository = visitanteRepository;
        _pessoaRepository = pessoaRepository;
    }

    public async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveAsync(string publicoAlvo)
    {
        var publico = (publicoAlvo ?? string.Empty).Trim().ToLowerInvariant();
        return publico switch
        {
            "visitantes" => await ResolveVisitantesAsync(),
            // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
            "pessoas" => await ResolvePessoasAtivasAsync(),
            _ => []
        };
    }

    private async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveVisitantesAsync()
    {
        var visitantes = await _visitanteRepository.GetAllAsync();
        return visitantes
            .Where(v => v.Pessoa != null && v.Pessoa.Ativo)
            .Select(v => new ComunicacaoDestinatario
            {
                PessoaId = v.PessoaId,
                VisitanteId = v.Id,
                Nome = v.Pessoa?.Nome ?? string.Empty,
                PrimeiroNome = GetPrimeiroNome(v.Pessoa?.Nome),
                Email = v.Pessoa?.Email,
                WhatsApp = v.Pessoa?.WhatsApp
            })
            .GroupBy(d => new { d.PessoaId, d.VisitanteId })
            .Select(g => g.First())
            .ToList();
    }

    private async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolvePessoasAtivasAsync()
    {
        var pessoas = await _pessoaRepository.GetAllAsync();
        return pessoas
            .Where(p => p.Ativo)
            .Select(MapPessoa)
            .ToList();
    }

    // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)

    private static ComunicacaoDestinatario MapPessoa(Pessoa pessoa)
    {
        return new ComunicacaoDestinatario
        {
            PessoaId = pessoa.Id,
            Nome = pessoa.Nome,
            PrimeiroNome = GetPrimeiroNome(pessoa.Nome),
            Email = pessoa.Email,
            WhatsApp = pessoa.WhatsApp
        };
    }

    private static string GetPrimeiroNome(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return string.Empty;
        }

        return nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nome;
    }
}

public class ComunicacaoDestinatario
{
    public int? PessoaId { get; set; }
    public int? VisitanteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string PrimeiroNome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? WhatsApp { get; set; }
}
