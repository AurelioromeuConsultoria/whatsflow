using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetEstatisticasAsync();
    Task<List<DashboardSeriePontoDto>> GetSerieAsync(int meses = 6);
}

public class DashboardService : IDashboardService
{
    private readonly IVisitanteRepository _visitanteRepository;
    private readonly IMensagemAgendadaRepository _mensagemAgendadaRepository;
    private readonly IConfiguracaoMensagemRepository _configuracaoMensagemRepository;
    private readonly IPessoaRepository _pessoaRepository;

    public DashboardService(
        IVisitanteRepository visitanteRepository,
        IMensagemAgendadaRepository mensagemAgendadaRepository,
        IConfiguracaoMensagemRepository configuracaoMensagemRepository,
        IPessoaRepository pessoaRepository)
    {
        _visitanteRepository = visitanteRepository;
        _mensagemAgendadaRepository = mensagemAgendadaRepository;
        _configuracaoMensagemRepository = configuracaoMensagemRepository;
        _pessoaRepository = pessoaRepository;
    }

    public async Task<DashboardDto> GetEstatisticasAsync()
    {
        var visitantes = await _visitanteRepository.GetAllAsync();
        var mensagensAgendadas = await _mensagemAgendadaRepository.GetMensagensPorStatusAsync(StatusMensagem.Agendada);
        var mensagensEnviadas = await _mensagemAgendadaRepository.GetMensagensPorStatusAsync(StatusMensagem.Enviada);
        var configuracoesAtivas = await _configuracaoMensagemRepository.GetAtivasAsync();
        var pessoas = await _pessoaRepository.GetAllAsync();
        var aniversariantes = CalcularAniversariantes(pessoas, 30, 5).ToList();

        // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
        return new DashboardDto
        {
            TotalVisitantes = visitantes.Count(),
            MensagensAgendadas = mensagensAgendadas.Count(),
            MensagensEnviadas = mensagensEnviadas.Count(),
            ConfiguracoesAtivas = configuracoesAtivas.Count(),
            TotalPessoas = pessoas.Count(),
            TotalAniversariantesProximos = aniversariantes.Count,
            ProximosAniversariantes = aniversariantes
        };
    }

    public async Task<List<DashboardSeriePontoDto>> GetSerieAsync(int meses = 6)
    {
        if (meses <= 0 || meses > 24) meses = 6;

        var pessoas = await _pessoaRepository.GetAllAsync();
        var visitantes = await _visitanteRepository.GetAllAsync();
        var enviadas = await _mensagemAgendadaRepository.GetMensagensPorStatusAsync(StatusMensagem.Enviada);

        string[] abrev = { "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez" };
        var hoje = DateTime.Today;
        var primeiroMesAtual = new DateTime(hoje.Year, hoje.Month, 1);

        var pontos = new List<DashboardSeriePontoDto>();
        for (var i = meses - 1; i >= 0; i--)
        {
            var inicioMes = primeiroMesAtual.AddMonths(-i);
            var inicioProximo = inicioMes.AddMonths(1);

            // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
            pontos.Add(new DashboardSeriePontoDto
            {
                Mes = $"{abrev[inicioMes.Month - 1]}/{inicioMes.Year % 100:00}",
                // Cumulativo (total até o fim do mês) — bate com os totais dos cards no último ponto.
                Pessoas = pessoas.Count(p => p.DataCriacao < inicioProximo),
                Visitantes = visitantes.Count(v => v.DataCadastro < inicioProximo),
                // Por mês (atividade do período).
                MensagensEnviadas = enviadas.Count(m => m.DataEnvio >= inicioMes && m.DataEnvio < inicioProximo)
            });
        }

        return pontos;
    }

    private static IEnumerable<AniversarianteDto> CalcularAniversariantes(IEnumerable<Pessoa> pessoas, int dias, int limite)
    {
        if (dias <= 0) dias = 30;
        if (limite <= 0) limite = 5;

        var hoje = DateTime.Today;

        return pessoas
            .Where(p => p.Ativo && p.DataNascimento.HasValue)
            .Select(p =>
            {
                var nasc = p.DataNascimento!.Value.Date;
                var prox = GetProximoAniversario(nasc, hoje);
                var diasRestantes = (prox - hoje).Days;
                return new AniversarianteDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    DataNascimento = nasc,
                    ProximoAniversario = prox,
                    DiasParaAniversario = diasRestantes
                };
            })
            .Where(a => a.DiasParaAniversario <= dias && a.DiasParaAniversario >= 0)
            .OrderBy(a => a.DiasParaAniversario)
            .ThenBy(a => a.Nome)
            .Take(limite);
    }

    private static DateTime GetProximoAniversario(DateTime dataNascimento, DateTime hoje)
    {
        var ano = hoje.Year;
        var mes = dataNascimento.Month;
        var dia = dataNascimento.Day;

        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        if (dia > diasNoMes) dia = diasNoMes;

        var proximo = new DateTime(ano, mes, dia);
        if (proximo < hoje)
        {
            ano += 1;
            diasNoMes = DateTime.DaysInMonth(ano, mes);
            if (dia > diasNoMes) dia = diasNoMes;
            proximo = new DateTime(ano, mes, dia);
        }

        return proximo;
    }
}
