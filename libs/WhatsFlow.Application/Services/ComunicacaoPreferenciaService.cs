using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoPreferenciaService
{
    Task<IReadOnlyList<ComunicacaoPreferenciaResumoDto>> GetByContatoIdAsync(int contatoId);
    Task<ComunicacaoPreferenciaResumoDto> UpsertAsync(int contatoId, CanalComunicacao canal, AtualizarComunicacaoPreferenciaDto dto);
    Task<bool> EstaBloqueadoAsync(int? contatoId, CanalComunicacao canal);
}

public class ComunicacaoPreferenciaService : IComunicacaoPreferenciaService
{
    private readonly IComunicacaoPreferenciaRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ComunicacaoPreferenciaService> _logger;

    public ComunicacaoPreferenciaService(
        IComunicacaoPreferenciaRepository repository,
        IAuditLogService auditLogService,
        ILogger<ComunicacaoPreferenciaService> logger)
    {
        _repository = repository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ComunicacaoPreferenciaResumoDto>> GetByContatoIdAsync(int contatoId)
    {
        var items = await _repository.GetByContatoIdAsync(contatoId);
        return items.Select(MapResumo).ToList();
    }

    public async Task<ComunicacaoPreferenciaResumoDto> UpsertAsync(int contatoId, CanalComunicacao canal, AtualizarComunicacaoPreferenciaDto dto)
    {
        var entity = await _repository.GetByContatoCanalAsync(contatoId, canal);
        var statusAnterior = entity?.Status;

        if (entity == null)
        {
            entity = await _repository.CreateAsync(new ComunicacaoPreferencia
            {
                ContatoId = contatoId,
                Canal = canal,
                Status = dto.Status,
                OrigemConsentimento = string.IsNullOrWhiteSpace(dto.OrigemConsentimento) ? null : dto.OrigemConsentimento.Trim(),
                DataCriacao = DateTime.UtcNow
            });
        }
        else
        {
            entity.Status = dto.Status;
            entity.OrigemConsentimento = string.IsNullOrWhiteSpace(dto.OrigemConsentimento) ? null : dto.OrigemConsentimento.Trim();
            entity.DataAtualizacao = DateTime.UtcNow;
            entity = await _repository.UpdateAsync(entity);
        }

        _logger.LogInformation(
            "{EventName} ContatoId={ContatoId} Canal={Canal} Status={Status}",
            ComunicacaoObservability.Events.PreferenciaAtualizada,
            contatoId,
            canal,
            entity.Status);
        await _auditLogService.RecordAsync("ComunicacaoPreferencia", $"{contatoId}:{canal}", "AtualizarPreferenciaCanal", new
        {
            ContatoId = contatoId,
            Canal = canal.ToString(),
            StatusAnterior = statusAnterior?.ToString(),
            StatusAtual = entity.Status.ToString(),
            entity.OrigemConsentimento
        });

        return MapResumo(entity);
    }

    public async Task<bool> EstaBloqueadoAsync(int? contatoId, CanalComunicacao canal)
    {
        if (!contatoId.HasValue || contatoId.Value <= 0)
        {
            return false;
        }

        var item = await _repository.GetByContatoCanalAsync(contatoId.Value, canal);
        return item?.Status == StatusPreferenciaCanal.Bloqueado;
    }

    private static ComunicacaoPreferenciaResumoDto MapResumo(ComunicacaoPreferencia item)
    {
        return new ComunicacaoPreferenciaResumoDto
        {
            Id = item.Id,
            ContatoId = item.ContatoId,
            Canal = item.Canal,
            Status = item.Status,
            OrigemConsentimento = item.OrigemConsentimento,
            DataCriacao = item.DataCriacao,
            DataAtualizacao = item.DataAtualizacao
        };
    }
}
