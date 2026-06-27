using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class VisitanteDto
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public DateTime DataVisita { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCadastro { get; set; }
    public List<string> Perfis { get; set; } = new();
}

// DTO de Request para criar visitante
public class CreateVisitanteRequest
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
    
    public DateTime? DataVisita { get; set; }
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }
}

// DTO de Response para visitante
public class VisitanteResponse
{
    public int VisitanteId { get; set; }
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public DateTime DataVisita { get; set; }
    public string? Observacoes { get; set; }
    public List<string> Perfis { get; set; } = new();
}

// DTOs legados mantidos para compatibilidade
public class CriarVisitanteDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public DateTime? DataNascimento { get; set; }
    public DateTime DataVisita { get; set; }
    public string? Observacoes { get; set; }
}

public class AtualizarVisitanteDto
{
    public DateTime DataVisita { get; set; }
    public string? Observacoes { get; set; }
}

