using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IEnqueteService
{
    Task<IEnumerable<EnqueteDto>> GetAllAsync();
    Task<EnqueteDto?> GetByIdAsync(int id);
    Task<IEnumerable<EnqueteDto>> GetAtivasAsync();
    Task<EnqueteDto> CreateAsync(CriarEnqueteDto dto);
    Task<EnqueteDto> UpdateAsync(int id, AtualizarEnqueteDto dto);
    Task DeleteAsync(int id);
}

public class EnqueteService : IEnqueteService
{
    private readonly IEnqueteRepository _repository;
    private readonly ITenantContext _tenantContext;

    public EnqueteService(IEnqueteRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public EnqueteService(IEnqueteRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<EnqueteDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<EnqueteDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<IEnumerable<EnqueteDto>> GetAtivasAsync()
    {
        var entities = await _repository.GetAtivasAsync();
        return entities.Select(MapToDto);
    }

    public async Task<EnqueteDto> CreateAsync(CriarEnqueteDto dto)
    {
        var entity = new Enquete
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Titulo = dto.Titulo,
            Descricao = dto.Descricao,
            DataInicio = dto.DataInicio,
            DataFim = dto.DataFim,
            Ativo = dto.Ativo,
            PermitirMultiplaEscolha = dto.PermitirMultiplaEscolha,
            PermitirVotoAnonimo = dto.PermitirVotoAnonimo,
            DataCriacao = DateTime.Now
        };

        // Adiciona opções
        foreach (var opcaoDto in dto.Opcoes.OrderBy(o => o.Ordem))
        {
            entity.Opcoes.Add(new EnqueteOpcao
            {
                TenantId = entity.TenantId,
                Texto = opcaoDto.Texto,
                Ordem = opcaoDto.Ordem,
                DataCriacao = DateTime.Now
            });
        }

        var created = await _repository.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<EnqueteDto> UpdateAsync(int id, AtualizarEnqueteDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Enquete não encontrada");

        entity.Titulo = dto.Titulo;
        entity.Descricao = dto.Descricao;
        entity.DataInicio = dto.DataInicio;
        entity.DataFim = dto.DataFim;
        entity.Ativo = dto.Ativo;
        entity.PermitirMultiplaEscolha = dto.PermitirMultiplaEscolha;
        entity.PermitirVotoAnonimo = dto.PermitirVotoAnonimo;

        // Atualiza opções
        var opcoesExistentes = entity.Opcoes.ToList();
        var opcoesParaManter = dto.Opcoes.Where(o => o.Id.HasValue).Select(o => o.Id!.Value).ToList();
        
        // Remove opções que não estão mais no DTO
        var opcoesParaRemover = opcoesExistentes.Where(o => !opcoesParaManter.Contains(o.Id)).ToList();
        foreach (var opcao in opcoesParaRemover)
        {
            await _repository.DeleteOpcaoAsync(opcao.Id);
        }

        // Atualiza ou cria opções
        foreach (var opcaoDto in dto.Opcoes.OrderBy(o => o.Ordem))
        {
            if (opcaoDto.Id.HasValue)
            {
                // Atualiza opção existente
                var opcaoExistente = opcoesExistentes.FirstOrDefault(o => o.Id == opcaoDto.Id.Value);
                if (opcaoExistente != null)
                {
                    opcaoExistente.Texto = opcaoDto.Texto;
                    opcaoExistente.Ordem = opcaoDto.Ordem;
                    await _repository.UpdateOpcaoAsync(opcaoExistente);
                }
            }
            else
            {
                // Cria nova opção
                var novaOpcao = new EnqueteOpcao
                {
                    TenantId = entity.TenantId,
                    EnqueteId = id,
                    Texto = opcaoDto.Texto,
                    Ordem = opcaoDto.Ordem,
                    DataCriacao = DateTime.Now
                };
                await _repository.CreateOpcaoAsync(novaOpcao);
            }
        }

        var updated = await _repository.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private EnqueteDto MapToDto(Enquete e)
    {
        var totalVotos = e.Votos?.Count ?? 0;
        var opcoesDto = e.Opcoes?.Select(o =>
        {
            var votosOpcao = e.Votos?.Count(v => v.EnqueteOpcaoId == o.Id) ?? 0;
            var percentual = totalVotos > 0 ? (votosOpcao * 100.0 / totalVotos) : 0;
            
            return new EnqueteOpcaoDto
            {
                Id = o.Id,
                EnqueteId = o.EnqueteId,
                Texto = o.Texto,
                Ordem = o.Ordem,
                TotalVotos = votosOpcao,
                PercentualVotos = Math.Round(percentual, 2)
            };
        }).OrderBy(o => o.Ordem).ToList() ?? new List<EnqueteOpcaoDto>();

        return new EnqueteDto
        {
            Id = e.Id,
            Titulo = e.Titulo,
            Descricao = e.Descricao,
            DataInicio = e.DataInicio,
            DataFim = e.DataFim,
            Ativo = e.Ativo,
            PermitirMultiplaEscolha = e.PermitirMultiplaEscolha,
            PermitirVotoAnonimo = e.PermitirVotoAnonimo,
            DataCriacao = e.DataCriacao,
            Opcoes = opcoesDto,
            TotalVotos = totalVotos
        };
    }
}
