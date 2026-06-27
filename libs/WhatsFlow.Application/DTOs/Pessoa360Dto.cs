namespace WhatsFlow.Application.DTOs;

/// <summary>
/// Visão consolidada 360° de uma pessoa (perfis, visitas, voluntariado, usuário).
/// </summary>
public class Pessoa360Dto
{
    public PessoaDto Pessoa { get; set; } = null!;
    public List<VisitanteDto> Visitantes { get; set; } = new();
    public List<VoluntarioDto> Voluntarios { get; set; } = new();
    public UsuarioResumoDto? Usuario { get; set; }
}

/// <summary>
/// Resumo do usuário para exibição na visão 360 (sem dados sensíveis).
/// </summary>
public class UsuarioResumoDto
{
    public int Id { get; set; }
    public string EmailLogin { get; set; } = string.Empty;
    public string TipoUsuarioDescricao { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public string? PerfilAcessoNome { get; set; }
    public DateTime? UltimoAcesso { get; set; }
}
