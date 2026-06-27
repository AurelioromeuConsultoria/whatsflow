using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Interfaces;

public interface IKidsDeviceTokenRepository
{
    Task UpsertAsync(int pessoaId, string fcmToken, string platform);
    Task<IEnumerable<string>> GetTokensByPessoaIdsAsync(IEnumerable<int> pessoaIds);
    Task DeleteByTokenAsync(string fcmToken);
}
