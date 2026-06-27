using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Application.JsonConverters;

namespace WhatsFlow.Application.DTOs;

public class PessoaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public DateTime? DataNascimento { get; set; }
    [JsonConverter(typeof(TipoPessoaJsonConverter))]
    public TipoPessoa TipoPessoa { get; set; }
    public string TipoPessoaDescricao { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<PessoaPerfilDto> Perfis { get; set; } = new();
}

public class AniversarianteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public DateTime ProximoAniversario { get; set; }
    public int DiasParaAniversario { get; set; }
}

public class CriarPessoaDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    public DateTime? DataNascimento { get; set; }

    [JsonConverter(typeof(TipoPessoaJsonConverter))]
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Adulto;
}

public class AtualizarPessoaDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    public DateTime? DataNascimento { get; set; }

    [JsonConverter(typeof(TipoPessoaJsonConverter))]
    public TipoPessoa TipoPessoa { get; set; }

    public bool Ativo { get; set; }
}

public class AtualizarMinhaPessoaDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    public DateTime? DataNascimento { get; set; }
}

