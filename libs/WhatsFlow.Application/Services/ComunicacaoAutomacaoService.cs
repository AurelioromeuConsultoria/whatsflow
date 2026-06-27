using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Auditoria;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoAutomacaoService
{
    // TODO(WhatsFlow Etapa 4C): gatilho de boas-vindas para novo Contato (substitui o fluxo de novo-visitante).
    Task<ComunicacaoAutomacaoExecucaoResumoDto> ExecutarBoasVindasContatoAsync(int contatoId, CancellationToken cancellationToken = default);
    Task<int> ExecutarLembretesOperacionaisAsync(IEnumerable<ComunicacaoLembreteOperacionalRequest> lembretes, CancellationToken cancellationToken = default);
    Task<int> ExecutarAvisoContextualKidsAsync(ComunicacaoAvisoContextualKidsRequest request, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ComunicacaoAutomacaoHistoricoItemDto>> GetHistoricoAsync(ComunicacaoAutomacaoHistoricoQueryDto query);
}

public class ComunicacaoAutomacaoService : IComunicacaoAutomacaoService
{
    private readonly IContatoRepository _contatoRepository;
    private readonly IConfiguracaoMensagemRepository _configuracaoMensagemRepository;
    private readonly IComunicacaoCampanhaRepository _campanhaRepository;
    private readonly IComunicacaoEntregaRepository _entregaRepository;
    private readonly IComunicacaoPreferenciaService _preferenciaService;
    private readonly IComunicacaoProcessamentoService _processamentoService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ComunicacaoAutomacaoService> _logger;

    public ComunicacaoAutomacaoService(
        IContatoRepository contatoRepository,
        IConfiguracaoMensagemRepository configuracaoMensagemRepository,
        IComunicacaoCampanhaRepository campanhaRepository,
        IComunicacaoEntregaRepository entregaRepository,
        IComunicacaoPreferenciaService preferenciaService,
        IComunicacaoProcessamentoService processamentoService,
        IAuditLogService auditLogService,
        ILogger<ComunicacaoAutomacaoService> logger)
    {
        _contatoRepository = contatoRepository;
        _configuracaoMensagemRepository = configuracaoMensagemRepository;
        _campanhaRepository = campanhaRepository;
        _entregaRepository = entregaRepository;
        _preferenciaService = preferenciaService;
        _processamentoService = processamentoService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<ComunicacaoAutomacaoExecucaoResumoDto> ExecutarBoasVindasContatoAsync(int contatoId, CancellationToken cancellationToken = default)
    {
        var contato = await _contatoRepository.GetByIdAsync(contatoId)
            ?? throw new ArgumentException("Contato não encontrado");

        var configuracoes = (await _configuracaoMensagemRepository.GetAtivasAsync())
            .OrderBy(x => x.DiasAposVisita)
            .ThenBy(x => x.HorarioEnvio)
            .ToList();

        var resultado = new ComunicacaoAutomacaoExecucaoResumoDto
        {
            Gatilho = "boas-vindas-contato"
        };

        if (configuracoes.Count == 0)
        {
            resultado.TotalIgnoradas = 1;
            return resultado;
        }

        foreach (var configuracao in configuracoes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO(WhatsFlow Etapa 4C): a base de agendamento era a data da visita;
            // para Contato usamos a data de criação como aproximação até definir o gatilho real.
            var dataAgendamento = contato.CriadoEm.Date.AddDays(configuracao.DiasAposVisita) + configuracao.HorarioEnvio;
            var campanha = await _campanhaRepository.CreateAsync(new ComunicacaoCampanha
            {
                Nome = $"Automação boas-vindas D+{configuracao.DiasAposVisita} - {contato.Nome}",
                Objetivo = "onboarding-contato",
                PublicoAlvo = "contatos",
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

            await _entregaRepository.CreateAsync(await CriarEntregaContatoAsync(campanha, contato, configuracao));
            resultado.TotalCriadas++;
        }

        _logger.LogInformation(
            "{EventName} Gatilho=boas-vindas-contato ContatoId={ContatoId} Quantidade={Quantidade}",
            ComunicacaoObservability.Events.CampanhaCriada,
            contatoId,
            resultado.TotalCriadas);
        await _auditLogService.RecordAsync("ComunicacaoAutomacao", contatoId.ToString(), "ExecutarBoasVindasContato", new
        {
            ContatoId = contatoId,
            resultado.TotalCriadas
        });

        return resultado;
    }

    public async Task<int> ExecutarLembretesOperacionaisAsync(IEnumerable<ComunicacaoLembreteOperacionalRequest> lembretes, CancellationToken cancellationToken = default)
    {
        var total = 0;

        foreach (var lembrete in lembretes.Where(x => x.ContatoId > 0 && !string.IsNullOrWhiteSpace(x.ChaveEvento)))
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
                PublicoAlvo = "contatos",
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
                ContatoId = lembrete.ContatoId,
                Canal = CanalComunicacao.NotificacaoInterna,
                DestinoResolvido = $"contato:{lembrete.ContatoId}",
                RemetenteResolvido = lembrete.Titulo,
                ConteudoFinal = lembrete.Mensagem,
                Status = StatusComunicacaoEntrega.Pendente,
                ChaveDedupe = lembrete.ChaveEvento,
                DataCriacao = DateTime.UtcNow
            });

            if (await _preferenciaService.EstaBloqueadoAsync(lembrete.ContatoId, CanalComunicacao.NotificacaoInterna))
            {
                entrega.Status = StatusComunicacaoEntrega.IgnoradoPorPreferencia;
                entrega.Erro = $"Entrega ignorada: contato {lembrete.ContatoId} bloqueou o canal {CanalComunicacao.NotificacaoInterna}.";
                await _entregaRepository.UpdateAsync(entrega);
            }
            else
            {
                await _processamentoService.ProcessarEntregaAsync(entrega.Id, cancellationToken);
            }
            await RegistrarExecucaoAsync("ExecutarLembreteOperacional", lembrete.ChaveEvento, new
            {
                lembrete.ContatoId,
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

        var responsavelIds = request.ResponsavelContatoIds.Where(x => x > 0).Distinct().ToList();
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
        foreach (var responsavelContatoId in responsavelIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entrega = await _entregaRepository.CreateAsync(new ComunicacaoEntrega
            {
                ComunicacaoCampanhaId = campanha.Id,
                ContatoId = responsavelContatoId,
                Canal = CanalComunicacao.Push,
                DestinoResolvido = $"contato:{responsavelContatoId}",
                RemetenteResolvido = request.Titulo,
                ConteudoFinal = request.Mensagem,
                Status = StatusComunicacaoEntrega.Pendente,
                ChaveDedupe = $"{request.ChaveEvento}:{responsavelContatoId}",
                DataCriacao = DateTime.UtcNow
            });

            if (await _preferenciaService.EstaBloqueadoAsync(responsavelContatoId, CanalComunicacao.Push))
            {
                entrega.Status = StatusComunicacaoEntrega.IgnoradoPorPreferencia;
                entrega.Erro = $"Entrega ignorada: contato {responsavelContatoId} bloqueou o canal {CanalComunicacao.Push}.";
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
            request.CriancaContatoId,
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

    private async Task<ComunicacaoEntrega> CriarEntregaContatoAsync(ComunicacaoCampanha campanha, Contato contato, ConfiguracaoMensagem configuracao)
    {
        var nome = string.IsNullOrWhiteSpace(contato.Nome) ? $"Contato {contato.Id}" : contato.Nome;
        var whatsapp = contato.TelefoneWhatsApp;

        if (await _preferenciaService.EstaBloqueadoAsync(contato.Id, CanalComunicacao.WhatsApp))
        {
            return new ComunicacaoEntrega
            {
                ComunicacaoCampanhaId = campanha.Id,
                ContatoId = contato.Id,
                Canal = CanalComunicacao.WhatsApp,
                DestinoResolvido = $"contato:{contato.Id}",
                RemetenteResolvido = campanha.Nome,
                ConteudoFinal = campanha.Nome,
                Status = StatusComunicacaoEntrega.IgnoradoPorPreferencia,
                Erro = $"Entrega ignorada: {nome} bloqueou o canal {CanalComunicacao.WhatsApp}.",
                ChaveDedupe = $"{campanha.Id}:{CanalComunicacao.WhatsApp}:{contato.Id}:preferencia",
                DataCriacao = DateTime.UtcNow
            };
        }

        if (string.IsNullOrWhiteSpace(whatsapp))
        {
            return new ComunicacaoEntrega
            {
                ComunicacaoCampanhaId = campanha.Id,
                ContatoId = contato.Id,
                Canal = CanalComunicacao.WhatsApp,
                DestinoResolvido = $"contato:{contato.Id}",
                RemetenteResolvido = campanha.Nome,
                ConteudoFinal = campanha.Nome,
                Status = StatusComunicacaoEntrega.Falhou,
                Erro = $"Entrega bloqueada: {nome} não possui WhatsApp válido.",
                ChaveDedupe = $"{campanha.Id}:{CanalComunicacao.WhatsApp}:{contato.Id}",
                DataCriacao = DateTime.UtcNow
            };
        }

        return new ComunicacaoEntrega
        {
            ComunicacaoCampanhaId = campanha.Id,
            ContatoId = contato.Id,
            Canal = CanalComunicacao.WhatsApp,
            DestinoResolvido = whatsapp,
            RemetenteResolvido = campanha.Nome,
            ConteudoFinal = RenderizarMensagem(configuracao.TextoMensagem, nome),
            Status = StatusComunicacaoEntrega.Pendente,
            ChaveDedupe = $"{campanha.Id}:{CanalComunicacao.WhatsApp}:{contato.Id}",
            DataCriacao = DateTime.UtcNow
        };
    }

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
