using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IConfiguracaoPortalService
{
    Task<ConfiguracaoPortalDto> GetAsync();
    Task<ConfiguracaoPortalDto> UpdateAsync(AtualizarConfiguracaoPortalDto dto);
}

public class ConfiguracaoPortalService : IConfiguracaoPortalService
{
    private readonly IConfiguracaoPortalRepository _repository;

    public ConfiguracaoPortalService(IConfiguracaoPortalRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConfiguracaoPortalDto> GetAsync()
    {
        var entity = await _repository.GetAsync();
        return MapToDto(entity!);
    }

    public async Task<ConfiguracaoPortalDto> UpdateAsync(AtualizarConfiguracaoPortalDto dto)
    {
        var entity = new ConfiguracaoPortal
        {
            TempoTransicaoCarrossel = dto.TempoTransicaoCarrossel > 0 ? dto.TempoTransicaoCarrossel : 5
        };
        
        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    private static ConfiguracaoPortalDto MapToDto(ConfiguracaoPortal c)
    {
        return new ConfiguracaoPortalDto
        {
            Id = c.Id,
            TempoTransicaoCarrossel = c.TempoTransicaoCarrossel > 0 ? c.TempoTransicaoCarrossel : 5,
            DataAtualizacao = c.DataAtualizacao
        };
    }
}
