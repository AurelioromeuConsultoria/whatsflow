using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsRetiradaService
{
    Task<RetiradaValidacaoDto> ValidarAsync(ValidarRetiradaRequest request);
    Task ConfirmarAsync(ConfirmarRetiradaRequest request);
    Task RegistrarExcecaoAsync(RetiradaExcecaoRequest request);
}

public class KidsRetiradaService : IKidsRetiradaService
{
    private readonly IKidsCheckinRepository _checkinRepository;
    private readonly IResponsavelCriancaRepository _responsavelRepository;
    private readonly ICriancaDetalheRepository _criancaDetalheRepository;
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IKidsNotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IKidsAuthorizationService _authorizationService;
    private readonly ITenantContext _tenantContext;
    private readonly IKidsPushNotificationService? _pushService;
    private readonly ILogger<KidsRetiradaService> _logger;

    public KidsRetiradaService(
        IKidsCheckinRepository checkinRepository,
        IResponsavelCriancaRepository responsavelRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IPessoaRepository pessoaRepository,
        IKidsNotificacaoRepository notificacaoRepository,
        IUnitOfWork unitOfWork,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService,
        ITenantContext tenantContext,
        ILogger<KidsRetiradaService> logger,
        IKidsPushNotificationService? pushService = null)
    {
        _checkinRepository = checkinRepository;
        _responsavelRepository = responsavelRepository;
        _criancaDetalheRepository = criancaDetalheRepository;
        _pessoaRepository = pessoaRepository;
        _notificacaoRepository = notificacaoRepository;
        _unitOfWork = unitOfWork;
        _usuarioRepository = usuarioRepository;
        _currentUserContext = currentUserContext;
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
        _logger = logger;
        _pushService = pushService;
    }

    public KidsRetiradaService(
        IKidsCheckinRepository checkinRepository,
        IResponsavelCriancaRepository responsavelRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IPessoaRepository pessoaRepository,
        IKidsNotificacaoRepository notificacaoRepository,
        IUnitOfWork unitOfWork,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService,
        ILogger<KidsRetiradaService> logger,
        IKidsPushNotificationService? pushService = null)
        : this(
            checkinRepository,
            responsavelRepository,
            criancaDetalheRepository,
            pessoaRepository,
            notificacaoRepository,
            unitOfWork,
            usuarioRepository,
            currentUserContext,
            authorizationService,
            new DefaultTenantContext(),
            logger,
            pushService)
    {
    }

    public async Task<RetiradaValidacaoDto> ValidarAsync(ValidarRetiradaRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();
        var (checkin, metodoValidado) = await ResolveCheckinAsync(request.Token, request.Pin);
        EnsureCheckinAtivo(checkin);

        var expirado = checkin.TokenRetiradaExpiraEm.HasValue && checkin.TokenRetiradaExpiraEm.Value < DateTime.UtcNow;
        var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(checkin.CriancaPessoaId);
        var responsaveis = (await _responsavelRepository.GetByCriancaIdAsync(checkin.CriancaPessoaId))
            .Where(r => r.Ativo && r.PodeRetirar)
            .ToList();

        return new RetiradaValidacaoDto
        {
            CheckinId = checkin.Id,
            CriancaPessoaId = checkin.CriancaPessoaId,
            CriancaNome = checkin.Crianca?.Nome ?? string.Empty,
            SalaId = detalhe?.SalaId,
            CheckinTime = checkin.CheckinTime,
            TokenRetiradaExpiraEm = checkin.TokenRetiradaExpiraEm,
            Expirado = expirado,
            MetodoValidado = metodoValidado,
            MetodosDisponiveis = GetMetodosDisponiveis(checkin),
            ResponsaveisAutorizados = responsaveis.Select(MapToAutorizadoDto).ToList()
        };
    }

    public async Task ConfirmarAsync(ConfirmarRetiradaRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();
        var operadorPessoaId = await GetRequiredCurrentUserPessoaIdAsync();

        _logger.LogInformation(
            "Confirmando retirada Kids. CheckinId={CheckinId}. ResponsavelPessoaId={ResponsavelPessoaId}. Metodo={Metodo}. OperadorPessoaId={OperadorPessoaId}",
            request.CheckinId,
            request.ResponsavelPessoaId,
            request.Metodo,
            operadorPessoaId);

        List<int>? responsavelIdsForPush = null;
        string? msgForPush = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (checkin, metodoValidado) = await ResolveCheckinAsync(request.Token, request.Pin);
            if (checkin.Id != request.CheckinId)
            {
                throw new ArgumentException("Check-in não corresponde ao token informado.");
            }

            EnsureCheckinAtivo(checkin);
            EnsureTokenNaoExpirado(checkin, metodoValidado);

            var podeRetirar = await _responsavelRepository.PodeRetirarAsync(checkin.CriancaPessoaId, request.ResponsavelPessoaId);
            if (!podeRetirar)
            {
                throw new UnauthorizedAccessException("Responsável não autorizado para retirada.");
            }

            checkin.CheckoutTime = DateTime.UtcNow;
            checkin.CheckoutByPessoaId = request.ResponsavelPessoaId;
            checkin.Status = "CheckedOut";
            checkin.RetiradaConfirmadaPorPessoaId = operadorPessoaId;
            checkin.RetiradaMetodo = string.IsNullOrWhiteSpace(request.Metodo) ? metodoValidado : request.Metodo.Trim().ToUpperInvariant();
            checkin.RetiradaEmModoExcecao = false;
            checkin.RetiradaMotivoExcecao = null;
            checkin.RetiradaPessoaNome = null;
            checkin.RetiradaPessoaDocumento = null;
            if (!string.IsNullOrWhiteSpace(request.Observacoes))
            {
                checkin.Observacoes = request.Observacoes.Trim();
            }

            await _checkinRepository.UpdateWithoutSaveAsync(checkin);

            var responsaveis = (await _responsavelRepository.GetByCriancaIdAsync(checkin.CriancaPessoaId)).ToList();
            responsavelIdsForPush = responsaveis.Select(r => r.ResponsavelPessoaId).Distinct().ToList();
            msgForPush = $"Check-out realizado para {checkin.Crianca?.Nome ?? "criança"} às {DateTime.UtcNow:HH:mm}";

            foreach (var responsavel in responsaveis)
            {
                await _notificacaoRepository.CreateWithoutSaveAsync(new KidsNotificacao
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    CriancaPessoaId = checkin.CriancaPessoaId,
                    ResponsavelPessoaId = responsavel.ResponsavelPessoaId,
                    Titulo = "Check-out realizado",
                    Tipo = "CHECKOUT",
                    Origem = "AUTOMATICA",
                    Mensagem = msgForPush,
                    Status = "Enviado",
                    EnviadoEm = DateTime.UtcNow,
                    DataCriacao = DateTime.UtcNow,
                    CriadoByPessoaId = operadorPessoaId
                });
            }

            await _unitOfWork.SaveChangesAsync();
        });

        if (_pushService != null && responsavelIdsForPush != null && responsavelIdsForPush.Count > 0 && msgForPush != null)
        {
            await _pushService.SendToPessoasAsync(
                responsavelIdsForPush,
                "App Kids - Check-out",
                msgForPush,
                new Dictionary<string, string> { ["tipo"] = "CHECKOUT" });
        }
    }

    public async Task RegistrarExcecaoAsync(RetiradaExcecaoRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();
        var operadorPessoaId = await GetRequiredCurrentUserPessoaIdAsync();

        _logger.LogWarning(
            "Registrando retirada em excecao Kids. CheckinId={CheckinId}. OperadorPessoaId={OperadorPessoaId}. PessoaRetirandoNome={PessoaRetirandoNome}",
            request.CheckinId,
            operadorPessoaId,
            request.PessoaRetirandoNome);

        List<int>? responsavelIdsForPush = null;
        string? msgForPush = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var checkin = await _checkinRepository.GetByIdAsync(request.CheckinId)
                ?? throw new ArgumentException("Check-in não encontrado.");

            EnsureCheckinAtivo(checkin);

            checkin.CheckoutTime = DateTime.UtcNow;
            checkin.CheckoutByPessoaId = null;
            checkin.Status = "CheckedOut";
            checkin.RetiradaConfirmadaPorPessoaId = operadorPessoaId;
            checkin.RetiradaMetodo = "EXCECAO";
            checkin.RetiradaEmModoExcecao = true;
            checkin.RetiradaMotivoExcecao = request.Motivo.Trim();
            checkin.RetiradaPessoaNome = request.PessoaRetirandoNome.Trim();
            checkin.RetiradaPessoaDocumento = request.PessoaRetirandoDocumento?.Trim();
            if (!string.IsNullOrWhiteSpace(request.Observacoes))
            {
                checkin.Observacoes = request.Observacoes.Trim();
            }

            await _checkinRepository.UpdateWithoutSaveAsync(checkin);

            var responsaveis = (await _responsavelRepository.GetByCriancaIdAsync(checkin.CriancaPessoaId)).ToList();
            responsavelIdsForPush = responsaveis.Select(r => r.ResponsavelPessoaId).Distinct().ToList();
            msgForPush = $"Retirada em exceção registrada para {checkin.Crianca?.Nome ?? "criança"} às {DateTime.UtcNow:HH:mm}.";

            foreach (var responsavel in responsaveis)
            {
                await _notificacaoRepository.CreateWithoutSaveAsync(new KidsNotificacao
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    CriancaPessoaId = checkin.CriancaPessoaId,
                    ResponsavelPessoaId = responsavel.ResponsavelPessoaId,
                    Titulo = "Retirada em exceção",
                    Tipo = "ALERTA",
                    Origem = "AUTOMATICA",
                    Mensagem = msgForPush,
                    Status = "Enviado",
                    EnviadoEm = DateTime.UtcNow,
                    DataCriacao = DateTime.UtcNow,
                    CriadoByPessoaId = operadorPessoaId
                });
            }

            await _unitOfWork.SaveChangesAsync();
        });

        if (_pushService != null && responsavelIdsForPush != null && responsavelIdsForPush.Count > 0 && msgForPush != null)
        {
            await _pushService.SendToPessoasAsync(
                responsavelIdsForPush,
                "App Kids - Alerta",
                msgForPush,
                new Dictionary<string, string> { ["tipo"] = "ALERTA" });
        }
    }

    private async Task<(KidsCheckin checkin, string metodoValidado)> ResolveCheckinAsync(string? token, string? pin)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var tokenNormalizado = token.Trim();
            var checkin = await _checkinRepository.GetByTokenRetiradaAsync(tokenNormalizado)
                ?? throw new ArgumentException("Token de retirada inválido.");
            return (checkin, "QR");
        }

        if (!string.IsNullOrWhiteSpace(pin))
        {
            var pinNormalizado = pin.Trim();
            var checkin = await _checkinRepository.GetByPinRetiradaAsync(pinNormalizado)
                ?? throw new ArgumentException("PIN de retirada inválido.");
            return (checkin, "PIN");
        }

        throw new ArgumentException("Informe token ou PIN para validar a retirada.");
    }

    private static void EnsureCheckinAtivo(KidsCheckin checkin)
    {
        if (checkin.Status != "CheckedIn" || checkin.CheckoutTime.HasValue)
        {
            throw new InvalidOperationException("Check-in não está ativo para retirada.");
        }
    }

    private static void EnsureTokenNaoExpirado(KidsCheckin checkin, string metodoValidado)
    {
        if (metodoValidado == "QR" &&
            checkin.TokenRetiradaExpiraEm.HasValue &&
            checkin.TokenRetiradaExpiraEm.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Token de retirada expirado.");
        }
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

    private static List<string> GetMetodosDisponiveis(KidsCheckin checkin)
    {
        var metodos = new List<string>();
        if (!string.IsNullOrWhiteSpace(checkin.TokenRetirada))
        {
            metodos.Add("QR");
        }

        if (!string.IsNullOrWhiteSpace(checkin.PinRetirada))
        {
            metodos.Add("PIN");
        }

        return metodos;
    }

    private static RetiradaAutorizadoDto MapToAutorizadoDto(ResponsavelCrianca item)
    {
        return new RetiradaAutorizadoDto
        {
            ResponsavelPessoaId = item.ResponsavelPessoaId,
            ResponsavelNome = item.Responsavel?.Nome ?? string.Empty,
            Parentesco = item.Parentesco,
            PodeRetirar = item.PodeRetirar
        };
    }
}
