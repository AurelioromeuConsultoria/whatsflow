using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsPainelService
{
    Task<KidsPainelOperacionalDto> GetPainelOperacionalAsync(DateTime? data = null, string? salaId = null);
}

public class KidsPainelService : IKidsPainelService
{
    private readonly IKidsCheckinRepository _checkinRepository;
    private readonly ICriancaDetalheRepository _criancaDetalheRepository;
    private readonly IKidsAuthorizationService _authorizationService;

    public KidsPainelService(
        IKidsCheckinRepository checkinRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IKidsAuthorizationService authorizationService)
    {
        _checkinRepository = checkinRepository;
        _criancaDetalheRepository = criancaDetalheRepository;
        _authorizationService = authorizationService;
    }

    public async Task<KidsPainelOperacionalDto> GetPainelOperacionalAsync(DateTime? data = null, string? salaId = null)
    {
        await _authorizationService.EnsureOperadorAsync();
        var dataBase = (data ?? DateTime.UtcNow).Date;
        var inicioDia = DateTime.SpecifyKind(dataBase, DateTimeKind.Utc);
        var fimDia = inicioDia.AddDays(1).AddTicks(-1);

        var ativos = (await _checkinRepository.GetCheckinsAtivosAsync()).ToList();
        var checkinsDoDia = (await _checkinRepository.GetByPeriodoAsync(inicioDia, fimDia)).ToList();

        var detalhesPorCrianca = new Dictionary<int, CriancaDetalhe?>();
        foreach (var checkin in ativos)
        {
            if (!detalhesPorCrianca.ContainsKey(checkin.CriancaPessoaId))
            {
                detalhesPorCrianca[checkin.CriancaPessoaId] = await _criancaDetalheRepository.GetByPessoaIdAsync(checkin.CriancaPessoaId);
            }
        }

        var presentes = ativos
            .Select(c => MapToPainelCriancaDto(c, detalhesPorCrianca.GetValueOrDefault(c.CriancaPessoaId)))
            .Where(c => string.IsNullOrWhiteSpace(salaId) || string.Equals(c.SalaId, salaId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.CheckinTime)
            .ToList();

        var alertasCriticos = presentes
            .Where(c => c.TemAlergia || c.TemRestricao || c.TemObservacaoCritica)
            .ToList();

        var porSala = presentes
            .GroupBy(c => string.IsNullOrWhiteSpace(c.SalaId) ? "Sem sala" : c.SalaId!)
            .Select(g => new KidsPainelSalaDto
            {
                SalaId = g.Key,
                TotalPresentes = g.Count(),
                TotalPendentesRetirada = g.Count(),
                TotalAlertasCriticos = g.Count(c => c.TemAlergia || c.TemRestricao || c.TemObservacaoCritica)
            })
            .OrderBy(g => g.SalaId)
            .ToList();

        return new KidsPainelOperacionalDto
        {
            TotalPresentes = presentes.Count,
            TotalPendentesRetirada = presentes.Count,
            TotalRetiradasHoje = checkinsDoDia.Count(c => c.CheckoutTime.HasValue),
            TotalAlertasCriticos = alertasCriticos.Count,
            Salas = porSala,
            CriancasPresentes = presentes,
            Pendencias = presentes,
            AlertasCriticos = alertasCriticos
        };
    }

    private static KidsPainelCriancaDto MapToPainelCriancaDto(KidsCheckin checkin, CriancaDetalhe? detalhe)
    {
        return new KidsPainelCriancaDto
        {
            CriancaPessoaId = checkin.CriancaPessoaId,
            Nome = checkin.Crianca?.Nome ?? string.Empty,
            SalaId = detalhe?.SalaId,
            CheckinTime = checkin.CheckinTime,
            Status = checkin.Status,
            TemAlergia = !string.IsNullOrWhiteSpace(detalhe?.Alergias),
            TemRestricao = !string.IsNullOrWhiteSpace(detalhe?.RestricoesAlimentares),
            TemObservacaoCritica = !string.IsNullOrWhiteSpace(detalhe?.Observacoes),
            TokenRetiradaAtivo = !string.IsNullOrWhiteSpace(checkin.TokenRetirada) &&
                                 (!checkin.TokenRetiradaExpiraEm.HasValue || checkin.TokenRetiradaExpiraEm.Value >= DateTime.UtcNow),
            RetiradaEmModoExcecao = checkin.RetiradaEmModoExcecao
        };
    }
}
