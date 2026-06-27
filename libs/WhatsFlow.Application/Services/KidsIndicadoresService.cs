using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsIndicadoresService
{
    Task<KidsIndicadoresDto> GetIndicadoresAsync(int dias = 30);
}

public class KidsIndicadoresService : IKidsIndicadoresService
{
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IResponsavelCriancaRepository _responsavelRepository;
    private readonly IKidsEstruturaRepository _estruturaRepository;
    private readonly IKidsCheckinRepository _checkinRepository;
    private readonly IKidsOcorrenciaRepository _ocorrenciaRepository;
    private readonly IKidsAuthorizationService _authorizationService;

    public KidsIndicadoresService(
        IPessoaRepository pessoaRepository,
        IResponsavelCriancaRepository responsavelRepository,
        IKidsEstruturaRepository estruturaRepository,
        IKidsCheckinRepository checkinRepository,
        IKidsOcorrenciaRepository ocorrenciaRepository,
        IKidsAuthorizationService authorizationService)
    {
        _pessoaRepository = pessoaRepository;
        _responsavelRepository = responsavelRepository;
        _estruturaRepository = estruturaRepository;
        _checkinRepository = checkinRepository;
        _ocorrenciaRepository = ocorrenciaRepository;
        _authorizationService = authorizationService;
    }

    public async Task<KidsIndicadoresDto> GetIndicadoresAsync(int dias = 30)
    {
        await _authorizationService.EnsureOperadorAsync();
        var diasNormalizados = Math.Clamp(dias, 1, 365);
        var fim = DateTime.UtcNow;
        var inicio = fim.AddDays(-diasNormalizados);

        var pessoas = await _pessoaRepository.GetAllAsync();
        var responsaveisAtivos = await _responsavelRepository.GetResponsavelIdsAtivosAsync();
        var salas = (await _estruturaRepository.GetSalasAsync()).ToList();
        var turmas = (await _estruturaRepository.GetTurmasAsync()).ToList();
        var checkinsPeriodo = (await _checkinRepository.GetByPeriodoAsync(inicio, fim)).ToList();
        var checkinsAtivos = (await _checkinRepository.GetCheckinsAtivosAsync()).ToList();
        var ocorrenciasAbertas = (await _ocorrenciaRepository.GetAbertasAsync()).ToList();

        return new KidsIndicadoresDto
        {
            DiasAnalisados = diasNormalizados,
            TotalCriancasAtivas = pessoas.Count(p => p.TipoPessoa == TipoPessoa.Crianca && p.Ativo),
            TotalResponsaveisAtivos = responsaveisAtivos.Distinct().Count(),
            TotalSalasAtivas = salas.Count,
            TotalTurmasAtivas = turmas.Count,
            TotalCheckinsPeriodo = checkinsPeriodo.Count,
            MediaCheckinsPorDia = Math.Round(checkinsPeriodo.Count / (decimal)diasNormalizados, 2),
            TotalRetiradasQr = checkinsPeriodo.Count(c => string.Equals(c.RetiradaMetodo, "QR", StringComparison.OrdinalIgnoreCase)),
            TotalRetiradasPin = checkinsPeriodo.Count(c => string.Equals(c.RetiradaMetodo, "PIN", StringComparison.OrdinalIgnoreCase)),
            TotalRetiradasExcecao = checkinsPeriodo.Count(c => c.RetiradaEmModoExcecao || string.Equals(c.RetiradaMetodo, "EXCECAO", StringComparison.OrdinalIgnoreCase)),
            TotalOcorrenciasAbertas = ocorrenciasAbertas.Count,
            TotalCriancasPresentesAgora = checkinsAtivos.Count
        };
    }
}
