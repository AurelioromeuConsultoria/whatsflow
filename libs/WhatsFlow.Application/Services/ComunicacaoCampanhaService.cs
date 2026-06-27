using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoCampanhaService
{
    Task<PagedResultDto<ComunicacaoCampanhaResumoDto>> GetPagedAsync(ComunicacaoCampanhaPagedQueryDto query);
    Task<ComunicacaoStatsDto> GetStatsAsync();
    Task<ComunicacaoCampanhaDetalheDto?> GetByIdAsync(int id);
    Task<ComunicacaoCampanhaDetalheDto> CreateAsync(CriarComunicacaoCampanhaDto dto);
    Task<ComunicacaoCampanhaDetalheDto> UpdateAsync(int id, AtualizarComunicacaoCampanhaDto dto);
}

public class ComunicacaoCampanhaService : IComunicacaoCampanhaService
{
    private readonly IComunicacaoCampanhaRepository _repository;
    private readonly IComunicacaoEntregaRepository _entregaRepository;
    private readonly IComunicacaoTemplateRepository _templateRepository;
    private readonly IComunicacaoPreferenciaService _preferenciaService;
    private readonly IComunicacaoAudienceResolver _audienceResolver;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ComunicacaoCampanhaService> _logger;

    public ComunicacaoCampanhaService(
        IComunicacaoCampanhaRepository repository,
        IComunicacaoEntregaRepository entregaRepository,
        IComunicacaoTemplateRepository templateRepository,
        IComunicacaoPreferenciaService preferenciaService,
        IComunicacaoAudienceResolver audienceResolver,
        ICurrentUserContext currentUser,
        IAuditLogService auditLogService,
        ILogger<ComunicacaoCampanhaService> logger)
    {
        _repository = repository;
        _entregaRepository = entregaRepository;
        _templateRepository = templateRepository;
        _preferenciaService = preferenciaService;
        _audienceResolver = audienceResolver;
        _currentUser = currentUser;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<PagedResultDto<ComunicacaoCampanhaResumoDto>> GetPagedAsync(ComunicacaoCampanhaPagedQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);
        var (items, total) = await _repository.GetPagedAsync(query);

        return new PagedResultDto<ComunicacaoCampanhaResumoDto>
        {
            Items = items.Select(MapResumo).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<ComunicacaoStatsDto> GetStatsAsync()
    {
        return _repository.GetStatsAsync();
    }

    public async Task<ComunicacaoCampanhaDetalheDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : MapDetalhe(entity);
    }

    public async Task<ComunicacaoCampanhaDetalheDto> CreateAsync(CriarComunicacaoCampanhaDto dto)
    {
        var status = dto.DataAgendamento.HasValue ? StatusComunicacaoCampanha.Agendada : StatusComunicacaoCampanha.Rascunho;
        var entity = new ComunicacaoCampanha
        {
            Nome = dto.Nome.Trim(),
            Objetivo = dto.Objetivo.Trim(),
            PublicoAlvo = dto.PublicoAlvo.Trim(),
            DataAgendamento = dto.DataAgendamento,
            Status = status,
            Origem = TipoOrigemComunicacao.Manual,
            CriadoPorUsuarioId = _currentUser.UserId,
            DataCriacao = DateTime.UtcNow,
            Canais = dto.Canais.Select(c => new ComunicacaoCampanhaCanal
            {
                Canal = c.Canal,
                TemplateId = c.TemplateId,
                Prioridade = c.Prioridade
            }).ToList()
        };

        var created = await _repository.CreateAsync(entity);
        var entregas = await GerarEntregasAsync(created);
        if (entregas.Count > 0)
        {
            await _entregaRepository.CreateManyAsync(entregas);
        }
        _logger.LogInformation(
            "{EventName} CampanhaId={CampanhaId} UsuarioId={UsuarioId} Canais={QuantidadeCanais}",
            ComunicacaoObservability.Events.CampanhaCriada,
            created.Id,
            _currentUser.UserId,
            created.Canais.Count);
        await _auditLogService.RecordAsync("ComunicacaoCampanha", created.Id.ToString(), "CriarCampanha", new
        {
            created.Nome,
            created.Objetivo,
            created.PublicoAlvo,
            Canais = created.Canais.Select(c => c.Canal.ToString()).ToArray(),
            EntregasGeradas = entregas.Count
        });

        var reloaded = await _repository.GetByIdAsync(created.Id) ?? created;
        return MapDetalhe(reloaded);
    }

    public async Task<ComunicacaoCampanhaDetalheDto> UpdateAsync(int id, AtualizarComunicacaoCampanhaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new ArgumentException("Campanha não encontrada");
        entity.Nome = dto.Nome.Trim();
        entity.Objetivo = dto.Objetivo.Trim();
        entity.PublicoAlvo = dto.PublicoAlvo.Trim();
        entity.Status = dto.Status;
        entity.DataAgendamento = dto.DataAgendamento;
        entity.DataAtualizacao = DateTime.UtcNow;

        entity.Canais.Clear();
        foreach (var canal in dto.Canais)
        {
            entity.Canais.Add(new ComunicacaoCampanhaCanal
            {
                Canal = canal.Canal,
                TemplateId = canal.TemplateId,
                Prioridade = canal.Prioridade
            });
        }

        var updated = await _repository.UpdateAsync(entity);
        return MapDetalhe(updated);
    }

    private static ComunicacaoCampanhaResumoDto MapResumo(ComunicacaoCampanha campanha)
    {
        return new ComunicacaoCampanhaResumoDto
        {
            Id = campanha.Id,
            Nome = campanha.Nome,
            Objetivo = campanha.Objetivo,
            PublicoAlvo = campanha.PublicoAlvo,
            Status = CalcularStatusOperacional(campanha),
            DataAgendamento = campanha.DataAgendamento,
            DataCriacao = campanha.DataCriacao,
            TotalEntregas = campanha.Entregas.Count,
            TotalFalhas = campanha.Entregas.Count(e => e.Status == StatusComunicacaoEntrega.Falhou)
        };
    }

    private static ComunicacaoCampanhaDetalheDto MapDetalhe(ComunicacaoCampanha campanha)
    {
        return new ComunicacaoCampanhaDetalheDto
        {
            Id = campanha.Id,
            Nome = campanha.Nome,
            Objetivo = campanha.Objetivo,
            PublicoAlvo = campanha.PublicoAlvo,
            Status = CalcularStatusOperacional(campanha),
            DataAgendamento = campanha.DataAgendamento,
            DataCriacao = campanha.DataCriacao,
            TotalEntregas = campanha.Entregas.Count,
            TotalFalhas = campanha.Entregas.Count(e => e.Status == StatusComunicacaoEntrega.Falhou),
            Origem = campanha.Origem,
            CriadoPorUsuarioId = campanha.CriadoPorUsuarioId,
            Canais = campanha.Canais.Select(c => new ComunicacaoCampanhaCanalDto
            {
                Canal = c.Canal,
                TemplateId = c.TemplateId,
                NomeTemplate = c.Template?.Nome,
                Prioridade = c.Prioridade
            }).ToList(),
            UltimasEntregas = campanha.Entregas
                .OrderByDescending(e => e.DataCriacao)
                .Take(20)
                .Select(MapEntrega)
                .ToList()
        };
    }

    private static StatusComunicacaoCampanha CalcularStatusOperacional(ComunicacaoCampanha campanha)
    {
        var entregas = campanha.Entregas.ToList();
        if (campanha.Status == StatusComunicacaoCampanha.Cancelada)
        {
            return StatusComunicacaoCampanha.Cancelada;
        }

        if (entregas.Count == 0)
        {
            return campanha.DataAgendamento.HasValue
                ? StatusComunicacaoCampanha.Agendada
                : StatusComunicacaoCampanha.Rascunho;
        }

        if (entregas.Any(e => e.Status == StatusComunicacaoEntrega.Pendente || e.Status == StatusComunicacaoEntrega.Reservado))
        {
            return campanha.DataAgendamento.HasValue && campanha.DataAgendamento.Value > DateTime.Now
                ? StatusComunicacaoCampanha.Agendada
                : StatusComunicacaoCampanha.Processando;
        }

        return entregas.Any(e => e.Status == StatusComunicacaoEntrega.Falhou)
            ? StatusComunicacaoCampanha.ConcluidaComFalhas
            : StatusComunicacaoCampanha.Concluida;
    }

    private static ComunicacaoEntregaResumoDto MapEntrega(ComunicacaoEntrega entrega)
    {
        return new ComunicacaoEntregaResumoDto
        {
            Id = entrega.Id,
            Canal = entrega.Canal,
            DestinoResolvido = entrega.DestinoResolvido,
            Status = entrega.Status,
            Tentativas = entrega.Tentativas,
            ProcessadoEm = entrega.ProcessadoEm,
            EntregueEm = entrega.EntregueEm,
            Erro = entrega.Erro,
            MidiaUrl = entrega.MidiaUrl
        };
    }

    private async Task<List<ComunicacaoEntrega>> GerarEntregasAsync(ComunicacaoCampanha campanha)
    {
        var destinatarios = await _audienceResolver.ResolveAsync(campanha.PublicoAlvo);
        if (destinatarios.Count == 0)
        {
            return [];
        }

        var templatesById = new Dictionary<int, ComunicacaoTemplate>();
        foreach (var templateId in campanha.Canais.Where(c => c.TemplateId.HasValue).Select(c => c.TemplateId!.Value).Distinct())
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template != null)
            {
                templatesById[templateId] = template;
            }
        }

        var entregas = new List<ComunicacaoEntrega>();
        foreach (var destinatario in destinatarios)
        {
            foreach (var canal in campanha.Canais.OrderBy(c => c.Prioridade))
            {
                var template = canal.TemplateId.HasValue && templatesById.TryGetValue(canal.TemplateId.Value, out var encontrado)
                    ? encontrado
                    : null;

                var destino = ResolveDestino(canal.Canal, destinatario);
                var conteudoFinal = RenderConteudo(template?.Corpo, campanha, destinatario);
                var conteudoHtmlFinal = string.IsNullOrWhiteSpace(template?.CorpoHtml)
                    ? null
                    : RenderConteudo(template.CorpoHtml, campanha, destinatario);
                var assunto = canal.Canal == CanalComunicacao.Email
                    ? RenderConteudo(template?.Assunto, campanha, destinatario) ?? campanha.Nome
                    : campanha.Nome;

                if (await _preferenciaService.EstaBloqueadoAsync(destinatario.PessoaId, canal.Canal))
                {
                    entregas.Add(CriarEntregaIgnoradaPorPreferencia(campanha, canal.Canal, destinatario, assunto));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(destino))
                {
                    entregas.Add(CriarEntregaBloqueada(campanha, canal.Canal, destinatario, assunto));
                    continue;
                }

                entregas.Add(new ComunicacaoEntrega
                {
                    ComunicacaoCampanhaId = campanha.Id,
                    DestinatarioPessoaId = destinatario.PessoaId,
                    DestinatarioVisitanteId = destinatario.VisitanteId,
                    Canal = canal.Canal,
                    DestinoResolvido = destino,
                    RemetenteResolvido = assunto,
                    ConteudoFinal = string.IsNullOrWhiteSpace(conteudoFinal) ? campanha.Nome : conteudoFinal,
                    ConteudoHtmlFinal = conteudoHtmlFinal,
                    Status = StatusComunicacaoEntrega.Pendente,
                    ChaveDedupe = $"{campanha.Id}:{canal.Canal}:{destinatario.PessoaId ?? 0}:{destinatario.VisitanteId ?? 0}",
                    DataCriacao = DateTime.UtcNow
                });
            }
        }

        return entregas;
    }

    private static ComunicacaoEntrega CriarEntregaBloqueada(
        ComunicacaoCampanha campanha,
        CanalComunicacao canal,
        ComunicacaoDestinatario destinatario,
        string? assunto)
    {
        return new ComunicacaoEntrega
        {
            ComunicacaoCampanhaId = campanha.Id,
            DestinatarioPessoaId = destinatario.PessoaId,
            DestinatarioVisitanteId = destinatario.VisitanteId,
            Canal = canal,
            DestinoResolvido = ResolveDestinoFallback(canal, destinatario),
            RemetenteResolvido = assunto,
            ConteudoFinal = campanha.Nome,
            Status = StatusComunicacaoEntrega.Falhou,
            Erro = BuildDestinoInvalidoErro(canal, destinatario),
            ChaveDedupe = $"{campanha.Id}:{canal}:{destinatario.PessoaId ?? 0}:{destinatario.VisitanteId ?? 0}",
            DataCriacao = DateTime.UtcNow
        };
    }

    private static ComunicacaoEntrega CriarEntregaIgnoradaPorPreferencia(
        ComunicacaoCampanha campanha,
        CanalComunicacao canal,
        ComunicacaoDestinatario destinatario,
        string? assunto)
    {
        return new ComunicacaoEntrega
        {
            ComunicacaoCampanhaId = campanha.Id,
            DestinatarioPessoaId = destinatario.PessoaId,
            DestinatarioVisitanteId = destinatario.VisitanteId,
            Canal = canal,
            DestinoResolvido = ResolveDestinoFallback(canal, destinatario),
            RemetenteResolvido = assunto,
            ConteudoFinal = campanha.Nome,
            Status = StatusComunicacaoEntrega.IgnoradoPorPreferencia,
            Erro = BuildPreferenciaBloqueadaErro(canal, destinatario),
            ChaveDedupe = $"{campanha.Id}:{canal}:{destinatario.PessoaId ?? 0}:{destinatario.VisitanteId ?? 0}:preferencia",
            DataCriacao = DateTime.UtcNow
        };
    }

    private static string? ResolveDestino(CanalComunicacao canal, ComunicacaoDestinatario destinatario)
    {
        return canal switch
        {
            CanalComunicacao.WhatsApp => destinatario.WhatsApp,
            CanalComunicacao.Email => destinatario.Email,
            CanalComunicacao.Push => destinatario.PessoaId.HasValue ? $"pessoa:{destinatario.PessoaId.Value}" : null,
            CanalComunicacao.NotificacaoInterna => destinatario.PessoaId.HasValue ? $"pessoa:{destinatario.PessoaId.Value}" : null,
            _ => null
        };
    }

    private static string ResolveDestinoFallback(CanalComunicacao canal, ComunicacaoDestinatario destinatario)
    {
        if (destinatario.PessoaId.HasValue)
        {
            return $"pessoa:{destinatario.PessoaId.Value}";
        }

        if (destinatario.VisitanteId.HasValue)
        {
            return $"visitante:{destinatario.VisitanteId.Value}";
        }

        return $"{canal}:destino-nao-resolvido";
    }

    private static string BuildDestinoInvalidoErro(CanalComunicacao canal, ComunicacaoDestinatario destinatario)
    {
        var nome = string.IsNullOrWhiteSpace(destinatario.Nome) ? "destinatário" : destinatario.Nome;

        return canal switch
        {
            CanalComunicacao.WhatsApp => $"Entrega bloqueada: {nome} não possui WhatsApp válido.",
            CanalComunicacao.Email => $"Entrega bloqueada: {nome} não possui e-mail válido.",
            CanalComunicacao.Push => $"Entrega bloqueada: {nome} não possui vínculo de pessoa para push.",
            CanalComunicacao.NotificacaoInterna => $"Entrega bloqueada: {nome} não possui vínculo de pessoa para notificação interna.",
            _ => $"Entrega bloqueada: {nome} não possui destino válido para o canal {canal}."
        };
    }

    private static string BuildPreferenciaBloqueadaErro(CanalComunicacao canal, ComunicacaoDestinatario destinatario)
    {
        var nome = string.IsNullOrWhiteSpace(destinatario.Nome) ? "destinatário" : destinatario.Nome;
        return $"Entrega ignorada: {nome} bloqueou o canal {canal}.";
    }

    private static string? RenderConteudo(string? template, ComunicacaoCampanha campanha, ComunicacaoDestinatario destinatario)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        return template
            .Replace("{Nome}", destinatario.Nome, StringComparison.OrdinalIgnoreCase)
            .Replace("{PrimeiroNome}", destinatario.PrimeiroNome, StringComparison.OrdinalIgnoreCase)
            .Replace("{PublicoAlvo}", campanha.PublicoAlvo, StringComparison.OrdinalIgnoreCase)
            .Replace("{Campanha}", campanha.Nome, StringComparison.OrdinalIgnoreCase);
    }
}
