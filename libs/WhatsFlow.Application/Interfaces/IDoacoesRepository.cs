using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IDoacoesRepository
{
    Task<IEnumerable<FinalidadeDoacao>> GetFinalidadesAsync(bool publicOnly = false);
    Task<FinalidadeDoacao?> GetFinalidadeByIdAsync(int id);
    Task<FinalidadeDoacao?> GetFinalidadeBySlugAsync(string slug);
    Task<FinalidadeDoacao> CreateFinalidadeAsync(FinalidadeDoacao finalidade);
    Task<FinalidadeDoacao> UpdateFinalidadeAsync(FinalidadeDoacao finalidade);
    Task DeleteFinalidadeAsync(int id);
    Task<IEnumerable<DoacaoOnline>> GetDoacoesAsync();
    Task<DoacaoOnline?> GetDoacaoByIdAsync(int id);
    Task<DoacaoOnline?> GetDoacaoByExternalPaymentIdAsync(string externalPaymentId);
    Task<DoacaoOnline?> GetDoacaoByReciboTokenAsync(string reciboToken);
    Task<DoacaoOnline> CreateDoacaoAsync(DoacaoOnline doacao);
    Task<DoacaoOnline> UpdateDoacaoAsync(DoacaoOnline doacao);
    Task<GivingProviderConfig?> GetProviderConfigAsync(GivingProvider provider);
    Task<GivingProviderConfig?> GetProviderConfigByTenantAsync(int tenantId, GivingProvider provider);
    Task<GivingProviderConfig> SaveProviderConfigAsync(GivingProviderConfig config);
    Task<DoacaoOnline> EnsureReceitaForDoacaoAsync(DoacaoOnline doacao);
}
