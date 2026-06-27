using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IPerfilAcessoService
{
    Task<IEnumerable<PerfilAcessoDto>> GetAllAsync();
    Task<PerfilAcessoDto?> GetByIdAsync(int id);
    Task<PerfilAcessoDto> CreateAsync(CriarPerfilAcessoDto dto);
    Task<PerfilAcessoDto> UpdateAsync(int id, AtualizarPerfilAcessoDto dto);
    Task DeleteAsync(int id);
}

public class PerfilAcessoService : IPerfilAcessoService
{
    private readonly IPerfilAcessoRepository _repository;
    private readonly ITenantContext _tenantContext;

    public PerfilAcessoService(IPerfilAcessoRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public PerfilAcessoService(IPerfilAcessoRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<PerfilAcessoDto>> GetAllAsync()
    {
        var perfis = await _repository.GetAllAsync();
        return perfis.Select(MapToDto);
    }

    public async Task<PerfilAcessoDto?> GetByIdAsync(int id)
    {
        var perfil = await _repository.GetByIdAsync(id);
        return perfil != null ? MapToDto(perfil) : null;
    }

    public async Task<PerfilAcessoDto> CreateAsync(CriarPerfilAcessoDto dto)
    {
        var perfil = new PerfilAcesso
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            DataCriacao = DateTime.Now,
            Permissoes = new List<PerfilAcessoPermissao>()
        };

        // Adicionar permissões após criar o perfil para garantir que o relacionamento seja estabelecido
        foreach (var permDto in dto.Permissoes)
        {
            perfil.Permissoes.Add(new PerfilAcessoPermissao
            {
                TenantId = perfil.TenantId,
                Recurso = permDto.Recurso,
                PodeVer = permDto.PodeVer,
                PodeEditar = permDto.PodeEditar,
                PodeExcluir = permDto.PodeExcluir
            });
        }

        var created = await _repository.CreateAsync(perfil);
        return MapToDto(created);
    }

    public async Task<PerfilAcessoDto> UpdateAsync(int id, AtualizarPerfilAcessoDto dto)
    {
        var perfil = await _repository.GetByIdAsync(id);
        if (perfil == null) throw new ArgumentException("Perfil não encontrado");

        perfil.Nome = dto.Nome;
        perfil.Descricao = dto.Descricao;

        perfil.Permissoes.Clear();
        foreach (var permDto in dto.Permissoes)
        {
            perfil.Permissoes.Add(new PerfilAcessoPermissao
            {
                TenantId = perfil.TenantId,
                Recurso = permDto.Recurso,
                PodeVer = permDto.PodeVer,
                PodeEditar = permDto.PodeEditar,
                PodeExcluir = permDto.PodeExcluir
            });
        }

        var updated = await _repository.UpdateAsync(perfil);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private static PerfilAcessoDto MapToDto(PerfilAcesso perfil)
    {
        return new PerfilAcessoDto
        {
            Id = perfil.Id,
            Nome = perfil.Nome,
            Descricao = perfil.Descricao,
            DataCriacao = perfil.DataCriacao,
            Permissoes = perfil.Permissoes.Select(p => new PermissaoPerfilDto
            {
                Id = p.Id,
                Recurso = p.Recurso,
                PodeVer = p.PodeVer,
                PodeEditar = p.PodeEditar,
                PodeExcluir = p.PodeExcluir
            }).ToList()
        };
    }

    private static PerfilAcessoPermissao MapToEntity(PermissaoPerfilDto dto)
    {
        return new PerfilAcessoPermissao
        {
            Recurso = dto.Recurso,
            PodeVer = dto.PodeVer,
            PodeEditar = dto.PodeEditar,
            PodeExcluir = dto.PodeExcluir
        };
    }
}
