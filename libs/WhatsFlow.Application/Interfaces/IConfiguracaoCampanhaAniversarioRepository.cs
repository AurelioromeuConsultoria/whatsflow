using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IConfiguracaoCampanhaAniversarioRepository
{
    Task<ConfiguracaoCampanhaAniversario> GetAsync();
    Task<ConfiguracaoCampanhaAniversario> UpdateAsync(ConfiguracaoCampanhaAniversario configuracao);
}
