using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoEntregaService
{
    Task<PagedResultDto<ComunicacaoEntregaResumoDto>> GetPagedAsync(ComunicacaoEntregaPagedQueryDto query);
    Task<IReadOnlyList<ComunicacaoEntregaResumoDto>> GetByCampanhaIdAsync(int campanhaId);
    Task<ComunicacaoEntregaResumoDto?> GetByIdAsync(int entregaId);
    Task<IReadOnlyList<ComunicacaoEntregaResumoDto>> ReservarPendentesAsync(int limit);
    Task MarcarComoEnviadaAsync(int entregaId, string? providerMessageId = null);
    Task MarcarComoFalhaAsync(int entregaId, string erro, string? errorCode = null);
    Task<ComunicacaoEntregaResumoDto> PrepararReprocessamentoAsync(int entregaId);
}

public class ComunicacaoEntregaService : IComunicacaoEntregaService
{
    private readonly IComunicacaoEntregaRepository _repository;
    private readonly IComunicacaoCampanhaRepository _campanhaRepository;
    private readonly ILogger<ComunicacaoEntregaService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly MessageSchedulerSettings _settings;

    public ComunicacaoEntregaService(
        IComunicacaoEntregaRepository repository,
        IComunicacaoCampanhaRepository campanhaRepository,
        ILogger<ComunicacaoEntregaService> logger,
        IAuditLogService auditLogService,
        IOptions<MessageSchedulerSettings> settings)
    {
        _repository = repository;
        _campanhaRepository = campanhaRepository;
        _logger = logger;
        _auditLogService = auditLogService;
        _settings = settings.Value;
    }

    public async Task<PagedResultDto<ComunicacaoEntregaResumoDto>> GetPagedAsync(ComunicacaoEntregaPagedQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);
        var (items, total) = await _repository.GetPagedAsync(query);

        return new PagedResultDto<ComunicacaoEntregaResumoDto>
        {
            Items = items.Select(MapResumo).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<ComunicacaoEntregaResumoDto>> GetByCampanhaIdAsync(int campanhaId)
    {
        var items = await _repository.GetByCampanhaIdAsync(campanhaId);
        return items.Select(MapResumo).ToList();
    }

    public async Task<ComunicacaoEntregaResumoDto?> GetByIdAsync(int entregaId)
    {
        var item = await _repository.GetByIdAsync(entregaId);
        return item == null ? null : MapResumo(item);
    }

    public async Task<IReadOnlyList<ComunicacaoEntregaResumoDto>> ReservarPendentesAsync(int limit)
    {
        var items = await _repository.ReservarPendentesAsync(limit);
        if (items.Count > 0)
        {
            _logger.LogInformation(
                "{EventName} Quantidade={Quantidade}",
                ComunicacaoObservability.Events.EntregaReservada,
                items.Count);
        }

        return items.Select(MapResumo).ToList();
    }

    public async Task MarcarComoEnviadaAsync(int entregaId, string? providerMessageId = null)
    {
        var entrega = await _repository.GetByIdAsync(entregaId) ?? throw new ArgumentException("Entrega não encontrada");
        entrega.Status = StatusComunicacaoEntrega.Enviado;
        // Enviado != Entregue: EntregueEm/LidoEm são preenchidos pelo webhook do provider.
        entrega.ProcessadoEm = DateTime.UtcNow;
        entrega.AtualizadoEm = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(providerMessageId))
        {
            entrega.ProviderMessageId = providerMessageId;
        }
        entrega.Tentativas += 1;
        await _repository.UpdateAsync(entrega);
        await AtualizarCampanhaAsync(entrega);

        _logger.LogInformation(
            "{EventName} EntregaId={EntregaId} Canal={Canal} ProviderMessageId={ProviderMessageId}",
            ComunicacaoObservability.Events.EntregaEnviada,
            entrega.Id,
            entrega.Canal,
            entrega.ProviderMessageId);
    }

    public async Task MarcarComoFalhaAsync(int entregaId, string erro, string? errorCode = null)
    {
        var entrega = await _repository.GetByIdAsync(entregaId) ?? throw new ArgumentException("Entrega não encontrada");
        entrega.Tentativas += 1;
        entrega.Erro = erro;
        entrega.ErrorCode = errorCode;
        entrega.ProcessadoEm = DateTime.UtcNow;
        entrega.AtualizadoEm = DateTime.UtcNow;

        if (entrega.Tentativas < _settings.MaxTentativas)
        {
            // Retentativa com backoff: volta para Pendente agendada no futuro.
            entrega.Status = StatusComunicacaoEntrega.Pendente;
            entrega.AgendadoPara = DateTime.UtcNow.AddMinutes(_settings.RetryBackoffMinutes * entrega.Tentativas);
            _logger.LogWarning(
                "EntregaId={EntregaId} falhou (tentativa {Tentativa}/{Max}); reagendada p/ {Agendado}. Erro={Erro}",
                entrega.Id, entrega.Tentativas, _settings.MaxTentativas, entrega.AgendadoPara, erro);
        }
        else
        {
            entrega.Status = StatusComunicacaoEntrega.Falhou;
            _logger.LogWarning(
                "{EventName} EntregaId={EntregaId} Canal={Canal} Erro={Erro} (esgotou tentativas)",
                ComunicacaoObservability.Events.EntregaFalhou, entrega.Id, entrega.Canal, erro);

            await _auditLogService.RecordAsync("ComunicacaoEntrega", entrega.Id.ToString(), "Falha", new
            {
                entrega.Canal,
                entrega.DestinoResolvido,
                entrega.Erro
            });
        }

        await _repository.UpdateAsync(entrega);
        await AtualizarCampanhaAsync(entrega);
    }

    public async Task<ComunicacaoEntregaResumoDto> PrepararReprocessamentoAsync(int entregaId)
    {
        var entrega = await _repository.GetByIdAsync(entregaId) ?? throw new ArgumentException("Entrega não encontrada");
        if (!PodeReprocessar(entrega))
        {
            throw new InvalidOperationException("Esta entrega não é elegível para reprocessamento.");
        }

        entrega.Status = StatusComunicacaoEntrega.Pendente;
        entrega.Erro = null;
        entrega.ProcessadoEm = null;
        entrega.EntregueEm = null;
        await _repository.UpdateAsync(entrega);
        await AtualizarCampanhaAsync(entrega);

        await _auditLogService.RecordAsync("ComunicacaoEntrega", entrega.Id.ToString(), "ReprocessarEntrega", new
        {
            entrega.Canal,
            entrega.DestinoResolvido,
            TentativasAnteriores = entrega.Tentativas
        });

        return MapResumo(entrega);
    }

    private static ComunicacaoEntregaResumoDto MapResumo(ComunicacaoEntrega entrega)
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
            MidiaUrl = entrega.MidiaUrl,
            PodeReprocessar = PodeReprocessar(entrega)
        };
    }

    private async Task AtualizarCampanhaAsync(ComunicacaoEntrega entrega)
    {
        if (entrega.ComunicacaoCampanhaId.HasValue)
        {
            await _campanhaRepository.AtualizarStatusPorEntregasAsync(entrega.ComunicacaoCampanhaId.Value);
        }
    }

    private static bool PodeReprocessar(ComunicacaoEntrega entrega)
    {
        if (entrega.Status != StatusComunicacaoEntrega.Falhou)
        {
            return false;
        }

        var erro = entrega.Erro ?? string.Empty;
        if (string.IsNullOrWhiteSpace(erro))
        {
            return true;
        }

        return !erro.StartsWith("Entrega bloqueada:", StringComparison.OrdinalIgnoreCase) &&
               !erro.StartsWith("Entrega ignorada:", StringComparison.OrdinalIgnoreCase);
    }
}
