using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IContatoService
{
    Task<IEnumerable<ContatoDto>> GetAllAsync();
    Task<PagedResultDto<ContatoDto>> GetPagedAsync(ContatoPagedQueryDto query);
    Task<ContatoDto?> GetByIdAsync(int id);
    Task<ContatoDto> CreateAsync(CriarContatoDto dto);
    Task<ContatoDto> UpdateAsync(int id, AtualizarContatoDto dto);
    Task DeleteAsync(int id);
}

public class ContatoService : IContatoService
{
    private readonly IContatoRepository _repository;
    private readonly ITenantContext _tenantContext;

    public ContatoService(IContatoRepository repository)
        : this(repository, new DefaultTenantContext())
    {
    }

    public ContatoService(IContatoRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<ContatoDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<PagedResultDto<ContatoDto>> GetPagedAsync(ContatoPagedQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);
        var (items, total) = await _repository.GetPagedAsync(query);

        return new PagedResultDto<ContatoDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ContatoDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<ContatoDto> CreateAsync(CriarContatoDto dto)
    {
        var telefone = (dto.TelefoneWhatsApp ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw new ArgumentException("Telefone WhatsApp é obrigatório.");
        }

        var duplicado = await _repository.GetByTelefoneWhatsAppAsync(telefone);
        if (duplicado != null)
        {
            throw new ArgumentException($"Já existe um contato com o telefone WhatsApp {telefone}.");
        }

        var now = DateTime.UtcNow;
        var entity = new Contato
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome.Trim(),
            TelefoneWhatsApp = telefone,
            Email = Normalize(dto.Email),
            Documento = Normalize(dto.Documento),
            Organizacao = Normalize(dto.Organizacao),
            Observacoes = Normalize(dto.Observacoes),
            Origem = Normalize(dto.Origem),
            Status = dto.Status,
            OptIn = dto.OptIn,
            DataOptIn = dto.OptIn ? now : null,
            CriadoEm = now
        };

        await SyncTagsAsync(entity, dto.TagIds);

        var created = await _repository.CreateAsync(entity);
        var reloaded = await _repository.GetByIdAsync(created.Id) ?? created;
        return MapToDto(reloaded);
    }

    public async Task<ContatoDto> UpdateAsync(int id, AtualizarContatoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Contato não encontrado");

        var telefone = (dto.TelefoneWhatsApp ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw new ArgumentException("Telefone WhatsApp é obrigatório.");
        }

        var duplicado = await _repository.GetByTelefoneWhatsAppAsync(telefone, ignoreId: id);
        if (duplicado != null)
        {
            throw new ArgumentException($"Já existe um contato com o telefone WhatsApp {telefone}.");
        }

        var now = DateTime.UtcNow;
        var optInAnterior = entity.OptIn;

        entity.Nome = dto.Nome.Trim();
        entity.TelefoneWhatsApp = telefone;
        entity.Email = Normalize(dto.Email);
        entity.Documento = Normalize(dto.Documento);
        entity.Organizacao = Normalize(dto.Organizacao);
        entity.Observacoes = Normalize(dto.Observacoes);
        entity.Origem = Normalize(dto.Origem);
        entity.Status = dto.Status;
        entity.OptIn = dto.OptIn;
        entity.AtualizadoEm = now;

        if (dto.OptIn && !optInAnterior)
        {
            entity.DataOptIn = now;
            entity.DataOptOut = null;
        }
        else if (!dto.OptIn && optInAnterior)
        {
            entity.DataOptOut = now;
        }

        await SyncTagsAsync(entity, dto.TagIds);

        var updated = await _repository.UpdateAsync(entity);
        var reloaded = await _repository.GetByIdAsync(updated.Id) ?? updated;
        return MapToDto(reloaded);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private async Task SyncTagsAsync(Contato entity, List<int> tagIds)
    {
        var desejadas = (tagIds ?? new List<int>()).Distinct().ToList();

        // Remove associações que não estão mais na lista.
        var paraRemover = entity.ContatoTags.Where(ct => !desejadas.Contains(ct.TagId)).ToList();
        foreach (var ct in paraRemover)
        {
            entity.ContatoTags.Remove(ct);
        }

        var existentes = entity.ContatoTags.Select(ct => ct.TagId).ToHashSet();
        var novas = desejadas.Where(tagId => !existentes.Contains(tagId)).ToList();
        if (novas.Count == 0)
        {
            return;
        }

        var tagsValidas = await _repository.GetTagsByIdsAsync(novas);
        foreach (var tag in tagsValidas)
        {
            entity.ContatoTags.Add(new ContatoTag
            {
                TenantId = entity.TenantId,
                ContatoId = entity.Id,
                TagId = tag.Id
            });
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ContatoDto MapToDto(Contato c)
    {
        return new ContatoDto
        {
            Id = c.Id,
            Nome = c.Nome,
            TelefoneWhatsApp = c.TelefoneWhatsApp,
            Email = c.Email,
            Documento = c.Documento,
            Organizacao = c.Organizacao,
            Observacoes = c.Observacoes,
            Origem = c.Origem,
            Status = c.Status,
            OptIn = c.OptIn,
            DataOptIn = c.DataOptIn,
            DataOptOut = c.DataOptOut,
            CriadoEm = c.CriadoEm,
            AtualizadoEm = c.AtualizadoEm,
            Tags = c.ContatoTags
                .Where(ct => ct.Tag != null)
                .Select(ct => new ContatoTagDto
                {
                    Id = ct.Tag.Id,
                    Nome = ct.Tag.Nome,
                    Cor = ct.Tag.Cor
                })
                .ToList()
        };
    }
}
