using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IComunicacaoSegmentoService
{
    Task<IReadOnlyList<ComunicacaoSegmentoResumoDto>> GetAllAsync();
    Task<ComunicacaoSegmentoDetalheDto?> GetByIdAsync(int id);
    Task<ComunicacaoSegmentoDetalheDto> CreateAsync(CriarComunicacaoSegmentoDto dto);
    Task<ComunicacaoSegmentoDetalheDto> UpdateAsync(int id, AtualizarComunicacaoSegmentoDto dto);
    Task<ComunicacaoEstimativaAudienciaDto> GetEstimativaAsync(string? publicoAlvo = null, int? segmentoId = null);
}

public class ComunicacaoSegmentoService : IComunicacaoSegmentoService
{
    private readonly IComunicacaoSegmentoRepository _repository;
    private readonly IComunicacaoAudienceResolver _audienceResolver;

    public ComunicacaoSegmentoService(
        IComunicacaoSegmentoRepository repository,
        IComunicacaoAudienceResolver audienceResolver)
    {
        _repository = repository;
        _audienceResolver = audienceResolver;
    }

    public async Task<IReadOnlyList<ComunicacaoSegmentoResumoDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapResumo).ToList();
    }

    public async Task<ComunicacaoSegmentoDetalheDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapDetalhe(item);
    }

    public async Task<ComunicacaoSegmentoDetalheDto> CreateAsync(CriarComunicacaoSegmentoDto dto)
    {
        var entity = new ComunicacaoSegmento
        {
            Nome = dto.Nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim(),
            PublicoAlvo = dto.PublicoAlvo.Trim(),
            Ativo = true,
            Padrao = false,
            DataCriacao = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(entity);
        return MapDetalhe(created);
    }

    public async Task<ComunicacaoSegmentoDetalheDto> UpdateAsync(int id, AtualizarComunicacaoSegmentoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new ArgumentException("Segmento não encontrado");
        entity.Nome = dto.Nome.Trim();
        entity.Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim();
        entity.PublicoAlvo = dto.PublicoAlvo.Trim();
        entity.Ativo = dto.Ativo;
        entity.DataAtualizacao = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(entity);
        return MapDetalhe(updated);
    }

    public async Task<ComunicacaoEstimativaAudienciaDto> GetEstimativaAsync(string? publicoAlvo = null, int? segmentoId = null)
    {
        var publicoResolvido = await ResolverPublicoAlvoAsync(publicoAlvo, segmentoId);
        var destinatarios = await _audienceResolver.ResolveAsync(publicoResolvido);

        return new ComunicacaoEstimativaAudienciaDto
        {
            PublicoAlvo = publicoResolvido,
            TotalDestinatarios = destinatarios.Count,
            ComWhatsApp = destinatarios.Count(x => !string.IsNullOrWhiteSpace(x.WhatsApp)),
            ComEmail = destinatarios.Count(x => !string.IsNullOrWhiteSpace(x.Email)),
            ComPush = destinatarios.Count(x => x.PessoaId.HasValue),
            ComNotificacaoInterna = destinatarios.Count(x => x.PessoaId.HasValue)
        };
    }

    private async Task<string> ResolverPublicoAlvoAsync(string? publicoAlvo, int? segmentoId)
    {
        if (segmentoId.HasValue)
        {
            var segmento = await _repository.GetByIdAsync(segmentoId.Value) ?? throw new ArgumentException("Segmento não encontrado");
            return segmento.PublicoAlvo;
        }

        if (string.IsNullOrWhiteSpace(publicoAlvo))
        {
            throw new ArgumentException("Público alvo é obrigatório");
        }

        return publicoAlvo.Trim();
    }

    private static ComunicacaoSegmentoResumoDto MapResumo(ComunicacaoSegmento item)
    {
        return new ComunicacaoSegmentoResumoDto
        {
            Id = item.Id,
            Nome = item.Nome,
            Descricao = item.Descricao,
            PublicoAlvo = item.PublicoAlvo,
            Ativo = item.Ativo,
            Padrao = item.Padrao
        };
    }

    private static ComunicacaoSegmentoDetalheDto MapDetalhe(ComunicacaoSegmento item)
    {
        return new ComunicacaoSegmentoDetalheDto
        {
            Id = item.Id,
            Nome = item.Nome,
            Descricao = item.Descricao,
            PublicoAlvo = item.PublicoAlvo,
            Ativo = item.Ativo,
            Padrao = item.Padrao,
            DataCriacao = item.DataCriacao,
            DataAtualizacao = item.DataAtualizacao
        };
    }
}
