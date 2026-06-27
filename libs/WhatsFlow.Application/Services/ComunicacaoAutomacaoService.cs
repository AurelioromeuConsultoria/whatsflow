using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Auditoria;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoAutomacaoService
{
    Task<ComunicacaoAutomacaoExecucaoResumoDto> ExecutarNovoVisitanteAsync(int visitanteId, CancellationToken cancellationToken = default);
    // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
    Task<int> ExecutarLembretesOperacionaisAsync(IEnumerable<ComunicacaoLembreteOperacionalRequest> lembretes, CancellationToken cancellationToken = default);
    Task<int> ExecutarAvisoContextualKidsAsync(ComunicacaoAvisoContextualKidsRequest request, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ComunicacaoAutomacaoHistoricoItemDto>> GetHistoricoAsync(ComunicacaoAutomacaoHistoricoQueryDto query);
}

public class ComunicacaoAutomacaoService : IComunicacaoAutomacaoService
{
    private readonly IVisitanteRepository _visitanteRepository;
    private readonly IConfiguracaoMensagemRepository _configuracaoMensagemRepository;
    private readonly IComunicacaoCampanhaRepository _campanhaRepository;
    private readonly IComunicacaoEntregaRepository _entregaRepository;
    private readonly IComunicacaoPreferenciaService _preferenciaService;
    private readonly IComunicacaoProcessamentoService _processamentoService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ComunicacaoAutomacaoService> _logger;

    public ComunicacaoAutomacaoService(
        IVisitanteRepository visitanteRepository,
        IConfiguracaoMensagemRepository configuracaoMensagemRepository,
        IComunicacaoCampanhaRepository campanhaRepository,
        IComunicacaoEntregaRepository entregaRepository,
        IComunicacaoPreferenciaService preferenciaService,
        IComunicacaoProcessamentoService processamentoService,
        IAuditLogService auditLogService,
        ILogger<ComunicacaoAutomacaoService> logger)
    {
        _visitanteRepository = visitanteRepository;
        _configuracaoMensagemRepository = configuracaoMensagemRepository;
        _campanhaRepository = campanhaRepository;
        _entregaRepository = entregaRepository;
        _preferenciaService = preferenciaService;
        _processamentoService = processamentoService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<ComunicacaoAutomacaoExecucaoResumoDto> ExecutarNovoVisitanteAsync(int visitanteId, CancellationToken cancellationToken = default)
    {
        var visitante = await _visitanteRepository.GetByIdAsync(visitanteId)
            ?? throw new ArgumentException("Visitante não encontrado");

        var configuracoes = (await _configuracaoMensagemRepository.GetAtivasAsync())
            .OrderBy(x => x.DiasAposVisita)
            .ThenBy(x => x.HorarioEnvio)
            .ToList();

        var resultado = new ComunicacaoAutomacaoExecucaoResumoDto
        {
            Gatilho = "novo-visitante"
        };

        if (configuracoes.Count == 0)
        {
            resultado.TotalIgnoradas = 1;
            return resultado;
        }

        foreach (var configuracao in configuracoes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dataAgendamento = visitante.DataVisita.Date.AddDays(configuracao.DiasAposVisita) + configuracao.HorarioEnvio;
            var campanha = await _campanhaRepository.CreateAsync(new ComunicacaoCampanha
            {
                Nome = $"Automação visitante D+{configuracao.DiasAposVisita} - {visitante.Pessoa?.Nome ?? $"Visitante {visitante.Id}"}",
                Objetivo = "onboarding-visitante",
                PublicoAlvo = "visitantes",
                Origem = TipoOrigemComunicacao.Automatica,
                Status = dataAgendamento > DateTime.Now ? StatusComunicacaoCampanha.Agendada : StatusComunicacaoCampanha.Rascunho,
                DataAgendamento = dataAgendamento,
                DataCriacao = DateTime.UtcNow,
                Canais =
                [
                    new ComunicacaoCampanhaCanal
                    {
                        Canal = CanalComunicacao.WhatsApp,
                        Prioridade = 1
                    }
                ]
            });

            await _entregaRepository.CreateAsync(await CriarEntregaVisitanteAsync(campanha, visitante, configuracao));
            resultado.TotalCriadas++;
        }

        _logger.LogInformation(
            "{EventName} Gatilho=novo-visitante VisitanteId={VisitanteId} Quantidade={Quantidade}",
            ComunicacaoObservability.Events.CampanhaCriada,
            visitanteId,
            resultado.TotalCriadas);
        await _auditLogService.RecordAsync("ComunicacaoAutomacao", visitanteId.ToString(), "ExecutarNovoVisitante", new
        {
            VisitanteId = visitanteId,
            resultado.TotalCriadas
        });

        return resultado;
    }

    // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)

    public async Task<int> ExecutarLembretesOperacionaisAsync(IEnumerable<ComunicacaoLembreteOperacionalRequest> lembretes, CancellationToken cancellationToken = default)
    {
        var total = 0;

        foreach (var lembrete in lembretes.Where(x => x.PessoaId > 0 && !string.IsNullOrWhiteSpace(x.ChaveEvento)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await JaExecutadoAsync("ExecutarLembreteOperacional", lembrete.ChaveEvento))
            {
                continue;
            }

            var campanha = await _campanhaRepository.CreateAsync(new ComunicacaoCampanha
            {
                Nome = lembrete.Titulo,
                Objetivo = string.IsNullOrWhiteSpace(lembrete.Objetivo) ? "lembrete-operacional" : lembrete.Objetivo.Trim(),
                PublicoAlvo = "pessoas",
                Origem = TipoOrigemComunicacao.Automatica,
                Status = StatusComunicacaoCampanha.Processando,
                DataAgendamento = DateTime.Now,
                DataCriacao = DateTime.UtcNow,
                Canais =
                [
                    new ComunicacaoCampanhaCanal
                    {
                        Canal = CanalComunicacao.NotificacaoInterna,
                        Prioridade = 1
                    }
                ]
            });

            var entrega = await _entregaRepository.CreateAsync(new ComunicacaoEntrega
            {
                ComunicacaoCampanhaId = campanha.Id,
                DestinatarioPessoaId = lembrete.PessoaId,
                Canal = CanalComunicacao.NotificacaoInterna,
                DestinoResolvido = $"pessoa:{lembrete.PessoaId}",
                RemetenteResolvido = lembrete.Titulo,
                ConteudoFinal = lembrete.Mensagem,
                Status = StatusComunicacaoEntrega.Pendente,
                ChaveDedupe = lembrete.ChaveEvento,
                DataCriacao = DateTime.UtcNow
            });

            if (await _preferenciaService.EstaBloqueadoAsync(lembrete.PessoaId, CanalComunicacao.NotificacaoInterna))
            {
                entrega.Status = StatusComunicacaoEntrega.IgnoradoPorPreferencia;
                entrega.Erro = $"Entrega ignorada: pessoa {lembrete.PessoaId} bloqueou o canal {CanalComunicacao.NotificacaoInterna}.";
                await _entregaRepository.UpdateAsync(entrega);
            }
            else
            {
                await _processamentoService.ProcessarEntregaAsync(entrega.Id, cancellationToken);
            }
            await RegistrarExecucaoAsync("ExecutarLembreteOperacional", lembrete.ChaveEvento, new
            {
                lembrete.PessoaId,
                lembrete.Titulo,
                CampanhaId = campanha.Id,
                EntregaId = entrega.Id
            });
            total++;
        }

        return total;
    }

    public async Task<int> ExecutarAvisoContextualKidsAsync(ComunicacaoAvisoContextualKidsRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ChaveEvento))
        {
            throw new ArgumentException("Chave do evento é obrigatória.");
        }

        if (await JaExecutadoAsync("ExecutarAvisoContextualKids", request.ChaveEvento))
        {
            return 0;
        }

        var responsavelIds = request.ResponsavelPessoaIds.Where(x => x > 0).Distinct().ToList();
        if (responsavelIds.Count == 0)
        {
            return 0;
        }

        var campanha = await _campanhaRepository.CreateAsync(new ComunicacaoCampanha
        {
            Nome = request.Titulo,
            Objetivo = "kids-contextual",
            PublicoAlvo = "responsaveis-kids",
            Origem = TipoOrigemComunicacao.Automatica,
            Status = StatusComunicacaoCampanha.Processando,
            DataAgendamento = DateTime.Now,
            DataCriacao = DateTime.UtcNow,
            Canais =
            [
                new ComunicacaoCampanhaCanal
                {
                    Canal = CanalComunicacao.Push,
                    Prioridade = 1
                }
            ]
        });

        var total = 0;
        foreach (var responsavelPessoaId in responsavelIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entrega = await _entregaRepository.CreateAsync(new ComunicacaoEntrega
            {
                ComunicacaoCampanhaId = campanha.Id,
                DestinatarioPessoaId = responsavelPessoaId,
                Canal = CanalComunicacao.Push,
                DestinoResolvido = $"pessoa:{responsavelPessoaId}",
                RemetenteResolvido = request.Titulo,
                ConteudoFinal = request.Mensagem,
                Status = StatusComunicacaoEntrega.Pendente,
                ChaveDedupe = $"{request.ChaveEvento}:{responsavelPessoaId}",
                DataCriacao = DateTime.UtcNow
            });

            if (await _preferenciaService.EstaBloqueadoAsync(responsavelPessoaId, CanalComunicacao.Push))
            {
                entrega.Status = StatusComunicacaoEntrega.IgnoradoPorPreferencia;
                entrega.Erro = $"Entrega ignorada: pessoa {responsavelPessoaId} bloqueou o canal {CanalComunicacao.Push}.";
                await _entregaRepository.UpdateAsync(entrega);
                continue;
            }

            var sucesso = await _processamentoService.ProcessarEntregaAsync(entrega.Id, cancellationToken);
            if (sucesso)
            {
                total++;
            }
        }

        await RegistrarExecucaoAsync("ExecutarAvisoContextualKids", request.ChaveEvento, new
        {
            request.CriancaPessoaId,
            request.Tipo,
            Responsaveis = responsavelIds,
            campanha.Id,
            TotalProcessados = total
        });

        return total;
    }

    public async Task<PagedResultDto<ComunicacaoAutomacaoHistoricoItemDto>> GetHistoricoAsync(ComunicacaoAutomacaoHistoricoQueryDto query)
    {
        var auditQuery = new AuditLogPagedQueryDto
        {
            Page = query.Page,
            PageSize = query.PageSize,
            EntityName = "ComunicacaoAutomacaoEvento",
            Action = string.IsNullOrWhiteSpace(query.Gatilho) ? null : query.Gatilho.Trim(),
            EntityId = string.IsNullOrWhiteSpace(query.ChaveEvento) ? null : query.ChaveEvento.Trim()
        };

        var page = await _auditLogService.GetPagedAsync(auditQuery);
        return new PagedResultDto<ComunicacaoAutomacaoHistoricoItemDto>
        {
            Items = page.Items.Select(x => new ComunicacaoAutomacaoHistoricoItemDto
            {
                Id = x.Id,
                Gatilho = x.Action,
                ChaveEvento = x.EntityId,
                Acao = x.Action,
                ExecutadoEm = x.CreatedAt,
                PayloadJson = x.ChangesJson
            }).ToList(),
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    private async Task<ComunicacaoEntrega> CriarEntregaVisitanteAsync(ComunicacaoCampanha campanha, Visitante visitante, ConfiguracaoMensagem configuracao)
    {
        var nome = visitante.Pessoa?.Nome ?? $"Visitante {visitante.Id}";
        var whatsapp = visitante.Pessoa?.WhatsApp;

        if (await _preferenciaService.EstaBloqueadoAsync(visitante.PessoaId, CanalComunicacao.WhatsApp))
        {
            return new ComunicacaoEntrega
            {
                ComunicacaoCampanhaId = campanha.Id,
                DestinatarioPessoaId = visitante.PessoaId,
                DestinatarioVisitanteId = visitante.Id,
                Canal = CanalComunicacao.WhatsApp,
                DestinoResolvido = visitante.PessoaId > 0 ? $"pessoa:{visitante.PessoaId}" : $"visitante:{visitante.Id}",
                RemetenteResolvido = campanha.Nome,
                ConteudoFinal = campanha.Nome,
                Status = StatusComunicacaoEntrega.IgnoradoPorPreferencia,
                Erro = $"Entrega ignorada: {nome} bloqueou o canal {CanalComunicacao.WhatsApp}.",
                ChaveDedupe = $"{campanha.Id}:{CanalComunicacao.WhatsApp}:{visitante.PessoaId}:{visitante.Id}:preferencia",
                DataCriacao = DateTime.UtcNow
            };
        }

        if (string.IsNullOrWhiteSpace(whatsapp))
        {
            return new ComunicacaoEntrega
            {
                ComunicacaoCampanhaId = campanha.Id,
                DestinatarioPessoaId = visitante.PessoaId,
                DestinatarioVisitanteId = visitante.Id,
                Canal = CanalComunicacao.WhatsApp,
                DestinoResolvido = visitante.PessoaId > 0 ? $"pessoa:{visitante.PessoaId}" : $"visitante:{visitante.Id}",
                RemetenteResolvido = campanha.Nome,
                ConteudoFinal = campanha.Nome,
                Status = StatusComunicacaoEntrega.Falhou,
                Erro = $"Entrega bloqueada: {nome} não possui WhatsApp válido.",
                ChaveDedupe = $"{campanha.Id}:{CanalComunicacao.WhatsApp}:{visitante.PessoaId}:{visitante.Id}",
                DataCriacao = DateTime.UtcNow
            };
        }

        return new ComunicacaoEntrega
        {
            ComunicacaoCampanhaId = campanha.Id,
            DestinatarioPessoaId = visitante.PessoaId,
            DestinatarioVisitanteId = visitante.Id,
            Canal = CanalComunicacao.WhatsApp,
            DestinoResolvido = whatsapp,
            RemetenteResolvido = campanha.Nome,
            ConteudoFinal = RenderizarMensagem(configuracao.TextoMensagem, nome),
            Status = StatusComunicacaoEntrega.Pendente,
            ChaveDedupe = $"{campanha.Id}:{CanalComunicacao.WhatsApp}:{visitante.PessoaId}:{visitante.Id}",
            DataCriacao = DateTime.UtcNow
        };
    }

    // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)

    private static string RenderizarMensagem(string template, string nome)
    {
        return template.Replace("{Nome}", nome, StringComparison.OrdinalIgnoreCase).Trim();
    }

    private async Task<bool> JaExecutadoAsync(string acao, string chaveEvento)
    {
        var historico = await _auditLogService.GetPagedAsync(new AuditLogPagedQueryDto
        {
            Page = 1,
            PageSize = 1,
            EntityName = "ComunicacaoAutomacaoEvento",
            EntityId = chaveEvento,
            Action = acao
        });

        return historico.Total > 0;
    }

    private Task RegistrarExecucaoAsync(string acao, string chaveEvento, object payload)
    {
        return _auditLogService.RecordAsync("ComunicacaoAutomacaoEvento", chaveEvento, acao, payload);
    }
}
