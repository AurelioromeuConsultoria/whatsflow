using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Acesso de leitura à entidade Tenant. Tenant não é ITenantEntity (não tem filtro de tenant),
/// então pode ser resolvido por Id/Slug mesmo antes de o contexto de tenant estar definido
/// (ex: webhook anônimo por slug).
/// </summary>
public interface ITenantLookupRepository
{
    Task<Tenant?> GetByIdAsync(int id);

    /// <summary>Resolve um tenant ativo pelo slug. Usado pelo webhook anônimo antes do escopo de tenant.</summary>
    Task<Tenant?> GetBySlugAsync(string slug);
}
