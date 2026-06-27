namespace WhatsFlow.API.Permissions;

public static class PermissionResourceMap
{
    private static readonly Dictionary<string, string> ResourceByPath = new()
    {
        // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
        ["/api/dashboard"] = "dashboard",
        ["/api/usuarios"] = "usuarios",
        ["/api/pessoas"] = "pessoas",
        ["/api/visitantes"] = "visitantes",
        ["/api/configuracoesmensagens"] = "configuracoes-mensagens",
        ["/api/mensagensagendadas"] = "mensagens-agendadas",
        ["/api/comunicacao"] = "comunicacao",
        ["/api/comunicacaocampanhas"] = "comunicacao",
        ["/api/comunicacaotemplates"] = "comunicacao",
        ["/api/comunicacaoentregas"] = "comunicacao",
        ["/api/comunicacaoautomacoes"] = "comunicacao",
        ["/api/comunicacaopreferencias"] = "comunicacao",
        ["/api/comunicacaosegmentos"] = "comunicacao",
        ["/api/contatos"] = "contatos",
        ["/api/perfis-acesso"] = "perfis-acesso",
        ["/api/auditlogs"] = "usuarios"
    };

    public static string? GetResourceFromPath(string path)
    {
        foreach (var kv in ResourceByPath)
        {
            if (path.StartsWith(kv.Key))
                return kv.Value;
        }

        return null;
    }

    public static string? GetActionFromMethod(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => "view",
            "POST" => "edit",
            "PUT" => "edit",
            "PATCH" => "edit",
            "DELETE" => "delete",
            _ => null
        };
    }
}
