using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoAudienceResolver
{
    Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveAsync(string publicoAlvo);
}

public class ComunicacaoAudienceResolver : IComunicacaoAudienceResolver
{
    private readonly IContatoRepository _contatoRepository;

    public ComunicacaoAudienceResolver(IContatoRepository contatoRepository)
    {
        _contatoRepository = contatoRepository;
    }

    public async Task<IReadOnlyList<ComunicacaoDestinatario>> ResolveAsync(string publicoAlvo)
    {
        var publico = (publicoAlvo ?? string.Empty).Trim().ToLowerInvariant();

        // Implementação simples: Contatos ativos. Para públicos de campanha ativa
        // ("campanha", "ativos", "marketing"...) também exige consentimento (OptIn).
        // TODO(WhatsFlow Etapa 4C): aplicar FiltrosJson do segmento (tags/origem/datas)
        var exigirOptIn = publico is not ("todos" or "todos-contatos" or "" );

        var contatos = await _contatoRepository.GetAllAsync();
        return contatos
            .Where(c => c.Status == ContatoStatus.Ativo && (!exigirOptIn || c.OptIn))
            .Select(MapContato)
            .ToList();
    }

    private static ComunicacaoDestinatario MapContato(Contato contato)
    {
        return new ComunicacaoDestinatario
        {
            ContatoId = contato.Id,
            Nome = contato.Nome,
            PrimeiroNome = GetPrimeiroNome(contato.Nome),
            Email = contato.Email,
            WhatsApp = contato.TelefoneWhatsApp
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
    public int? ContatoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string PrimeiroNome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? WhatsApp { get; set; }
}
