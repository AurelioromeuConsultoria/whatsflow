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
    private readonly IMensagemAgendadaRepository _mensagemAgendadaRepository;
    private readonly IConfiguracaoMensagemRepository _configuracaoMensagemRepository;
    private readonly IContatoRepository _contatoRepository;

    public DashboardService(
        IMensagemAgendadaRepository mensagemAgendadaRepository,
        IConfiguracaoMensagemRepository configuracaoMensagemRepository,
        IContatoRepository contatoRepository)
    {
        _mensagemAgendadaRepository = mensagemAgendadaRepository;
        _configuracaoMensagemRepository = configuracaoMensagemRepository;
        _contatoRepository = contatoRepository;
    }

    public async Task<DashboardDto> GetEstatisticasAsync()
    {
        var mensagensAgendadas = await _mensagemAgendadaRepository.GetMensagensPorStatusAsync(StatusMensagem.Agendada);
        var mensagensEnviadas = await _mensagemAgendadaRepository.GetMensagensPorStatusAsync(StatusMensagem.Enviada);
        var configuracoesAtivas = await _configuracaoMensagemRepository.GetAtivasAsync();
        var contatos = await _contatoRepository.GetAllAsync();
        var listaContatos = contatos.ToList();

        // TODO(WhatsFlow Etapa 4C): aniversariantes dependiam de Pessoa.DataNascimento (removida);
        // reavaliar quando o Contato tiver campo de data de nascimento.
        return new DashboardDto
        {
            TotalContatos = listaContatos.Count,
            ContatosAtivos = listaContatos.Count(c => c.Status == ContatoStatus.Ativo),
            ContatosOptIn = listaContatos.Count(c => c.OptIn),
            MensagensAgendadas = mensagensAgendadas.Count(),
            MensagensEnviadas = mensagensEnviadas.Count(),
            ConfiguracoesAtivas = configuracoesAtivas.Count()
        };
    }

    public async Task<List<DashboardSeriePontoDto>> GetSerieAsync(int meses = 6)
    {
        if (meses <= 0 || meses > 24) meses = 6;

        var contatos = (await _contatoRepository.GetAllAsync()).ToList();
        var enviadas = await _mensagemAgendadaRepository.GetMensagensPorStatusAsync(StatusMensagem.Enviada);

        string[] abrev = { "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez" };
        var hoje = DateTime.Today;
        var primeiroMesAtual = new DateTime(hoje.Year, hoje.Month, 1);

        var pontos = new List<DashboardSeriePontoDto>();
        for (var i = meses - 1; i >= 0; i--)
        {
            var inicioMes = primeiroMesAtual.AddMonths(-i);
            var inicioProximo = inicioMes.AddMonths(1);

            pontos.Add(new DashboardSeriePontoDto
            {
                Mes = $"{abrev[inicioMes.Month - 1]}/{inicioMes.Year % 100:00}",
                // Cumulativo (total até o fim do mês) — bate com os totais dos cards no último ponto.
                Contatos = contatos.Count(c => c.CriadoEm < inicioProximo),
                // Por mês (atividade do período).
                MensagensEnviadas = enviadas.Count(m => m.DataEnvio >= inicioMes && m.DataEnvio < inicioProximo)
            });
        }

        return pontos;
    }
}
