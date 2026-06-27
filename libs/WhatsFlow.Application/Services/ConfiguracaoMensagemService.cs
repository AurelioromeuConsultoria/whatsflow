using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IConfiguracaoMensagemService
{
    Task<IEnumerable<ConfiguracaoMensagemDto>> GetAllAsync();
    Task<ConfiguracaoMensagemDto?> GetByIdAsync(int id);
    Task<ConfiguracaoMensagemDto> CreateAsync(CriarConfiguracaoMensagemDto dto);
    Task<ConfiguracaoMensagemDto> UpdateAsync(int id, AtualizarConfiguracaoMensagemDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<ConfiguracaoMensagemDto>> GetAtivasAsync();
}

public class ConfiguracaoMensagemService : IConfiguracaoMensagemService
{
    private readonly IConfiguracaoMensagemRepository _repository;

    public ConfiguracaoMensagemService(IConfiguracaoMensagemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ConfiguracaoMensagemDto>> GetAllAsync()
    {
        var configuracoes = await _repository.GetAllAsync();
        return configuracoes.Select(MapToDto);
    }

    public async Task<ConfiguracaoMensagemDto?> GetByIdAsync(int id)
    {
        var configuracao = await _repository.GetByIdAsync(id);
        return configuracao != null ? MapToDto(configuracao) : null;
    }

    public async Task<ConfiguracaoMensagemDto> CreateAsync(CriarConfiguracaoMensagemDto dto)
    {
        var configuracao = new ConfiguracaoMensagem
        {
            Nome = dto.Nome,
            TextoMensagem = dto.TextoMensagem,
            DiasAposVisita = dto.DiasAposVisita,
            HorarioEnvio = dto.HorarioEnvio,
            Ativo = dto.Ativo,
            DataCriacao = DateTime.Now
        };

        var configuracaoCriada = await _repository.CreateAsync(configuracao);
        return MapToDto(configuracaoCriada);
    }

    public async Task<ConfiguracaoMensagemDto> UpdateAsync(int id, AtualizarConfiguracaoMensagemDto dto)
    {
        var configuracao = await _repository.GetByIdAsync(id);
        if (configuracao == null)
            throw new ArgumentException("Configuração de mensagem não encontrada");

        configuracao.Nome = dto.Nome;
        configuracao.TextoMensagem = dto.TextoMensagem;
        configuracao.DiasAposVisita = dto.DiasAposVisita;
        configuracao.HorarioEnvio = dto.HorarioEnvio;
        configuracao.Ativo = dto.Ativo;

        var configuracaoAtualizada = await _repository.UpdateAsync(configuracao);
        return MapToDto(configuracaoAtualizada);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ConfiguracaoMensagemDto>> GetAtivasAsync()
    {
        var configuracoes = await _repository.GetAtivasAsync();
        return configuracoes.Select(MapToDto);
    }

    private static ConfiguracaoMensagemDto MapToDto(ConfiguracaoMensagem configuracao)
    {
        return new ConfiguracaoMensagemDto
        {
            Id = configuracao.Id,
            Nome = configuracao.Nome,
            TextoMensagem = configuracao.TextoMensagem,
            DiasAposVisita = configuracao.DiasAposVisita,
            HorarioEnvio = configuracao.HorarioEnvio,
            Ativo = configuracao.Ativo,
            DataCriacao = configuracao.DataCriacao
        };
    }
}

