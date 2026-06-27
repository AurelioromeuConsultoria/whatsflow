using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IPatrimonioItemService
{
    Task<IEnumerable<PatrimonioItemDto>> GetAllAsync();
    Task<PatrimonioItemDto?> GetByIdAsync(int id);
    Task<PatrimonioItemDto> CreateAsync(CriarPatrimonioItemDto dto);
    Task<PatrimonioItemDto> UpdateAsync(int id, AtualizarPatrimonioItemDto dto);
    Task DeleteAsync(int id);
}

public class PatrimonioItemService : IPatrimonioItemService
{
    private readonly IPatrimonioItemRepository _repository;
    private readonly ICategoriaPatrimonioRepository _categoriaRepository;
    private readonly IPatrimonioMovimentacaoService _movimentacaoService;

    public PatrimonioItemService(
        IPatrimonioItemRepository repository,
        ICategoriaPatrimonioRepository categoriaRepository,
        IPatrimonioMovimentacaoService movimentacaoService)
    {
        _repository = repository;
        _categoriaRepository = categoriaRepository;
        _movimentacaoService = movimentacaoService;
    }

    public async Task<IEnumerable<PatrimonioItemDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<PatrimonioItemDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<PatrimonioItemDto> CreateAsync(CriarPatrimonioItemDto dto)
    {
        await ValidarAsync(dto.Codigo, dto.CategoriaPatrimonioId);

        var entity = new PatrimonioItem
        {
            Codigo = dto.Codigo.Trim(),
            Nome = dto.Nome.Trim(),
            Descricao = dto.Descricao?.Trim(),
            CategoriaPatrimonioId = dto.CategoriaPatrimonioId,
            Marca = dto.Marca?.Trim(),
            Modelo = dto.Modelo?.Trim(),
            NumeroSerie = dto.NumeroSerie?.Trim(),
            Quantidade = dto.Quantidade,
            Campus = dto.Campus?.Trim(),
            Localizacao = dto.Localizacao?.Trim(),
            MinisterioArea = dto.MinisterioArea?.Trim(),
            ResponsavelPessoaId = dto.ResponsavelPessoaId,
            TipoAquisicao = string.IsNullOrWhiteSpace(dto.TipoAquisicao) ? "Comprado" : dto.TipoAquisicao.Trim(),
            DataAquisicao = dto.DataAquisicao,
            ValorAquisicao = dto.ValorAquisicao,
            FornecedorId = dto.FornecedorId,
            NumeroNotaFiscal = dto.NumeroNotaFiscal?.Trim(),
            DespesaId = dto.DespesaId,
            CentroCustoId = dto.CentroCustoId,
            ProjetoId = dto.ProjetoId,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "EmUso" : dto.Status.Trim(),
            EstadoConservacao = string.IsNullOrWhiteSpace(dto.EstadoConservacao) ? "Bom" : dto.EstadoConservacao.Trim(),
            DataUltimaAvaliacao = dto.DataUltimaAvaliacao,
            PossuiGarantia = dto.PossuiGarantia,
            GarantiaAte = dto.GarantiaAte,
            DataUltimaManutencao = dto.DataUltimaManutencao,
            DataProximaManutencao = dto.DataProximaManutencao,
            FotoUrl = dto.FotoUrl?.Trim(),
            DocumentoUrl = dto.DocumentoUrl?.Trim(),
            Observacoes = dto.Observacoes?.Trim(),
            Ativo = true,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        var createdLoaded = await _repository.GetByIdAsync(created.Id) ?? created;
        await _movimentacaoService.RegistrarCadastroInicialAsync(createdLoaded);
        return MapToDto(createdLoaded);
    }

    public async Task<PatrimonioItemDto> UpdateAsync(int id, AtualizarPatrimonioItemDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Item patrimonial não encontrado");

        await ValidarAsync(dto.Codigo, dto.CategoriaPatrimonioId, id);

        entity.Codigo = dto.Codigo.Trim();
        entity.Nome = dto.Nome.Trim();
        entity.Descricao = dto.Descricao?.Trim();
        entity.CategoriaPatrimonioId = dto.CategoriaPatrimonioId;
        entity.Marca = dto.Marca?.Trim();
        entity.Modelo = dto.Modelo?.Trim();
        entity.NumeroSerie = dto.NumeroSerie?.Trim();
        entity.Quantidade = dto.Quantidade;
        entity.Campus = dto.Campus?.Trim();
        entity.Localizacao = dto.Localizacao?.Trim();
        entity.MinisterioArea = dto.MinisterioArea?.Trim();
        entity.ResponsavelPessoaId = dto.ResponsavelPessoaId;
        entity.TipoAquisicao = string.IsNullOrWhiteSpace(dto.TipoAquisicao) ? "Comprado" : dto.TipoAquisicao.Trim();
        entity.DataAquisicao = dto.DataAquisicao;
        entity.ValorAquisicao = dto.ValorAquisicao;
        entity.FornecedorId = dto.FornecedorId;
        entity.NumeroNotaFiscal = dto.NumeroNotaFiscal?.Trim();
        entity.DespesaId = dto.DespesaId;
        entity.CentroCustoId = dto.CentroCustoId;
        entity.ProjetoId = dto.ProjetoId;
        entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? "EmUso" : dto.Status.Trim();
        entity.EstadoConservacao = string.IsNullOrWhiteSpace(dto.EstadoConservacao) ? "Bom" : dto.EstadoConservacao.Trim();
        entity.DataUltimaAvaliacao = dto.DataUltimaAvaliacao;
        entity.PossuiGarantia = dto.PossuiGarantia;
        entity.GarantiaAte = dto.GarantiaAte;
        entity.DataUltimaManutencao = dto.DataUltimaManutencao;
        entity.DataProximaManutencao = dto.DataProximaManutencao;
        entity.FotoUrl = dto.FotoUrl?.Trim();
        entity.DocumentoUrl = dto.DocumentoUrl?.Trim();
        entity.Observacoes = dto.Observacoes?.Trim();
        entity.Ativo = dto.Ativo;

        var updated = await _repository.UpdateAsync(entity);
        var updatedLoaded = await _repository.GetByIdAsync(updated.Id) ?? updated;
        return MapToDto(updatedLoaded);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    private async Task ValidarAsync(string codigo, int categoriaId, int? idAtual = null)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new InvalidOperationException("Código é obrigatório");
        }

        var categoria = await _categoriaRepository.GetByIdAsync(categoriaId);
        if (categoria == null)
        {
            throw new InvalidOperationException("Categoria de patrimônio não encontrada");
        }

        var existente = await _repository.GetByCodigoAsync(codigo.Trim());
        if (existente != null && existente.Id != idAtual)
        {
            throw new InvalidOperationException("Já existe um item patrimonial com este código");
        }
    }

    private static PatrimonioItemDto MapToDto(PatrimonioItem item)
    {
        return new PatrimonioItemDto
        {
            Id = item.Id,
            Codigo = item.Codigo,
            Nome = item.Nome,
            Descricao = item.Descricao,
            CategoriaPatrimonioId = item.CategoriaPatrimonioId,
            CategoriaNome = item.CategoriaPatrimonio?.Nome ?? string.Empty,
            Marca = item.Marca,
            Modelo = item.Modelo,
            NumeroSerie = item.NumeroSerie,
            Quantidade = item.Quantidade,
            Campus = item.Campus,
            Localizacao = item.Localizacao,
            MinisterioArea = item.MinisterioArea,
            ResponsavelPessoaId = item.ResponsavelPessoaId,
            ResponsavelNome = item.ResponsavelPessoa?.Nome,
            TipoAquisicao = item.TipoAquisicao,
            DataAquisicao = item.DataAquisicao,
            ValorAquisicao = item.ValorAquisicao,
            FornecedorId = item.FornecedorId,
            FornecedorNome = item.Fornecedor?.Nome,
            NumeroNotaFiscal = item.NumeroNotaFiscal,
            DespesaId = item.DespesaId,
            CentroCustoId = item.CentroCustoId,
            CentroCustoNome = item.CentroCusto?.Nome,
            ProjetoId = item.ProjetoId,
            ProjetoNome = item.Projeto?.Nome,
            Status = item.Status,
            EstadoConservacao = item.EstadoConservacao,
            DataUltimaAvaliacao = item.DataUltimaAvaliacao,
            PossuiGarantia = item.PossuiGarantia,
            GarantiaAte = item.GarantiaAte,
            DataUltimaManutencao = item.DataUltimaManutencao,
            DataProximaManutencao = item.DataProximaManutencao,
            FotoUrl = item.FotoUrl,
            DocumentoUrl = item.DocumentoUrl,
            Observacoes = item.Observacoes,
            Ativo = item.Ativo,
            DataCriacao = item.DataCriacao,
        };
    }
}
