using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IConsentimentoRegistroRepository
{
    /// <summary>Adiciona o registro ao contexto sem salvar (uso dentro de transações).</summary>
    Task<ConsentimentoRegistro> CreateWithoutSaveAsync(ConsentimentoRegistro registro);

    /// <summary>Histórico de consentimentos de um titular, do mais recente para o mais antigo.</summary>
    Task<IEnumerable<ConsentimentoRegistro>> GetByPessoaAsync(int pessoaId);
}
