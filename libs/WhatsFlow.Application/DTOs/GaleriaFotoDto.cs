namespace WhatsFlow.Application.DTOs;

public class GaleriaFotoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Data { get; set; }
    public string CaminhoDiretorio { get; set; } = string.Empty;
    public string? ImagemDestaque { get; set; }
    public int QuantidadeFotos { get; set; }
    public bool Ativo { get; set; }
    public int? EventoId { get; set; }
    public string? EventoTitulo { get; set; }
    public int? CategoriaMidiaId { get; set; }
    public string? CategoriaMidiaNome { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarGaleriaFotoDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Data { get; set; }
    public int? EventoId { get; set; }
    public int? CategoriaMidiaId { get; set; }
    public bool Ativo { get; set; } = true;
}

public class AtualizarGaleriaFotoDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Data { get; set; }
    public int? EventoId { get; set; }
    public int? CategoriaMidiaId { get; set; }
    public bool Ativo { get; set; }
}

public class FotoDto
{
    public string NomeArquivo { get; set; } = string.Empty;
    public bool Destaque { get; set; }
}

