using WhatsFlow.Application.DTOs;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Gestão das requisições de titulares (LGPD): registro, acompanhamento do prazo
/// legal (15 dias) e desfecho (conclusão/recusa), com trilha de conformidade.
/// </summary>
public interface ISolicitacaoTitularService
{
    Task<SolicitacaoTitularDto> CriarAsync(CriarSolicitacaoTitularDto dto);
    Task<IEnumerable<SolicitacaoTitularDto>> ListarAsync(StatusSolicitacaoTitular? status = null);
    Task<SolicitacaoTitularDto?> ObterAsync(int id);
    Task<SolicitacaoTitularDto?> AtenderAsync(int id);
    Task<SolicitacaoTitularDto?> ConcluirAsync(int id, string? observacao);
    Task<SolicitacaoTitularDto?> RecusarAsync(int id, string motivo);
}
