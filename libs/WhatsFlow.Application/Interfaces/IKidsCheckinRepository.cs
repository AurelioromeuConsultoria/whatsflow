using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IKidsCheckinRepository
{
    Task<KidsCheckin?> GetByIdAsync(int id);
    Task<KidsCheckin?> GetCheckinAtivoPorCriancaAsync(int criancaPessoaId);
    Task<KidsCheckin?> GetByCodigoSessaoAsync(string codigoSessao);
    Task<KidsCheckin?> GetByTokenRetiradaAsync(string tokenRetirada);
    Task<KidsCheckin?> GetByPinRetiradaAsync(string pinRetirada);
    Task<IEnumerable<KidsCheckin>> GetByPeriodoAsync(DateTime dataInicioUtc, DateTime dataFimUtc);
    Task<IEnumerable<KidsCheckin>> GetHistoricoPorCriancaAsync(int criancaPessoaId, int? limit = null);
    Task<(IReadOnlyList<KidsCheckin> Items, int Total)> GetHistoricoPagedAsync(IEnumerable<int> criancaIds, int page, int pageSize);
    Task<IEnumerable<KidsCheckin>> GetCheckinsAtivosAsync();
    Task<KidsCheckin> CreateAsync(KidsCheckin checkin);
    Task<KidsCheckin> CreateWithoutSaveAsync(KidsCheckin checkin);
    Task<KidsCheckin> UpdateAsync(KidsCheckin checkin);
    Task UpdateWithoutSaveAsync(KidsCheckin checkin);
}

public interface IKidsPreCheckinRepository
{
    Task<KidsPreCheckin?> GetByIdAsync(int id);
    Task<KidsPreCheckin?> GetByQrTokenAsync(string qrToken);
    Task<KidsPreCheckin?> GetByCodigoCurtoAsync(string codigoCurto);
    Task<KidsPreCheckin?> GetAtivoPorCriancaESessaoAsync(int criancaPessoaId, int? eventoOcorrenciaId);
    Task<IEnumerable<KidsPreCheckin>> GetByResponsavelIdAsync(int responsavelPessoaId, string? status = null, bool somenteAtivos = false);
    Task<IEnumerable<KidsPreCheckin>> GetPendentesAsync(int? eventoOcorrenciaId = null, string? salaId = null, string? turmaId = null);
    Task<IEnumerable<KidsPreCheckin>> GetExpiradosPendentesAsync(DateTime referenciaUtc);
    Task<KidsPreCheckin> CreateAsync(KidsPreCheckin preCheckin);
    Task<KidsPreCheckin> CreateWithoutSaveAsync(KidsPreCheckin preCheckin);
    Task<KidsPreCheckin> UpdateAsync(KidsPreCheckin preCheckin);
    Task UpdateWithoutSaveAsync(KidsPreCheckin preCheckin);
}
