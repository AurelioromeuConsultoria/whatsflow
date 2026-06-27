namespace WhatsFlow.Application.Services;

public static class ComunicacaoObservability
{
    public static class Events
    {
        public const string CampanhaCriada = "comunicacao.campanha.criada";
        public const string CampanhaAgendada = "comunicacao.campanha.agendada";
        public const string CampanhaCancelada = "comunicacao.campanha.cancelada";
        public const string EntregaReservada = "comunicacao.entrega.reservada";
        public const string EntregaEnviada = "comunicacao.entrega.enviada";
        public const string EntregaFalhou = "comunicacao.entrega.falhou";
        public const string EntregaReprocessada = "comunicacao.entrega.reprocessada";
        public const string PreferenciaAtualizada = "comunicacao.preferencia.atualizada";
        public const string AutomacaoExecutada = "comunicacao.automacao.executada";
        public const string AutomacaoFalhou = "comunicacao.automacao.falhou";
    }

    public static class Tags
    {
        public const string CampanhaId = "CampanhaId";
        public const string EntregaId = "EntregaId";
        public const string TemplateId = "TemplateId";
        public const string AutomacaoId = "AutomacaoId";
        public const string Canal = "Canal";
        public const string DestinatarioPessoaId = "DestinatarioPessoaId";
        public const string DestinatarioVisitanteId = "DestinatarioVisitanteId";
        public const string UsuarioId = "UsuarioId";
        public const string Status = "Status";
        public const string Provider = "Provider";
    }
}
