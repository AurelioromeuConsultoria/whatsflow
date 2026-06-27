using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Transições automáticas do ciclo de billing (rodadas por job): trial expirado →
/// inadimplente; inadimplente além da carência → suspensa (com notificação).
/// </summary>
public interface IBillingCycleService
{
    Task<CicloBillingResultado> ExecutarTransicoesAutomaticasAsync(CancellationToken cancellationToken = default);
}
