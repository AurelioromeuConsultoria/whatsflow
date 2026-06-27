using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsNotificacaoService
{
    Task<IEnumerable<MeuAvisoKidsDto>> GetMeusAvisosAsync(bool somenteNaoLidos = false, string? tipo = null, int? criancaPessoaId = null, int? limit = null);
    Task<MeuAvisoKidsDto> MarcarComoLidoAsync(int id);
    Task<KidsNotificacaoDto> CriarAvisoAsync(CreateKidsAvisoRequest request);
    Task<IEnumerable<KidsNotificacaoDto>> GetAvisosAsync(string? tipo = null, int? responsavelPessoaId = null, int? criancaPessoaId = null, int? limit = null);
}

public class KidsNotificacaoService : IKidsNotificacaoService
{
    private readonly IKidsNotificacaoRepository _repository;
    private readonly IResponsavelCriancaRepository _responsavelCriancaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IKidsAuthorizationService _authorizationService;
    private readonly ITenantContext _tenantContext;
    private readonly IKidsPushNotificationService? _pushService;

    public KidsNotificacaoService(
        IKidsNotificacaoRepository repository,
        IResponsavelCriancaRepository responsavelCriancaRepository,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService,
        ITenantContext tenantContext,
        IKidsPushNotificationService? pushService = null)
    {
        _repository = repository;
        _responsavelCriancaRepository = responsavelCriancaRepository;
        _usuarioRepository = usuarioRepository;
        _currentUserContext = currentUserContext;
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
        _pushService = pushService;
    }

    public KidsNotificacaoService(
        IKidsNotificacaoRepository repository,
        IResponsavelCriancaRepository responsavelCriancaRepository,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService,
        IKidsPushNotificationService? pushService = null)
        : this(repository, responsavelCriancaRepository, usuarioRepository, currentUserContext, authorizationService, new DefaultTenantContext(), pushService)
    {
    }

    public async Task<IEnumerable<MeuAvisoKidsDto>> GetMeusAvisosAsync(bool somenteNaoLidos = false, string? tipo = null, int? criancaPessoaId = null, int? limit = null)
    {
        var responsavelPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        var items = await _repository.GetFeedByResponsavelIdAsync(responsavelPessoaId, somenteNaoLidos, tipo, criancaPessoaId, limit);
        return items.Select(MapToMeuAvisoDto);
    }

    public async Task<MeuAvisoKidsDto> MarcarComoLidoAsync(int id)
    {
        var responsavelPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        var item = await _repository.GetByIdAsync(id);

        if (item == null || item.ResponsavelPessoaId != responsavelPessoaId)
        {
            throw new UnauthorizedAccessException("Aviso não encontrado para o responsável atual.");
        }

        if (!item.LidoEm.HasValue)
        {
            item.LidoEm = DateTime.UtcNow;
            await _repository.UpdateAsync(item);
        }

        return MapToMeuAvisoDto(item);
    }

    public async Task<KidsNotificacaoDto> CriarAvisoAsync(CreateKidsAvisoRequest request)
    {
        await _authorizationService.EnsureLiderAsync();

        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            throw new ArgumentException("Título é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Mensagem))
        {
            throw new ArgumentException("Mensagem é obrigatória.");
        }

        var criadoByPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        var tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "AVISO_GERAL" : request.Tipo.Trim().ToUpperInvariant();
        var destino = string.IsNullOrWhiteSpace(request.Destino) ? "GERAL" : request.Destino.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;
        var titulo = request.Titulo.Trim();
        var mensagem = request.Mensagem.Trim();

        List<KidsNotificacao> notificacoes;
        switch (destino)
        {
            case "GERAL":
                notificacoes = await CriarAvisosGeraisAsync(titulo, mensagem, tipo, criadoByPessoaId, now);
                break;
            case "CRIANCA":
                notificacoes = await CriarAvisosPorCriancaAsync(request.CriancaPessoaIds, titulo, mensagem, tipo, criadoByPessoaId, now);
                break;
            case "RESPONSAVEL":
                notificacoes = CriarAvisosPorResponsavel(request.ResponsavelPessoaIds, titulo, mensagem, tipo, criadoByPessoaId, now);
                break;
            default:
                throw new ArgumentException("Destino de aviso inválido.");
        }

        if (notificacoes.Count == 0)
        {
            throw new InvalidOperationException("Nenhum destinatário válido foi encontrado para o aviso.");
        }

        await _repository.CreateRangeAsync(notificacoes);

        var responsavelIds = notificacoes.Select(n => n.ResponsavelPessoaId).Distinct().ToList();
        if (_pushService != null && responsavelIds.Count > 0)
        {
            await _pushService.SendToPessoasAsync(
                responsavelIds,
                titulo,
                mensagem,
                new Dictionary<string, string>
                {
                    ["tipo"] = tipo,
                    ["origem"] = "MANUAL"
                });
        }

        var primeira = await _repository.GetByIdAsync(notificacoes[0].Id) ?? notificacoes[0];
        return MapToKidsNotificacaoDto(primeira);
    }

    public async Task<IEnumerable<KidsNotificacaoDto>> GetAvisosAsync(string? tipo = null, int? responsavelPessoaId = null, int? criancaPessoaId = null, int? limit = null)
    {
        await _authorizationService.EnsureOperadorAsync();
        var items = await _repository.GetAdministrativosAsync(tipo, responsavelPessoaId, criancaPessoaId, limit);
        return items.Select(MapToKidsNotificacaoDto);
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

    private async Task<List<KidsNotificacao>> CriarAvisosGeraisAsync(string titulo, string mensagem, string tipo, int criadoByPessoaId, DateTime now)
    {
        var responsavelIds = await _responsavelCriancaRepository.GetResponsavelIdsAtivosAsync();
        return responsavelIds
            .Distinct()
            .Select(responsavelPessoaId => BuildNotificacao(_tenantContext.TenantId ?? Tenant.InitialTenantId, null, responsavelPessoaId, titulo, mensagem, tipo, criadoByPessoaId, now))
            .ToList();
    }

    private async Task<List<KidsNotificacao>> CriarAvisosPorCriancaAsync(IEnumerable<int> criancaPessoaIds, string titulo, string mensagem, string tipo, int criadoByPessoaId, DateTime now)
    {
        var criancas = criancaPessoaIds.Where(id => id > 0).Distinct().ToList();
        if (criancas.Count == 0)
        {
            throw new ArgumentException("Informe ao menos uma criança para o aviso segmentado.");
        }

        var itens = new List<KidsNotificacao>();
        foreach (var criancaPessoaId in criancas)
        {
            var responsaveis = await _responsavelCriancaRepository.GetByCriancaIdAsync(criancaPessoaId);
            foreach (var responsavelPessoaId in responsaveis.Where(r => r.Ativo).Select(r => r.ResponsavelPessoaId).Distinct())
            {
                itens.Add(BuildNotificacao(_tenantContext.TenantId ?? Tenant.InitialTenantId, criancaPessoaId, responsavelPessoaId, titulo, mensagem, tipo, criadoByPessoaId, now));
            }
        }

        return itens;
    }

    private List<KidsNotificacao> CriarAvisosPorResponsavel(IEnumerable<int> responsavelPessoaIds, string titulo, string mensagem, string tipo, int criadoByPessoaId, DateTime now)
    {
        var responsaveis = responsavelPessoaIds.Where(id => id > 0).Distinct().ToList();
        if (responsaveis.Count == 0)
        {
            throw new ArgumentException("Informe ao menos um responsável para o aviso segmentado.");
        }

        return responsaveis
            .Select(responsavelPessoaId => BuildNotificacao(_tenantContext.TenantId ?? Tenant.InitialTenantId, null, responsavelPessoaId, titulo, mensagem, tipo, criadoByPessoaId, now))
            .ToList();
    }

    private static KidsNotificacao BuildNotificacao(int tenantId, int? criancaPessoaId, int responsavelPessoaId, string titulo, string mensagem, string tipo, int criadoByPessoaId, DateTime now)
    {
        return new KidsNotificacao
        {
            TenantId = tenantId,
            CriancaPessoaId = criancaPessoaId,
            ResponsavelPessoaId = responsavelPessoaId,
            Titulo = titulo,
            Tipo = tipo,
            Origem = "MANUAL",
            Mensagem = mensagem,
            EnviadoEm = now,
            Status = "Enviado",
            DataCriacao = now,
            CriadoByPessoaId = criadoByPessoaId
        };
    }

    private static MeuAvisoKidsDto MapToMeuAvisoDto(KidsNotificacao item)
    {
        return new MeuAvisoKidsDto
        {
            Id = item.Id,
            Titulo = item.Titulo,
            Mensagem = item.Mensagem,
            Tipo = item.Tipo,
            Origem = item.Origem,
            CriancaPessoaId = item.CriancaPessoaId,
            CriancaNome = item.Crianca?.Nome,
            DataCriacao = item.DataCriacao,
            EnviadoEm = item.EnviadoEm,
            LidoEm = item.LidoEm,
            FoiLido = item.LidoEm.HasValue
        };
    }

    private static KidsNotificacaoDto MapToKidsNotificacaoDto(KidsNotificacao item)
    {
        return new KidsNotificacaoDto
        {
            Id = item.Id,
            CriancaPessoaId = item.CriancaPessoaId,
            CriancaNome = item.Crianca?.Nome,
            ResponsavelPessoaId = item.ResponsavelPessoaId,
            ResponsavelNome = item.Responsavel?.Nome ?? string.Empty,
            Titulo = item.Titulo,
            Tipo = item.Tipo,
            Origem = item.Origem,
            Mensagem = item.Mensagem,
            EnviadoEm = item.EnviadoEm,
            Status = item.Status,
            LidoEm = item.LidoEm,
            FoiLido = item.LidoEm.HasValue,
            CriadoByPessoaId = item.CriadoByPessoaId,
            DataCriacao = item.DataCriacao
        };
    }
}
