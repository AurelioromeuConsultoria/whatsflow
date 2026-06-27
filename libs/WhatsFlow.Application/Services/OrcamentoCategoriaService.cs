using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IOrcamentoCategoriaService
{
    Task<IEnumerable<OrcamentoCategoriaDto>> GetByAnoAsync(int ano);
    Task<OrcamentoCategoriaDto> SaveAsync(SalvarOrcamentoCategoriaDto dto);
    Task DeleteAsync(int id);
    Task<OrcamentoComparacaoDto> GetComparacaoAsync(int ano);
}

public class OrcamentoCategoriaService : IOrcamentoCategoriaService
{
    private readonly IOrcamentoCategoriaRepository _repository;
    private readonly IReceitaRepository _receitaRepository;
    private readonly IDespesaRepository _despesaRepository;

    public OrcamentoCategoriaService(
        IOrcamentoCategoriaRepository repository,
        IReceitaRepository receitaRepository,
        IDespesaRepository despesaRepository)
    {
        _repository = repository;
        _receitaRepository = receitaRepository;
        _despesaRepository = despesaRepository;
    }

    public async Task<IEnumerable<OrcamentoCategoriaDto>> GetByAnoAsync(int ano)
    {
        var items = await _repository.GetByAnoAsync(ano);
        return items.Select(MapToDto);
    }

    public async Task<OrcamentoCategoriaDto> SaveAsync(SalvarOrcamentoCategoriaDto dto)
    {
        var existing = await _repository.FindAsync(dto.Ano, dto.Tipo, dto.CategoriaReceitaId, dto.CategoriaDespesaId);

        if (existing != null)
        {
            existing.ValorOrcado = dto.ValorOrcado;
            var updated = await _repository.UpdateAsync(existing);
            var updatedWithRel = await _repository.GetByIdAsync(updated.Id);
            return MapToDto(updatedWithRel!);
        }

        var entity = new OrcamentoCategoria
        {
            Ano = dto.Ano,
            Tipo = dto.Tipo,
            CategoriaReceitaId = dto.CategoriaReceitaId,
            CategoriaDespesaId = dto.CategoriaDespesaId,
            ValorOrcado = dto.ValorOrcado,
        };

        var created = await _repository.CreateAsync(entity);
        var createdWithRel = await _repository.GetByIdAsync(created.Id);
        return MapToDto(createdWithRel!);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<OrcamentoComparacaoDto> GetComparacaoAsync(int ano)
    {
        var dataInicio = new DateTime(ano, 1, 1);
        var dataFim = new DateTime(ano, 12, 31);

        var orcamentos = (await _repository.GetByAnoAsync(ano)).ToList();
        var receitas = (await _receitaRepository.GetContribuicoesNoPeriodoAsync(dataInicio, dataFim)).ToList();
        var despesas = (await _despesaRepository.GetPorPeriodoAsync(dataInicio, dataFim)).ToList();

        var receitasGrupadas = receitas
            .Where(r => r.Status != StatusReceita.Cancelada)
            .GroupBy(r => r.CategoriaReceitaId)
            .ToDictionary(g => (int?)g.Key, g => new { Total = g.Sum(r => r.Valor), Nome = g.First().CategoriaReceita?.Nome ?? "Sem categoria" });

        var despesasGrupadas = despesas
            .GroupBy(d => d.CategoriaDespesaId)
            .ToDictionary(g => (int?)g.Key, g => new { Total = g.Sum(d => d.Valor), Nome = g.First().CategoriaDespesa?.Nome ?? "Sem categoria" });

        var itensReceita = new List<OrcamentoComparacaoItemDto>();
        var itensDespesa = new List<OrcamentoComparacaoItemDto>();

        // Itens com orçamento
        foreach (var o in orcamentos)
        {
            if (o.Tipo == TipoOrcamento.Receita)
            {
                var realizado = receitasGrupadas.TryGetValue(o.CategoriaReceitaId, out var r) ? r.Total : 0;
                itensReceita.Add(new OrcamentoComparacaoItemDto
                {
                    CategoriaId = o.CategoriaReceitaId,
                    CategoriaNome = o.CategoriaReceita?.Nome ?? "Sem categoria",
                    Tipo = TipoOrcamento.Receita,
                    ValorOrcado = o.ValorOrcado,
                    ValorRealizado = realizado,
                });
                receitasGrupadas.Remove(o.CategoriaReceitaId);
            }
            else
            {
                var realizado = despesasGrupadas.TryGetValue(o.CategoriaDespesaId, out var d) ? d.Total : 0;
                itensDespesa.Add(new OrcamentoComparacaoItemDto
                {
                    CategoriaId = o.CategoriaDespesaId,
                    CategoriaNome = o.CategoriaDespesa?.Nome ?? "Sem categoria",
                    Tipo = TipoOrcamento.Despesa,
                    ValorOrcado = o.ValorOrcado,
                    ValorRealizado = realizado,
                });
                despesasGrupadas.Remove(o.CategoriaDespesaId);
            }
        }

        // Realizados sem orçamento
        foreach (var kv in receitasGrupadas)
            itensReceita.Add(new OrcamentoComparacaoItemDto { CategoriaId = kv.Key, CategoriaNome = kv.Value.Nome, Tipo = TipoOrcamento.Receita, ValorOrcado = 0, ValorRealizado = kv.Value.Total });

        foreach (var kv in despesasGrupadas)
            itensDespesa.Add(new OrcamentoComparacaoItemDto { CategoriaId = kv.Key, CategoriaNome = kv.Value.Nome, Tipo = TipoOrcamento.Despesa, ValorOrcado = 0, ValorRealizado = kv.Value.Total });

        return new OrcamentoComparacaoDto
        {
            Ano = ano,
            TotalOrcadoReceitas = itensReceita.Sum(i => i.ValorOrcado),
            TotalRealizadoReceitas = itensReceita.Sum(i => i.ValorRealizado),
            TotalOrcadoDespesas = itensDespesa.Sum(i => i.ValorOrcado),
            TotalRealizadoDespesas = itensDespesa.Sum(i => i.ValorRealizado),
            Receitas = itensReceita.OrderBy(i => i.CategoriaNome).ToList(),
            Despesas = itensDespesa.OrderBy(i => i.CategoriaNome).ToList(),
        };
    }

    private static OrcamentoCategoriaDto MapToDto(OrcamentoCategoria o) => new()
    {
        Id = o.Id,
        Ano = o.Ano,
        Tipo = o.Tipo,
        CategoriaReceitaId = o.CategoriaReceitaId,
        CategoriaReceitaNome = o.CategoriaReceita?.Nome,
        CategoriaDespesaId = o.CategoriaDespesaId,
        CategoriaDespesaNome = o.CategoriaDespesa?.Nome,
        CategoriaNome = o.CategoriaReceita?.Nome ?? o.CategoriaDespesa?.Nome ?? "Sem categoria",
        ValorOrcado = o.ValorOrcado,
    };
}
