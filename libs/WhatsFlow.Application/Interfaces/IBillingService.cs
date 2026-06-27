using System.Text.Json;
using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Orquestra o billing de assinatura: cria assinatura (customer+subscription no Asaas
/// da plataforma) com trial, consulta/cancela, e processa os webhooks do gateway
/// aplicando a máquina de estados da assinatura.
/// </summary>
public interface IBillingService
{
    Task<IEnumerable<PlanoDto>> ListarPlanosAsync();
    Task<AssinaturaDto> AssinarAsync(AssinarTenantDto dto);
    Task<AssinaturaDto?> ObterPorTenantAsync(int tenantId);

    // Admin de plataforma
    Task<IEnumerable<AssinaturaDto>> ListarTodasAsync();
    Task<AssinaturaDto?> SuspenderAsync(int tenantId);
    Task<AssinaturaDto?> ReativarAsync(int tenantId);
    Task<IEnumerable<FaturaDto>> ListarFaturasAsync(int tenantId);
    Task<AssinaturaDto?> CancelarAsync(int tenantId);

    /// <summary>Processa um webhook de billing do Asaas. Retorna false para responder 401.</summary>
    Task<bool> ProcessarWebhookAsync(JsonElement payload, string? accessToken);

    /// <summary>
    /// Regra de gating: true se o tenant deve ser bloqueado (assinatura suspensa, ou
    /// cancelada após o fim do período pago). Fail-open: sem assinatura → não bloqueia.
    /// </summary>
    Task<bool> TenantBloqueadoAsync(int tenantId);
}
