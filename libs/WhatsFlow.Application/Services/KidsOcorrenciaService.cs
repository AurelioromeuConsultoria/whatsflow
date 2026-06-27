using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsOcorrenciaService
{
    Task<KidsOcorrenciaDto> CriarAsync(CriarKidsOcorrenciaRequest request);
    Task<KidsOcorrenciaDto> AtualizarAsync(int id, AtualizarKidsOcorrenciaRequest request);
    Task<IEnumerable<KidsOcorrenciaDto>> GetByCriancaAsync(int criancaPessoaId);
    Task<IEnumerable<KidsOcorrenciaResumoDto>> GetAbertasAsync();
}

public class KidsOcorrenciaService : IKidsOcorrenciaService
{
    private readonly IKidsOcorrenciaRepository _repository;
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IKidsCheckinRepository _checkinRepository;
    private readonly ICriancaDetalheRepository _criancaDetalheRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IKidsAuthorizationService _authorizationService;
    private readonly ITenantContext _tenantContext;

    public KidsOcorrenciaService(
        IKidsOcorrenciaRepository repository,
        IPessoaRepository pessoaRepository,
        IKidsCheckinRepository checkinRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _pessoaRepository = pessoaRepository;
        _checkinRepository = checkinRepository;
        _criancaDetalheRepository = criancaDetalheRepository;
        _usuarioRepository = usuarioRepository;
        _currentUserContext = currentUserContext;
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
    }

    public KidsOcorrenciaService(
        IKidsOcorrenciaRepository repository,
        IPessoaRepository pessoaRepository,
        IKidsCheckinRepository checkinRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService)
        : this(repository, pessoaRepository, checkinRepository, criancaDetalheRepository, usuarioRepository, currentUserContext, authorizationService, new DefaultTenantContext())
    {
    }

    public async Task<KidsOcorrenciaDto> CriarAsync(CriarKidsOcorrenciaRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();
        var crianca = await _pessoaRepository.GetByIdAsync(request.CriancaPessoaId);
        if (crianca == null || !crianca.Ativo || crianca.TipoPessoa != TipoPessoa.Crianca)
        {
            throw new ArgumentException("Criança não encontrada ou inativa.");
        }

        KidsCheckin? checkin = null;
        if (request.CheckinId.HasValue)
        {
            checkin = await _checkinRepository.GetByIdAsync(request.CheckinId.Value);
            if (checkin == null || checkin.CriancaPessoaId != request.CriancaPessoaId)
            {
                throw new ArgumentException("Check-in não encontrado para a criança informada.");
            }
        }

        var registradoPorPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(request.CriancaPessoaId);

        var entity = new KidsOcorrencia
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            CriancaPessoaId = request.CriancaPessoaId,
            CheckinId = request.CheckinId,
            Tipo = request.Tipo.Trim().ToUpperInvariant(),
            Titulo = request.Titulo.Trim(),
            Descricao = request.Descricao.Trim(),
            Status = "Aberta",
            RequerContatoResponsavel = request.RequerContatoResponsavel,
            SalaId = detalhe?.SalaId,
            TurmaId = detalhe?.TurmaId,
            RegistradoPorPessoaId = registradoPorPessoaId,
            DataCriacao = DateTime.UtcNow,
            VisivelAoResponsavel = request.VisivelAoResponsavel
        };

        var created = await _repository.CreateAsync(entity);
        var loaded = await _repository.GetByIdAsync(created.Id) ?? created;
        return MapToDto(loaded);
    }

    public async Task<KidsOcorrenciaDto> AtualizarAsync(int id, AtualizarKidsOcorrenciaRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new ArgumentException("Ocorrência não encontrada.");
        }

        var pessoaAtualId = await GetRequiredCurrentUserPessoaIdAsync();

        if (!string.IsNullOrWhiteSpace(request.Descricao))
        {
            entity.Descricao = request.Descricao.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            entity.Status = request.Status.Trim();
            if (string.Equals(entity.Status, "Encerrada", StringComparison.OrdinalIgnoreCase))
            {
                entity.EncerradoEm = DateTime.UtcNow;
                entity.EncerradoPorPessoaId = pessoaAtualId;
            }
            else
            {
                entity.EncerradoEm = null;
                entity.EncerradoPorPessoaId = null;
            }
        }

        if (request.ContatoResponsavelRealizado == true)
        {
            entity.ContatoResponsavelRealizadoEm = DateTime.UtcNow;
            entity.ContatoResponsavelPorPessoaId = pessoaAtualId;
        }

        if (request.VisivelAoResponsavel.HasValue)
        {
            entity.VisivelAoResponsavel = request.VisivelAoResponsavel.Value;
        }

        entity.DataAtualizacao = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(entity);
        var loaded = await _repository.GetByIdAsync(updated.Id) ?? updated;
        return MapToDto(loaded);
    }

    public async Task<IEnumerable<KidsOcorrenciaDto>> GetByCriancaAsync(int criancaPessoaId)
    {
        await _authorizationService.EnsureOperadorAsync();
        var items = await _repository.GetByCriancaIdAsync(criancaPessoaId);
        return items.Select(MapToDto);
    }

    public async Task<IEnumerable<KidsOcorrenciaResumoDto>> GetAbertasAsync()
    {
        await _authorizationService.EnsureOperadorAsync();
        var items = await _repository.GetAbertasAsync();
        return items.Select(MapToResumoDto);
    }

    private async Task<int> GetRequiredCurrentUserPessoaIdAsync()
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuário atual não identificado.");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(_currentUserContext.UserId.Value);
        if (usuario == null || !usuario.Ativo)
        {
            throw new UnauthorizedAccessException("Usuário atual não identificado.");
        }

        return usuario.PessoaId;
    }

    private static KidsOcorrenciaDto MapToDto(KidsOcorrencia item)
    {
        return new KidsOcorrenciaDto
        {
            Id = item.Id,
            CriancaPessoaId = item.CriancaPessoaId,
            CriancaNome = item.Crianca?.Nome ?? string.Empty,
            CheckinId = item.CheckinId,
            Tipo = item.Tipo,
            Titulo = item.Titulo,
            Descricao = item.Descricao,
            Status = item.Status,
            RequerContatoResponsavel = item.RequerContatoResponsavel,
            ContatoResponsavelRealizadoEm = item.ContatoResponsavelRealizadoEm,
            ContatoResponsavelPorNome = item.ContatoResponsavelPor?.Nome,
            SalaId = item.SalaId,
            TurmaId = item.TurmaId,
            RegistradoPorPessoaId = item.RegistradoPorPessoaId,
            RegistradoPorNome = item.RegistradoPor?.Nome ?? string.Empty,
            DataCriacao = item.DataCriacao,
            DataAtualizacao = item.DataAtualizacao,
            EncerradoEm = item.EncerradoEm,
            EncerradoPorNome = item.EncerradoPor?.Nome,
            VisivelAoResponsavel = item.VisivelAoResponsavel
        };
    }

    private static KidsOcorrenciaResumoDto MapToResumoDto(KidsOcorrencia item)
    {
        return new KidsOcorrenciaResumoDto
        {
            Id = item.Id,
            CriancaPessoaId = item.CriancaPessoaId,
            CriancaNome = item.Crianca?.Nome ?? string.Empty,
            Tipo = item.Tipo,
            Status = item.Status,
            DataCriacao = item.DataCriacao
        };
    }
}
