namespace WhatsFlow.API.Permissions;

public static class PermissionResourceMap
{
    private static readonly Dictionary<string, string> ResourceByPath = new()
    {
        ["/api/dashboard"] = "dashboard",
        ["/api/usuarios"] = "usuarios",
        ["/api/solicitacoestitular"] = "pessoas",
        ["/api/pessoas"] = "pessoas",
        ["/api/pessoasperfis"] = "perfis",
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
        ["/api/equipes"] = "equipes",
        ["/api/cargos"] = "cargos",
        ["/api/voluntarios"] = "voluntarios",
        ["/api/eventos"] = "eventos",
        ["/api/inscricoeseventos"] = "inscricoes-eventos",
        ["/api/categoriasnoticias"] = "categorias-noticias",
        ["/api/noticias"] = "noticias",
        ["/api/contatos"] = "contatos",
        ["/api/destaquessite"] = "destaques-site",
        ["/api/configuracaoportal"] = "portal",
        ["/api/categoriasmidias"] = "categorias-midias",
        ["/api/galeriasfotos"] = "galerias-fotos",
        ["/api/enquetes"] = "enquetes",
        ["/api/kids"] = "kids",
        ["/api/hub"] = "hub",
        ["/api/fornecedores"] = "fornecedores",
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
