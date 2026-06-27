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
    private readonly IVoluntarioRepository _voluntarioRepository;
    private readonly IResponsavelCriancaRepository _responsavelCriancaRepository;

    public ComunicacaoAudienceResolver(
        IVisitanteRepository visitanteRepository,
        IPessoaRepository pessoaRepository,
        IVoluntarioRepository voluntarioRepository,
        IResponsavelCriancaRepository responsavelCriancaRepository)
    {
        _visitanteRepository = visitanteRepository;
        _pessoaRepository = pessoaRepository;
        _voluntarioRepository = voluntarioRepository;
        _responsavelCriancaRepository = responsavelCriancaRepository;
    }

    public async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveAsync(string publicoAlvo)
    {
        var publico = (publicoAlvo ?? string.Empty).Trim().ToLowerInvariant();
        return publico switch
        {
            "visitantes" => await ResolveVisitantesAsync(),
            "membros" => await ResolvePessoasPorPerfilAsync(PerfilPessoa.Membro),
            "voluntarios" => await ResolveVoluntariosAsync(),
            "responsaveis-kids" => await ResolveResponsaveisKidsAsync(),
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

    private async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolvePessoasPorPerfilAsync(PerfilPessoa perfil)
    {
        var pessoas = await _pessoaRepository.GetAllAsync();
        return pessoas
            .Where(p => p.Ativo && p.Perfis.Any(pp => pp.DataFim == null && pp.Perfil == perfil))
            .Select(MapPessoa)
            .ToList();
    }

    private async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveVoluntariosAsync()
    {
        var voluntarios = await _voluntarioRepository.GetAllAsync();
        return voluntarios
            .Where(v => v.Pessoa != null && v.Pessoa.Ativo)
            .Select(v => MapPessoa(v.Pessoa))
            .GroupBy(d => d.PessoaId)
            .Select(g => g.First())
            .ToList();
    }

    private async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveResponsaveisKidsAsync()
    {
        var responsavelIds = (await _responsavelCriancaRepository.GetResponsavelIdsAtivosAsync()).ToHashSet();
        if (responsavelIds.Count == 0)
        {
            return [];
        }

        var pessoas = await _pessoaRepository.GetAllAsync();
        return pessoas
            .Where(p => p.Ativo && responsavelIds.Contains(p.Id))
            .Select(MapPessoa)
            .ToList();
    }

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
