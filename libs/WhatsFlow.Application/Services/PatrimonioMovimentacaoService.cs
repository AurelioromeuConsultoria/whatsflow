using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IPatrimonioMovimentacaoService
{
    Task<IEnumerable<PatrimonioMovimentacaoDto>> GetByPatrimonioIdAsync(int patrimonioItemId);
    Task<PatrimonioMovimentacaoDto> CreateAsync(int patrimonioItemId, CriarPatrimonioMovimentacaoDto dto);
    Task RegistrarCadastroInicialAsync(PatrimonioItem item);
}

public class PatrimonioMovimentacaoService : IPatrimonioMovimentacaoService
{
    private readonly IPatrimonioMovimentacaoRepository _repository;
    private readonly IPatrimonioItemRepository _patrimonioRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public PatrimonioMovimentacaoService(
        IPatrimonioMovimentacaoRepository repository,
        IPatrimonioItemRepository patrimonioRepository,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _patrimonioRepository = patrimonioRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<IEnumerable<PatrimonioMovimentacaoDto>> GetByPatrimonioIdAsync(int patrimonioItemId)
    {
        var items = await _repository.GetByPatrimonioIdAsync(patrimonioItemId);
        return items.Select(MapToDto);
    }

    public async Task<PatrimonioMovimentacaoDto> CreateAsync(int patrimonioItemId, CriarPatrimonioMovimentacaoDto dto)
    {
        var patrimonio = await _patrimonioRepository.GetByIdAsync(patrimonioItemId);
        if (patrimonio == null) throw new ArgumentException("Item patrimonial não encontrado");

        var tipoMovimentacao = dto.TipoMovimentacao.Trim();
        var entity = new PatrimonioMovimentacao
        {
            PatrimonioItemId = patrimonioItemId,
            TipoMovimentacao = tipoMovimentacao,
            DataMovimentacao = dto.DataMovimentacao ?? DateTime.Now,
            Origem = dto.Origem?.Trim(),
            Destino = dto.Destino?.Trim(),
            ResponsavelOrigem = dto.ResponsavelOrigem?.Trim(),
            ResponsavelDestino = dto.ResponsavelDestino?.Trim(),
            Observacoes = dto.Observacoes?.Trim(),
            UsuarioId = _currentUserContext.UserId,
            UsuarioNome = _currentUserContext.UserName ?? _currentUserContext.UserEmail,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        await AplicarEfeitosNoPatrimonioAsync(patrimonio, entity);
        return MapToDto(created);
    }

    public async Task RegistrarCadastroInicialAsync(PatrimonioItem item)
    {
        var entity = new PatrimonioMovimentacao
        {
            PatrimonioItemId = item.Id,
            TipoMovimentacao = "CadastroInicial",
            DataMovimentacao = item.DataCriacao,
            Destino = item.Localizacao,
            ResponsavelDestino = item.ResponsavelPessoa?.Nome,
            Observacoes = "Cadastro inicial do bem patrimonial",
            UsuarioId = _currentUserContext.UserId,
            UsuarioNome = _currentUserContext.UserName ?? _currentUserContext.UserEmail,
            DataCriacao = DateTime.Now,
        };

        await _repository.CreateAsync(entity);
    }

    private async Task AplicarEfeitosNoPatrimonioAsync(PatrimonioItem patrimonio, PatrimonioMovimentacao movimentacao)
    {
        switch (movimentacao.TipoMovimentacao)
        {
            case "TransferenciaLocal":
                if (!string.IsNullOrWhiteSpace(movimentacao.Destino))
                {
                    patrimonio.Localizacao = movimentacao.Destino;
                }
                break;

            case "ManutencaoEnvio":
                patrimonio.Status = "EmManutencao";
                if (!string.IsNullOrWhiteSpace(movimentacao.Destino))
                {
                    patrimonio.Localizacao = movimentacao.Destino;
                }
                break;

            case "ManutencaoRetorno":
                patrimonio.Status = "EmUso";
                patrimonio.DataUltimaManutencao = movimentacao.DataMovimentacao;
                if (!string.IsNullOrWhiteSpace(movimentacao.Destino))
                {
                    patrimonio.Localizacao = movimentacao.Destino;
                }
                break;

            case "Emprestimo":
                patrimonio.Status = "Emprestado";
                if (!string.IsNullOrWhiteSpace(movimentacao.Destino))
                {
                    patrimonio.Localizacao = movimentacao.Destino;
                }
                break;

            case "Devolucao":
                patrimonio.Status = "EmUso";
                if (!string.IsNullOrWhiteSpace(movimentacao.Destino))
                {
                    patrimonio.Localizacao = movimentacao.Destino;
                }
                break;

            case "Baixa":
                patrimonio.Status = "Baixado";
                if (!string.IsNullOrWhiteSpace(movimentacao.Destino))
                {
                    patrimonio.Localizacao = movimentacao.Destino;
                }
                break;
        }

        await _patrimonioRepository.UpdateAsync(patrimonio);
    }

    private static PatrimonioMovimentacaoDto MapToDto(PatrimonioMovimentacao item)
    {
        return new PatrimonioMovimentacaoDto
        {
            Id = item.Id,
            PatrimonioItemId = item.PatrimonioItemId,
            TipoMovimentacao = item.TipoMovimentacao,
            DataMovimentacao = item.DataMovimentacao,
            Origem = item.Origem,
            Destino = item.Destino,
            ResponsavelOrigem = item.ResponsavelOrigem,
            ResponsavelDestino = item.ResponsavelDestino,
            Observacoes = item.Observacoes,
            UsuarioId = item.UsuarioId,
            UsuarioNome = item.UsuarioNome,
            DataCriacao = item.DataCriacao,
        };
    }
}
