using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

/// <summary>
/// Exceção lançada quando uma operação excederia um limite do plano do tenant
/// (contatos ou mensagens mensais). Herda de InvalidOperationException para ser tratada
/// como erro de regra de negócio pelos controllers.
/// </summary>
public sealed class PlanLimitExceededException : InvalidOperationException
{
    public PlanLimitExceededException(string message) : base(message)
    {
    }
}

public interface IPlanLimitService
{
    /// <summary>True se o tenant pode criar mais um contato (limite 0 = ilimitado).</summary>
    Task<bool> PodeCriarContatoAsync();

    /// <summary>
    /// Garante que enfileirar <paramref name="novasMensagens"/> entregas não excede o limite mensal
    /// do tenant. Lança <see cref="PlanLimitExceededException"/> caso exceda.
    /// </summary>
    Task EnsureCanEnqueueAsync(int novasMensagens);
}

public class PlanLimitService : IPlanLimitService
{
    private readonly ITenantLookupRepository _tenantRepository;
    private readonly IContatoRepository _contatoRepository;
    private readonly IComunicacaoEntregaRepository _entregaRepository;
    private readonly ITenantContext _tenantContext;

    public PlanLimitService(
        ITenantLookupRepository tenantRepository,
        IContatoRepository contatoRepository,
        IComunicacaoEntregaRepository entregaRepository)
        : this(tenantRepository, contatoRepository, entregaRepository, new DefaultTenantContext())
    {
    }

    public PlanLimitService(
        ITenantLookupRepository tenantRepository,
        IContatoRepository contatoRepository,
        IComunicacaoEntregaRepository entregaRepository,
        ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _contatoRepository = contatoRepository;
        _entregaRepository = entregaRepository;
        _tenantContext = tenantContext;
    }

    public async Task<bool> PodeCriarContatoAsync()
    {
        var tenant = await GetTenantAsync();
        if (tenant == null || tenant.LimiteContatos <= 0)
        {
            // Sem tenant resolvido ou limite ilimitado.
            return true;
        }

        var atuais = await _contatoRepository.CountAsync();
        return atuais < tenant.LimiteContatos;
    }

    public async Task EnsureCanEnqueueAsync(int novasMensagens)
    {
        if (novasMensagens <= 0)
        {
            return;
        }

        var tenant = await GetTenantAsync();
        if (tenant == null || tenant.LimiteMensalMensagens <= 0)
        {
            // Limite ilimitado / herdado.
            return;
        }

        var enviadasNoMes = await _entregaRepository.CountCriadasNoMesAsync(DateTime.UtcNow);
        if (enviadasNoMes + novasMensagens > tenant.LimiteMensalMensagens)
        {
            var disponivel = Math.Max(0, tenant.LimiteMensalMensagens - enviadasNoMes);
            throw new PlanLimitExceededException(
                $"Limite mensal de mensagens do plano atingido. Limite: {tenant.LimiteMensalMensagens}, " +
                $"já usadas neste mês: {enviadasNoMes}, disponíveis: {disponivel}, solicitadas: {novasMensagens}.");
        }
    }

    private async Task<Tenant?> GetTenantAsync()
    {
        var tenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        return await _tenantRepository.GetByIdAsync(tenantId);
    }
}
