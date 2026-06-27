using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsEstruturaService
{
    Task<IEnumerable<KidsSalaDto>> GetSalasAsync(bool incluirInativas = false);
    Task<KidsSalaDto> CreateSalaAsync(CreateKidsSalaRequest request);
    Task<KidsSalaDto> UpdateSalaAsync(string id, UpdateKidsSalaRequest request);
    Task<IEnumerable<KidsTurmaDto>> GetTurmasAsync(string? salaId = null, bool incluirInativas = false);
    Task<KidsTurmaDto> CreateTurmaAsync(CreateKidsTurmaRequest request);
    Task<KidsTurmaDto> UpdateTurmaAsync(string id, UpdateKidsTurmaRequest request);
}

public class KidsEstruturaService : IKidsEstruturaService
{
    private readonly IKidsEstruturaRepository _repository;
    private readonly IKidsAuthorizationService _authorizationService;
    private readonly ITenantContext _tenantContext;

    public KidsEstruturaService(IKidsEstruturaRepository repository, IKidsAuthorizationService authorizationService, ITenantContext tenantContext)
    {
        _repository = repository;
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
    }

    public KidsEstruturaService(IKidsEstruturaRepository repository, IKidsAuthorizationService authorizationService)
        : this(repository, authorizationService, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<KidsSalaDto>> GetSalasAsync(bool incluirInativas = false)
    {
        await _authorizationService.EnsureOperadorAsync();
        var items = await _repository.GetSalasAsync(incluirInativas);
        return items.Select(MapSalaDto);
    }

    public async Task<KidsSalaDto> CreateSalaAsync(CreateKidsSalaRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var id = NormalizeId(request.Id);
        if (await _repository.GetSalaByIdAsync(id) != null)
        {
            throw new ArgumentException("Já existe uma sala com o identificador informado.");
        }

        var sala = new KidsSala
        {
            Id = id,
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = request.Nome.Trim(),
            CapacidadeMaxima = request.CapacidadeMaxima,
            Ativo = request.Ativo,
            DataCriacao = DateTime.UtcNow
        };

        var created = await _repository.CreateSalaAsync(sala);
        return MapSalaDto(created);
    }

    public async Task<KidsSalaDto> UpdateSalaAsync(string id, UpdateKidsSalaRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var sala = await _repository.GetSalaByIdAsync(id);
        if (sala == null)
        {
            throw new ArgumentException("Sala não encontrada.");
        }

        sala.Nome = request.Nome.Trim();
        sala.CapacidadeMaxima = request.CapacidadeMaxima;
        sala.Ativo = request.Ativo;
        sala.DataAtualizacao = DateTime.UtcNow;

        var updated = await _repository.UpdateSalaAsync(sala);
        return MapSalaDto(updated);
    }

    public async Task<IEnumerable<KidsTurmaDto>> GetTurmasAsync(string? salaId = null, bool incluirInativas = false)
    {
        await _authorizationService.EnsureOperadorAsync();
        var items = await _repository.GetTurmasAsync(salaId, incluirInativas);
        return items.Select(MapTurmaDto);
    }

    public async Task<KidsTurmaDto> CreateTurmaAsync(CreateKidsTurmaRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var id = NormalizeId(request.Id);
        var salaId = NormalizeId(request.SalaId);

        if (await _repository.GetTurmaByIdAsync(id) != null)
        {
            throw new ArgumentException("Já existe uma turma com o identificador informado.");
        }

        var sala = await _repository.GetSalaByIdAsync(salaId);
        if (sala == null || !sala.Ativo)
        {
            throw new ArgumentException("Sala não encontrada ou inativa.");
        }

        var turma = new KidsTurma
        {
            Id = id,
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            SalaId = salaId,
            Nome = request.Nome.Trim(),
            CapacidadeMaxima = request.CapacidadeMaxima,
            Ativo = request.Ativo,
            DataCriacao = DateTime.UtcNow
        };

        var created = await _repository.CreateTurmaAsync(turma);
        created.Sala = sala;
        return MapTurmaDto(created);
    }

    public async Task<KidsTurmaDto> UpdateTurmaAsync(string id, UpdateKidsTurmaRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var turma = await _repository.GetTurmaByIdAsync(id);
        if (turma == null)
        {
            throw new ArgumentException("Turma não encontrada.");
        }

        var salaId = NormalizeId(request.SalaId);
        var sala = await _repository.GetSalaByIdAsync(salaId);
        if (sala == null || !sala.Ativo)
        {
            throw new ArgumentException("Sala não encontrada ou inativa.");
        }

        turma.SalaId = salaId;
        turma.Nome = request.Nome.Trim();
        turma.CapacidadeMaxima = request.CapacidadeMaxima;
        turma.Ativo = request.Ativo;
        turma.DataAtualizacao = DateTime.UtcNow;
        turma.Sala = sala;

        var updated = await _repository.UpdateTurmaAsync(turma);
        return MapTurmaDto(updated);
    }

    private static KidsSalaDto MapSalaDto(KidsSala item)
    {
        return new KidsSalaDto
        {
            Id = item.Id,
            Nome = item.Nome,
            CapacidadeMaxima = item.CapacidadeMaxima,
            Ativo = item.Ativo,
            DataCriacao = item.DataCriacao,
            DataAtualizacao = item.DataAtualizacao
        };
    }

    private static KidsTurmaDto MapTurmaDto(KidsTurma item)
    {
        return new KidsTurmaDto
        {
            Id = item.Id,
            SalaId = item.SalaId,
            SalaNome = item.Sala?.Nome,
            Nome = item.Nome,
            CapacidadeMaxima = item.CapacidadeMaxima,
            Ativo = item.Ativo,
            DataCriacao = item.DataCriacao,
            DataAtualizacao = item.DataAtualizacao
        };
    }

    private static string NormalizeId(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
