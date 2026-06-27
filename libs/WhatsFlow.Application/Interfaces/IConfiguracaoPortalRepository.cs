using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IConfiguracaoPortalRepository
{
    Task<ConfiguracaoPortal?> GetAsync();
    Task<ConfiguracaoPortal> UpdateAsync(ConfiguracaoPortal configuracao);
}
