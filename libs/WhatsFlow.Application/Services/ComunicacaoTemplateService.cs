using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoTemplateService
{
    Task<IReadOnlyList<ComunicacaoTemplateResumoDto>> GetAllAsync();
    Task<ComunicacaoTemplateDetalheDto?> GetByIdAsync(int id);
    Task<ComunicacaoTemplateDetalheDto> CreateAsync(CriarComunicacaoTemplateDto dto);
    Task<ComunicacaoTemplateDetalheDto> UpdateAsync(int id, AtualizarComunicacaoTemplateDto dto);
}

public class ComunicacaoTemplateService : IComunicacaoTemplateService
{
    private readonly IComunicacaoTemplateRepository _repository;

    public ComunicacaoTemplateService(IComunicacaoTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ComunicacaoTemplateResumoDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapResumo).ToList();
    }

    public async Task<ComunicacaoTemplateDetalheDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : MapDetalhe(entity);
    }

    public async Task<ComunicacaoTemplateDetalheDto> CreateAsync(CriarComunicacaoTemplateDto dto)
    {
        var entity = new ComunicacaoTemplate
        {
            Nome = dto.Nome.Trim(),
            Objetivo = dto.Objetivo.Trim(),
            Canal = dto.Canal,
            Assunto = dto.Assunto?.Trim(),
            Corpo = dto.Corpo.Trim(),
            CorpoHtml = dto.CorpoHtml?.Trim(),
            VariaveisPermitidas = dto.VariaveisPermitidas.Trim(),
            Status = StatusComunicacaoTemplate.Rascunho,
            DataCriacao = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(entity);
        return MapDetalhe(created);
    }

    public async Task<ComunicacaoTemplateDetalheDto> UpdateAsync(int id, AtualizarComunicacaoTemplateDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new ArgumentException("Template não encontrado");
        entity.Nome = dto.Nome.Trim();
        entity.Objetivo = dto.Objetivo.Trim();
        entity.Assunto = dto.Assunto?.Trim();
        entity.Corpo = dto.Corpo.Trim();
        entity.CorpoHtml = dto.CorpoHtml?.Trim();
        entity.VariaveisPermitidas = dto.VariaveisPermitidas.Trim();
        entity.Status = dto.Status;
        entity.DataAtualizacao = DateTime.UtcNow;
        entity.Versao += 1;

        var updated = await _repository.UpdateAsync(entity);
        return MapDetalhe(updated);
    }

    private static ComunicacaoTemplateResumoDto MapResumo(ComunicacaoTemplate template)
    {
        return new ComunicacaoTemplateResumoDto
        {
            Id = template.Id,
            Nome = template.Nome,
            Objetivo = template.Objetivo,
            Canal = template.Canal,
            Status = template.Status,
            Versao = template.Versao
        };
    }

    private static ComunicacaoTemplateDetalheDto MapDetalhe(ComunicacaoTemplate template)
    {
        return new ComunicacaoTemplateDetalheDto
        {
            Id = template.Id,
            Nome = template.Nome,
            Objetivo = template.Objetivo,
            Canal = template.Canal,
            Status = template.Status,
            Versao = template.Versao,
            Assunto = template.Assunto,
            Corpo = template.Corpo,
            CorpoHtml = template.CorpoHtml,
            VariaveisPermitidas = template.VariaveisPermitidas,
            DataCriacao = template.DataCriacao,
            DataAtualizacao = template.DataAtualizacao
        };
    }
}
