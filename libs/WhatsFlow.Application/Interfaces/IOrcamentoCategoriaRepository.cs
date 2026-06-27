using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IOrcamentoCategoriaRepository
{
    Task<IEnumerable<OrcamentoCategoria>> GetByAnoAsync(int ano);
    Task<OrcamentoCategoria?> GetByIdAsync(int id);
    Task<OrcamentoCategoria?> FindAsync(int ano, TipoOrcamento tipo, int? categoriaReceitaId, int? categoriaDespesaId);
    Task<OrcamentoCategoria> CreateAsync(OrcamentoCategoria entity);
    Task<OrcamentoCategoria> UpdateAsync(OrcamentoCategoria entity);
    Task DeleteAsync(int id);
}
