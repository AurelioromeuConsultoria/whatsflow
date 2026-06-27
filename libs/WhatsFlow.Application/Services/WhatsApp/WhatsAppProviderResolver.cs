using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services.WhatsApp;

/// <summary>
/// Resolve a implementação de IWhatsAppProvider pelo tipo declarado na WhatsAppAccount.
/// Cai para o provider Fake quando o tipo não tem implementação registrada (dev-friendly).
/// </summary>
public sealed class WhatsAppProviderResolver : IWhatsAppProviderResolver
{
    private readonly IReadOnlyDictionary<WhatsAppProviderType, IWhatsAppProvider> _byType;
    private readonly IWhatsAppProvider _fallback;

    public WhatsAppProviderResolver(IEnumerable<IWhatsAppProvider> providers)
    {
        var list = providers.ToList();
        // Último registrado por tipo vence (permite sobrescrever em testes).
        _byType = list
            .GroupBy(p => p.Type)
            .ToDictionary(g => g.Key, g => g.Last());

        _fallback = _byType.TryGetValue(WhatsAppProviderType.Fake, out var fake)
            ? fake
            : list.FirstOrDefault()
              ?? throw new InvalidOperationException("Nenhum IWhatsAppProvider registrado.");
    }

    public IWhatsAppProvider Resolve(WhatsAppProviderType type)
        => _byType.TryGetValue(type, out var provider) ? provider : _fallback;

    public IWhatsAppProvider ResolveFor(WhatsAppAccount account)
        => Resolve(account.Provider);
}
