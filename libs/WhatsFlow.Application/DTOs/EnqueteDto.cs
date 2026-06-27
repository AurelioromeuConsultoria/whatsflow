namespace WhatsFlow.Application.DTOs;

public class EnqueteDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool Ativo { get; set; }
    public bool PermitirMultiplaEscolha { get; set; }
    public bool PermitirVotoAnonimo { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<EnqueteOpcaoDto> Opcoes { get; set; } = new();
    public int TotalVotos { get; set; }
}

public class EnqueteOpcaoDto
{
    public int Id { get; set; }
    public int EnqueteId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public int TotalVotos { get; set; }
    public double PercentualVotos { get; set; }
}

public class CriarEnqueteDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool Ativo { get; set; } = true;
    public bool PermitirMultiplaEscolha { get; set; } = false;
    public bool PermitirVotoAnonimo { get; set; } = true;
    public List<CriarEnqueteOpcaoDto> Opcoes { get; set; } = new();
}

public class CriarEnqueteOpcaoDto
{
    public string Texto { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

public class AtualizarEnqueteDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool Ativo { get; set; }
    public bool PermitirMultiplaEscolha { get; set; }
    public bool PermitirVotoAnonimo { get; set; }
    public List<AtualizarEnqueteOpcaoDto> Opcoes { get; set; } = new();
}

public class AtualizarEnqueteOpcaoDto
{
    public int? Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Ordem { get; set; }
}
