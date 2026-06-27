using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsPreCheckinService
{
    Task<KidsPreCheckinDto> CriarMeuPreCheckinAsync(CreateKidsPreCheckinRequest request);
    Task<IEnumerable<KidsPreCheckinDto>> GetMeusPreCheckinsAsync(string? status = null, bool somenteAtivos = false);
    Task<KidsPreCheckinDto> CancelarMeuPreCheckinAsync(int preCheckinId, CancelKidsPreCheckinRequest request);
    Task<IEnumerable<KidsPreCheckinDto>> GetPendentesAsync(int? eventoOcorrenciaId = null, string? salaId = null, string? turmaId = null);
    Task<KidsPreCheckinDto> ValidarAsync(ValidarKidsPreCheckinRequest request);
    Task<KidsPreCheckinDto> ConfirmarAsync(int preCheckinId, ConfirmKidsPreCheckinRequest request);
    Task<KidsPreCheckinDto> CancelarAsync(int preCheckinId, CancelKidsPreCheckinRequest request);
    Task<int> ExpirarPendentesAsync(DateTime? referenciaUtc = null);
}

public class KidsPreCheckinService : IKidsPreCheckinService
{
    private readonly IKidsPreCheckinRepository _preCheckinRepository;
    private readonly IResponsavelCriancaRepository _responsavelRepository;
    private readonly ICriancaDetalheRepository _criancaDetalheRepository;
    private readonly IKidsCheckinRepository _checkinRepository;
    private readonly IEventoOcorrenciaRepository _eventoOcorrenciaRepository;
    private readonly IKidsAuthorizationService _authorizationService;
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IKidsService _kidsService;
    private readonly ILogger<KidsPreCheckinService> _logger;

    public KidsPreCheckinService(
        IKidsPreCheckinRepository preCheckinRepository,
        IResponsavelCriancaRepository responsavelRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IKidsCheckinRepository checkinRepository,
        IEventoOcorrenciaRepository eventoOcorrenciaRepository,
        IKidsAuthorizationService authorizationService,
        IPessoaRepository pessoaRepository,
        IKidsService kidsService,
        ILogger<KidsPreCheckinService> logger)
    {
        _preCheckinRepository = preCheckinRepository;
        _responsavelRepository = responsavelRepository;
        _criancaDetalheRepository = criancaDetalheRepository;
        _checkinRepository = checkinRepository;
        _eventoOcorrenciaRepository = eventoOcorrenciaRepository;
        _authorizationService = authorizationService;
        _pessoaRepository = pessoaRepository;
        _kidsService = kidsService;
        _logger = logger;
    }

    public async Task<KidsPreCheckinDto> CriarMeuPreCheckinAsync(CreateKidsPreCheckinRequest request)
    {
        var context = await _authorizationService.GetCurrentContextAsync();

        await EnsureCriancaElegivelAsync(request.CriancaPessoaId, context.PessoaId);
        await EnsureEventoOcorrenciaValidaAsync(request.EventoOcorrenciaId);

        var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(request.CriancaPessoaId);
        if (checkinAtivo != null)
        {
            throw new InvalidOperationException("A criança já possui check-in ativo.");
        }

        var existente = await _preCheckinRepository.GetAtivoPorCriancaESessaoAsync(request.CriancaPessoaId, request.EventoOcorrenciaId);
        if (existente != null)
        {
            return MapToDto(existente);
        }

        var preCheckin = new KidsPreCheckin
        {
            CriancaPessoaId = request.CriancaPessoaId,
            ResponsavelPessoaId = context.PessoaId,
            EventoOcorrenciaId = request.EventoOcorrenciaId,
            SalaId = request.SalaId,
            TurmaId = request.TurmaId,
            QrToken = GenerateToken(40),
            CodigoCurto = GenerateCode(8),
            Status = "Pending",
            ExpiraEm = DateTime.UtcNow.AddMinutes(10),
            ObservacoesResponsavel = request.Observacoes?.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        var criado = await _preCheckinRepository.CreateAsync(preCheckin);

        _logger.LogInformation(
            "Pre-checkin Kids criado. PreCheckinId={PreCheckinId} CriancaPessoaId={CriancaPessoaId} ResponsavelPessoaId={ResponsavelPessoaId} EventoOcorrenciaId={EventoOcorrenciaId}",
            criado.Id,
            criado.CriancaPessoaId,
            criado.ResponsavelPessoaId,
            criado.EventoOcorrenciaId);

        return MapToDto(criado);
    }

    public async Task<IEnumerable<KidsPreCheckinDto>> GetMeusPreCheckinsAsync(string? status = null, bool somenteAtivos = false)
    {
        var context = await _authorizationService.GetCurrentContextAsync();
        var itens = await _preCheckinRepository.GetByResponsavelIdAsync(context.PessoaId, NormalizeStatus(status), somenteAtivos);
        return itens.Select(MapToDto).ToList();
    }

    public async Task<KidsPreCheckinDto> CancelarMeuPreCheckinAsync(int preCheckinId, CancelKidsPreCheckinRequest request)
    {
        var context = await _authorizationService.GetCurrentContextAsync();
        var item = await _preCheckinRepository.GetByIdAsync(preCheckinId)
            ?? throw new ArgumentException("Pré-check-in não encontrado.");

        if (item.ResponsavelPessoaId != context.PessoaId)
        {
            throw new UnauthorizedAccessException("Este pré-check-in não pertence ao responsável autenticado.");
        }

        return await CancelInternalAsync(
            item,
            context.PessoaId,
            string.IsNullOrWhiteSpace(request.Motivo) ? "Cancelado pela família no AppKids." : request.Motivo.Trim());
    }

    public async Task<IEnumerable<KidsPreCheckinDto>> GetPendentesAsync(int? eventoOcorrenciaId = null, string? salaId = null, string? turmaId = null)
    {
        await _authorizationService.EnsureOperadorAsync();
        var itens = await _preCheckinRepository.GetPendentesAsync(eventoOcorrenciaId, salaId, turmaId);
        return itens.Select(MapToDto).ToList();
    }

    public async Task<KidsPreCheckinDto> ValidarAsync(ValidarKidsPreCheckinRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();

        if (string.IsNullOrWhiteSpace(request.QrToken) && string.IsNullOrWhiteSpace(request.CodigoCurto))
        {
            throw new ArgumentException("Informe o token ou o código do pré-check-in.");
        }

        var item = !string.IsNullOrWhiteSpace(request.QrToken)
            ? await _preCheckinRepository.GetByQrTokenAsync(request.QrToken.Trim())
            : await _preCheckinRepository.GetByCodigoCurtoAsync(request.CodigoCurto!.Trim());

        if (item == null)
        {
            throw new ArgumentException("Pré-check-in não encontrado.");
        }

        if (item.Status == "Cancelled")
        {
            throw new InvalidOperationException("Este pré-check-in foi cancelado.");
        }

        if (item.Status == "Expired" || item.ExpiraEm <= DateTime.UtcNow)
        {
            if (item.Status == "Pending")
            {
                item.Status = "Expired";
                await _preCheckinRepository.UpdateAsync(item);
            }
            throw new InvalidOperationException("Este pré-check-in expirou.");
        }

        return MapToDto(item);
    }

    public async Task<KidsPreCheckinDto> ConfirmarAsync(int preCheckinId, ConfirmKidsPreCheckinRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();
        var context = await _authorizationService.GetCurrentContextAsync();
        var item = await _preCheckinRepository.GetByIdAsync(preCheckinId)
            ?? throw new ArgumentException("Pré-check-in não encontrado.");

        if (item.Status == "Cancelled")
        {
            throw new InvalidOperationException("Este pré-check-in já foi cancelado.");
        }

        if (item.Status == "Expired" || item.ExpiraEm <= DateTime.UtcNow)
        {
            if (item.Status == "Pending")
            {
                item.Status = "Expired";
                await _preCheckinRepository.UpdateAsync(item);
            }
            throw new InvalidOperationException("Este pré-check-in expirou.");
        }

        var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(item.CriancaPessoaId);
        if (checkinAtivo != null)
        {
            item.Status = "Confirmed";
            item.CheckinId ??= checkinAtivo.Id;
            item.ConfirmadoEm ??= DateTime.UtcNow;
            item.ConfirmadoPorPessoaId ??= context.PessoaId;
            item.SalaId = request.SalaId ?? item.SalaId;
            item.TurmaId = request.TurmaId ?? item.TurmaId;
            await _preCheckinRepository.UpdateAsync(item);
            return MapToDto(item);
        }

        var observacoes = BuildObservacoes(item.ObservacoesResponsavel, request.ObservacoesEquipe);
        var response = await _kidsService.CheckinAsync(new CheckinRequest
        {
            CriancaPessoaId = item.CriancaPessoaId,
            Metodo = "PRECHECKIN",
            CheckinByPessoaId = context.PessoaId,
            Observacoes = observacoes
        });

        item.Status = "Confirmed";
        item.CheckinId = response.CheckinId;
        item.ConfirmadoEm = DateTime.UtcNow;
        item.ConfirmadoPorPessoaId = context.PessoaId;
        item.SalaId = request.SalaId ?? item.SalaId;
        item.TurmaId = request.TurmaId ?? item.TurmaId;

        await _preCheckinRepository.UpdateAsync(item);

        _logger.LogInformation(
            "Pré-check-in Kids confirmado. PreCheckinId={PreCheckinId} CheckinId={CheckinId} CriancaPessoaId={CriancaPessoaId} ConfirmadoPorPessoaId={ConfirmadoPorPessoaId}",
            item.Id,
            item.CheckinId,
            item.CriancaPessoaId,
            context.PessoaId);

        return MapToDto(item);
    }

    public async Task<KidsPreCheckinDto> CancelarAsync(int preCheckinId, CancelKidsPreCheckinRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();
        var context = await _authorizationService.GetCurrentContextAsync();
        var item = await _preCheckinRepository.GetByIdAsync(preCheckinId)
            ?? throw new ArgumentException("Pré-check-in não encontrado.");

        return await CancelInternalAsync(
            item,
            context.PessoaId,
            string.IsNullOrWhiteSpace(request.Motivo) ? "Cancelado pela equipe." : request.Motivo.Trim());
    }

    public async Task<int> ExpirarPendentesAsync(DateTime? referenciaUtc = null)
    {
        var now = referenciaUtc ?? DateTime.UtcNow;
        var expirados = await _preCheckinRepository.GetExpiradosPendentesAsync(now);
        var total = 0;

        foreach (var item in expirados)
        {
            item.Status = "Expired";
            await _preCheckinRepository.UpdateAsync(item);
            total++;
        }

        if (total > 0)
        {
            _logger.LogInformation("Pre-checkins Kids expirados. Total={Total}", total);
        }

        return total;
    }

    private async Task EnsureCriancaElegivelAsync(int criancaPessoaId, int responsavelPessoaId)
    {
        var possuiVinculo = await _responsavelRepository.ExisteVinculoAtivoAsync(criancaPessoaId, responsavelPessoaId);
        if (!possuiVinculo)
        {
            throw new UnauthorizedAccessException("Esta criança não pertence ao responsável autenticado.");
        }

        var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(criancaPessoaId);
        if (detalhe == null)
        {
            throw new ArgumentException("Criança não encontrada no módulo Kids.");
        }

        var pessoa = await _pessoaRepository.GetByIdAsync(criancaPessoaId);
        if (pessoa == null || !pessoa.Ativo)
        {
            throw new ArgumentException("Criança inválida ou inativa.");
        }
    }

    private async Task EnsureEventoOcorrenciaValidaAsync(int? eventoOcorrenciaId)
    {
        if (!eventoOcorrenciaId.HasValue)
        {
            return;
        }

        var eventoOcorrencia = await _eventoOcorrenciaRepository.GetByIdAsync(eventoOcorrenciaId.Value);
        if (eventoOcorrencia == null)
        {
            throw new ArgumentException("Sessão/ocorrência não encontrada.");
        }
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
    }

    private async Task<KidsPreCheckinDto> CancelInternalAsync(KidsPreCheckin item, int canceladoPorPessoaId, string motivo)
    {
        if (item.Status == "Confirmed")
        {
            throw new InvalidOperationException("Este pré-check-in já foi confirmado e não pode ser cancelado.");
        }

        if (item.Status == "Cancelled")
        {
            return MapToDto(item);
        }

        item.Status = "Cancelled";
        item.CanceladoEm = DateTime.UtcNow;
        item.CanceladoPorPessoaId = canceladoPorPessoaId;
        item.CancelamentoMotivo = motivo;

        await _preCheckinRepository.UpdateAsync(item);

        _logger.LogInformation(
            "Pré-check-in Kids cancelado. PreCheckinId={PreCheckinId} CriancaPessoaId={CriancaPessoaId} CanceladoPorPessoaId={CanceladoPorPessoaId}",
            item.Id,
            item.CriancaPessoaId,
            canceladoPorPessoaId);

        return MapToDto(item);
    }

    private static KidsPreCheckinDto MapToDto(KidsPreCheckin item)
    {
        return new KidsPreCheckinDto
        {
            Id = item.Id,
            CriancaPessoaId = item.CriancaPessoaId,
            CriancaNome = item.Crianca?.Nome ?? string.Empty,
            ResponsavelPessoaId = item.ResponsavelPessoaId,
            ResponsavelNome = item.Responsavel?.Nome ?? string.Empty,
            EventoOcorrenciaId = item.EventoOcorrenciaId,
            CheckinId = item.CheckinId,
            EventoDataHoraInicio = item.EventoOcorrencia?.DataHoraInicio,
            SalaId = item.SalaId,
            TurmaId = item.TurmaId,
            QrToken = item.QrToken,
            CodigoCurto = item.CodigoCurto,
            Status = item.Status,
            ExpiraEm = item.ExpiraEm,
            ObservacoesResponsavel = item.ObservacoesResponsavel,
            CriadoEm = item.CriadoEm,
            ConfirmadoEm = item.ConfirmadoEm,
            ConfirmadoPorNome = item.ConfirmadoPor?.Nome,
            CanceladoEm = item.CanceladoEm,
            CanceladoPorNome = item.CanceladoPor?.Nome,
            CancelamentoMotivo = item.CancelamentoMotivo
        };
    }

    private static string GenerateToken(int bytesLength)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(bytesLength));
    }

    private static string GenerateCode(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[buffer[i] % alphabet.Length];
        }

        return new string(chars);
    }

    private static string? BuildObservacoes(string? observacaoResponsavel, string? observacaoEquipe)
    {
        var partes = new List<string>();

        if (!string.IsNullOrWhiteSpace(observacaoResponsavel))
        {
            partes.Add($"Responsável: {observacaoResponsavel.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(observacaoEquipe))
        {
            partes.Add($"Equipe: {observacaoEquipe.Trim()}");
        }

        return partes.Count == 0 ? null : string.Join(" | ", partes);
    }
}
