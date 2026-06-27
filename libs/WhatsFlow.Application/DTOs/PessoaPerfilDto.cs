using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class PessoaPerfilDto
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public string NomePessoa { get; set; } = string.Empty;
    public PerfilPessoa Perfil { get; set; }
    public string PerfilDescricao { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public bool Ativo { get; set; }
}

public class CriarPessoaPerfilDto
{
    [Required(ErrorMessage = "PessoaId é obrigatório")]
    public int PessoaId { get; set; }

    [Required(ErrorMessage = "Perfil é obrigatório")]
    public PerfilPessoa Perfil { get; set; }

    public DateTime? DataInicio { get; set; }
}

public class AtualizarPessoaPerfilDto
{
    public PerfilPessoa Perfil { get; set; }
    public DateTime? DataFim { get; set; }
}



