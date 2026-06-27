using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoPreferenciaService
{
    Task<IReadOnlyList<ComunicacaoPreferenciaResumoDto>> GetByPessoaIdAsync(int pessoaId);
    Task<ComunicacaoPreferenciaResumoDto> UpsertAsync(int pessoaId, CanalComunicacao canal, AtualizarComunicacaoPreferenciaDto dto);
    Task<bool> EstaBloqueadoAsync(int? pessoaId, CanalComunicacao canal);
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

    public async Task<IReadOnlyList<ComunicacaoPreferenciaResumoDto>> GetByPessoaIdAsync(int pessoaId)
    {
        var items = await _repository.GetByPessoaIdAsync(pessoaId);
        return items.Select(MapResumo).ToList();
    }

    public async Task<ComunicacaoPreferenciaResumoDto> UpsertAsync(int pessoaId, CanalComunicacao canal, AtualizarComunicacaoPreferenciaDto dto)
    {
        var entity = await _repository.GetByPessoaCanalAsync(pessoaId, canal);
        var statusAnterior = entity?.Status;

        if (entity == null)
        {
            entity = await _repository.CreateAsync(new ComunicacaoPreferencia
            {
                PessoaId = pessoaId,
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
            "{EventName} PessoaId={PessoaId} Canal={Canal} Status={Status}",
            ComunicacaoObservability.Events.PreferenciaAtualizada,
            pessoaId,
            canal,
            entity.Status);
        await _auditLogService.RecordAsync("ComunicacaoPreferencia", $"{pessoaId}:{canal}", "AtualizarPreferenciaCanal", new
        {
            PessoaId = pessoaId,
            Canal = canal.ToString(),
            StatusAnterior = statusAnterior?.ToString(),
            StatusAtual = entity.Status.ToString(),
            entity.OrigemConsentimento
        });

        return MapResumo(entity);
    }

    public async Task<bool> EstaBloqueadoAsync(int? pessoaId, CanalComunicacao canal)
    {
        if (!pessoaId.HasValue || pessoaId.Value <= 0)
        {
            return false;
        }

        var item = await _repository.GetByPessoaCanalAsync(pessoaId.Value, canal);
        return item?.Status == StatusPreferenciaCanal.Bloqueado;
    }

    private static ComunicacaoPreferenciaResumoDto MapResumo(ComunicacaoPreferencia item)
    {
        return new ComunicacaoPreferenciaResumoDto
        {
            Id = item.Id,
            PessoaId = item.PessoaId,
            Canal = item.Canal,
            Status = item.Status,
            OrigemConsentimento = item.OrigemConsentimento,
            DataCriacao = item.DataCriacao,
            DataAtualizacao = item.DataAtualizacao
        };
    }
}
