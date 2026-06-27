namespace WhatsFlow.Application.DTOs;

public class PerfilAcessoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<PermissaoPerfilDto> Permissoes { get; set; } = new();
}

public class PermissaoPerfilDto
{
    public int Id { get; set; }
    public string Recurso { get; set; } = string.Empty;
    public bool PodeVer { get; set; }
    public bool PodeEditar { get; set; }
    public bool PodeExcluir { get; set; }
}

public class CriarPerfilAcessoDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public List<PermissaoPerfilDto> Permissoes { get; set; } = new();
}

public class AtualizarPerfilAcessoDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public List<PermissaoPerfilDto> Permissoes { get; set; } = new();
}
