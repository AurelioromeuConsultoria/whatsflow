using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Direitos do titular previstos na LGPD (Art. 18): acesso/portabilidade dos dados
/// e eliminação por anonimização (direito ao esquecimento, preservando agregados).
/// </summary>
public interface IDadosPessoaisService
{
    /// <summary>Reúne os dados pessoais do titular para acesso/portabilidade. Null se não encontrado no tenant.</summary>
    Task<DadosPessoaisExportDto?> ExportarAsync(int pessoaId);

    /// <summary>
    /// Anonimiza os dados identificáveis do titular, preservando vínculos e agregados
    /// (financeiro, presença). Revoga consentimentos ativos e registra auditoria.
    /// Null se o titular não for encontrado no tenant.
    /// </summary>
    Task<AnonimizacaoResultadoDto?> AnonimizarAsync(int pessoaId);
}
