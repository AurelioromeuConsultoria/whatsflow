using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface IPessoaPerfilService
{
    Task<IEnumerable<PessoaPerfilDto>> GetAllAsync();
    Task<PessoaPerfilDto?> GetByIdAsync(int id);
    Task<IEnumerable<PessoaPerfilDto>> GetPerfisPorPessoaAsync(int pessoaId);
    Task<PessoaPerfilDto> CreateAsync(CriarPessoaPerfilDto dto);
    Task<PessoaPerfilDto> UpdateAsync(int id, AtualizarPessoaPerfilDto dto);
    Task DeleteAsync(int id);
}

public class PessoaPerfilService : IPessoaPerfilService
{
    private readonly IPessoaPerfilRepository _repository;
    private readonly IPessoaRepository _pessoaRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PessoaPerfilService> _logger;

    public PessoaPerfilService(IPessoaPerfilRepository repository, IPessoaRepository pessoaRepository, ILogger<PessoaPerfilService> logger)
        : this(repository, pessoaRepository, new DefaultTenantContext(), logger)
    {
    }

    public PessoaPerfilService(IPessoaPerfilRepository repository, IPessoaRepository pessoaRepository, ITenantContext tenantContext, ILogger<PessoaPerfilService> logger)
    {
        _repository = repository;
        _pessoaRepository = pessoaRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<PessoaPerfilDto>> GetAllAsync()
    {
        var perfis = await _repository.GetAllAsync();
        return perfis.Select(p => MapToDto(p, p.Pessoa));
    }

    public async Task<PessoaPerfilDto?> GetByIdAsync(int id)
    {
        var perfil = await _repository.GetByIdAsync(id);
        if (perfil == null) return null;

        return MapToDto(perfil, perfil.Pessoa);
    }

    public async Task<IEnumerable<PessoaPerfilDto>> GetPerfisPorPessoaAsync(int pessoaId)
    {
        var perfis = await _repository.GetPerfisPorPessoaAsync(pessoaId);
        return perfis.Select(p => MapToDto(p, p.Pessoa));
    }

    public async Task<PessoaPerfilDto> CreateAsync(CriarPessoaPerfilDto dto)
    {
        // Validar se pessoa existe
        var pessoa = await _pessoaRepository.GetByIdAsync(dto.PessoaId);
        if (pessoa == null) throw new ArgumentException("Pessoa não encontrada");

        // Verificar se já existe perfil ativo do mesmo tipo
        var perfilAtivo = await _repository.GetPerfilAtivoAsync(dto.PessoaId, dto.Perfil);
        if (perfilAtivo != null) throw new InvalidOperationException($"Já existe um perfil {dto.Perfil} ativo para esta pessoa");

        var perfil = new PessoaPerfil
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            PessoaId = dto.PessoaId,
            Perfil = dto.Perfil,
            DataInicio = dto.DataInicio ?? DateTime.UtcNow,
            DataFim = null
        };

        var created = await _repository.CreateAsync(perfil);
        _logger.LogInformation(
            "Perfil de pessoa criado. PessoaPerfilId={PessoaPerfilId} PessoaId={PessoaId} Perfil={Perfil}",
            created.Id,
            created.PessoaId,
            created.Perfil);
        return MapToDto(created, pessoa);
    }

    public async Task<PessoaPerfilDto> UpdateAsync(int id, AtualizarPessoaPerfilDto dto)
    {
        var perfil = await _repository.GetByIdAsync(id);
        if (perfil == null) throw new ArgumentException("Perfil não encontrado");

        var pessoa = await _pessoaRepository.GetByIdAsync(perfil.PessoaId);
        if (pessoa == null) throw new ArgumentException("Pessoa não encontrada");

        perfil.Perfil = dto.Perfil;
        perfil.DataFim = dto.DataFim;

        var updated = await _repository.UpdateAsync(perfil);
        _logger.LogInformation(
            "Perfil de pessoa atualizado. PessoaPerfilId={PessoaPerfilId} PessoaId={PessoaId} Perfil={Perfil} Ativo={Ativo}",
            updated.Id,
            updated.PessoaId,
            updated.Perfil,
            updated.DataFim == null);
        // Recarregar com relacionamento
        var updatedCompleto = await _repository.GetByIdAsync(updated.Id);
        return MapToDto(updatedCompleto ?? updated, pessoa);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Perfil de pessoa removido. PessoaPerfilId={PessoaPerfilId}", id);
    }

    private static PessoaPerfilDto MapToDto(PessoaPerfil perfil, Pessoa? pessoa)
    {
        return new PessoaPerfilDto
        {
            Id = perfil.Id,
            PessoaId = perfil.PessoaId,
            NomePessoa = pessoa?.Nome ?? string.Empty,
            Perfil = perfil.Perfil,
            PerfilDescricao = GetPerfilDescricao(perfil.Perfil),
            DataInicio = perfil.DataInicio,
            DataFim = perfil.DataFim,
            Ativo = perfil.DataFim == null
        };
    }

    private static string GetPerfilDescricao(PerfilPessoa perfil)
    {
        return perfil switch
        {
            PerfilPessoa.Visitante => "Visitante",
            PerfilPessoa.Membro => "Membro",
            PerfilPessoa.Voluntario => "Voluntário",
            PerfilPessoa.Lider => "Líder",
            PerfilPessoa.Kids => "Kids",
            PerfilPessoa.Admin => "Administrador",
            _ => "Desconhecido"
        };
    }
}
