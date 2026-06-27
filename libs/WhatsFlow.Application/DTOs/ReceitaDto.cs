using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

public class ReceitaDto
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataRecebimento { get; set; }
    public DateTime? DataConfirmacao { get; set; }
    public StatusReceita Status { get; set; }
    public string StatusDescricao { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public string? ComprovanteUrl { get; set; }
    public int? CategoriaReceitaId { get; set; }
    public string? CategoriaReceitaNome { get; set; }
    public int? ContaBancariaId { get; set; }
    public string? ContaBancariaNome { get; set; }
    public int? CentroCustoId { get; set; }
    public string? CentroCustoNome { get; set; }
    public int? ProjetoId { get; set; }
    public string? ProjetoNome { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public int? PessoaId { get; set; }
    public string? PessoaNome { get; set; }
    public bool Recorrente { get; set; }
    public TipoRecorrencia? TipoRecorrencia { get; set; }
    public string? TipoRecorrenciaDescricao { get; set; }
    public int? RecorrenciaOriginalId { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarReceitaDto
{
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "Data de recebimento é obrigatória")]
    public DateTime DataRecebimento { get; set; }

    public DateTime? DataConfirmacao { get; set; }

    public StatusReceita Status { get; set; } = StatusReceita.Pendente;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    [MaxLength(500)]
    public string? ComprovanteUrl { get; set; }

    public int? CategoriaReceitaId { get; set; }
    public int? ContaBancariaId { get; set; }
    public int? CentroCustoId { get; set; }
    public int? ProjetoId { get; set; }
    public int? UsuarioId { get; set; }
    public int? PessoaId { get; set; }
    public bool Recorrente { get; set; } = false;
    public TipoRecorrencia? TipoRecorrencia { get; set; }
}

public class AtualizarReceitaDto
{
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "Data de recebimento é obrigatória")]
    public DateTime DataRecebimento { get; set; }

    public DateTime? DataConfirmacao { get; set; }

    public StatusReceita Status { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    [MaxLength(500)]
    public string? ComprovanteUrl { get; set; }

    public int? CategoriaReceitaId { get; set; }
    public int? ContaBancariaId { get; set; }
    public int? CentroCustoId { get; set; }
    public int? ProjetoId { get; set; }
    public int? UsuarioId { get; set; }
    public int? PessoaId { get; set; }
    public bool Recorrente { get; set; } = false;
    public TipoRecorrencia? TipoRecorrencia { get; set; }
}

public class LancarContribuicoesLoteDto
{
    [Required(ErrorMessage = "Data é obrigatória")]
    public DateTime Data { get; set; }

    public string? DescricaoPadrao { get; set; }

    public int? CategoriaReceitaId { get; set; }
    public int? ContaBancariaId { get; set; }
    public int? CentroCustoId { get; set; }
    public int? ProjetoId { get; set; }

    [Required(ErrorMessage = "Informe pelo menos um item")]
    public List<ContribuicaoLoteItemDto> Itens { get; set; } = new();
}

public class ContribuicaoLoteItemDto
{
    public int? PessoaId { get; set; }

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    public string? Descricao { get; set; }
    public string? Observacoes { get; set; }
}

public class ContribuicaoMembroDto
{
    public int PessoaId { get; set; }
    public string PessoaNome { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int QuantidadeLancamentos { get; set; }
    public DateTime? UltimaContribuicao { get; set; }
    public List<ContribuicaoMembroCategoriaDto> PorCategoria { get; set; } = new();
}

public class ContribuicaoMembroCategoriaDto
{
    public int? CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
}

public class RelatorioContribuicoesDto
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal TotalGeral { get; set; }
    public int TotalLancamentos { get; set; }
    public int TotalMembrosContribuiram { get; set; }
    public int TotalMembrosSemContribuicao { get; set; }
    public List<ContribuicaoMembroDto> Contribuidores { get; set; } = new();
    public List<MembroSemContribuicaoDto> SemContribuicao { get; set; } = new();
}

public class MembroSemContribuicaoDto
{
    public int PessoaId { get; set; }
    public string PessoaNome { get; set; } = string.Empty;
    public DateTime? UltimaContribuicaoConhecida { get; set; }
}

public class InformeContribuicoesDto
{
    public int PessoaId { get; set; }
    public string PessoaNome { get; set; } = string.Empty;
    public string? PessoaEmail { get; set; }
    public int Ano { get; set; }
    public decimal TotalAnual { get; set; }
    public List<InformeContribuicaoMesDto> PorMes { get; set; } = new();
    public List<ContribuicaoMembroCategoriaDto> PorCategoria { get; set; } = new();
    public DateTime DataEmissao { get; set; }
}

public class InformeContribuicaoMesDto
{
    public int Mes { get; set; }
    public string MesNome { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
}
