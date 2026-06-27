using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public static class ComunicacaoModule
{
    public const string Resource = "comunicacao";

    public static readonly IReadOnlyList<string> AuditActions =
    [
        "CriarCampanha",
        "AgendarCampanha",
        "CancelarCampanha",
        "ReprocessarEntrega",
        "AtualizarPreferenciaCanal"
    ];

    public static readonly IReadOnlyList<CanalComunicacao> CanaisExternosMvp =
    [
        CanalComunicacao.WhatsApp,
        CanalComunicacao.Email
    ];

    public static readonly IReadOnlyList<CanalComunicacao> CanaisComplementaresMvp =
    [
        CanalComunicacao.Push,
        CanalComunicacao.NotificacaoInterna
    ];

    public static string BuildDeliveryScope(CanalComunicacao canal, string status)
    {
        return $"comunicacao.entrega.{canal.ToString().ToLowerInvariant()}.{status.ToLowerInvariant()}";
    }
}
