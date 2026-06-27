namespace WhatsFlow.Application.DTOs;

/// <summary>
/// Pacote de dados pessoais de um titular, para atender ao direito de acesso e
/// portabilidade (LGPD, Art. 18). Reúne os dados das principais entidades vinculadas.
/// </summary>
public class DadosPessoaisExportDto
{
    public DateTime GeradoEm { get; set; }
    public PessoaDadosDto Pessoa { get; set; } = new();
    public List<PerfilDadosDto> Perfis { get; set; } = new();
    public List<VisitaDadosDto> Visitas { get; set; } = new();
    public List<VoluntariadoDadosDto> Voluntariados { get; set; } = new();
    public DetalheCriancaDadosDto? DetalheCrianca { get; set; }
    public List<VinculoResponsavelDadosDto> VinculosResponsaveis { get; set; } = new();
    public List<CheckinDadosDto> CheckinsKids { get; set; } = new();
    public List<OcorrenciaDadosDto> OcorrenciasKids { get; set; } = new();
    public List<DoacaoDadosDto> Doacoes { get; set; } = new();
    public List<PreferenciaComunicacaoDadosDto> PreferenciasComunicacao { get; set; } = new();
    public List<ConsentimentoDadosDto> Consentimentos { get; set; } = new();
}

public class PessoaDadosDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string TipoPessoa { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class PerfilDadosDto
{
    public string Perfil { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}

public class VisitaDadosDto
{
    public DateTime DataVisita { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCadastro { get; set; }
}

public class VoluntariadoDadosDto
{
    public int EquipeId { get; set; }
    public int CargoId { get; set; }
    public DateTime DataCadastro { get; set; }
}

public class DetalheCriancaDadosDto
{
    public string? Alergias { get; set; }
    public string? RestricoesAlimentares { get; set; }
    public string? Observacoes { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
}

public class VinculoResponsavelDadosDto
{
    public string Papel { get; set; } = string.Empty; // "crianca" ou "responsavel"
    public int CriancaPessoaId { get; set; }
    public int ResponsavelPessoaId { get; set; }
    public string? Parentesco { get; set; }
    public bool PodeRetirar { get; set; }
}

public class CheckinDadosDto
{
    public DateTime CheckinTime { get; set; }
    public DateTime? CheckoutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Metodo { get; set; } = string.Empty;
}

public class OcorrenciaDadosDto
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}

public class DoacaoDadosDto
{
    public string NomeDoador { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string MetodoPagamento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataConfirmacao { get; set; }
}

public class PreferenciaComunicacaoDadosDto
{
    public string Canal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? OrigemConsentimento { get; set; }
}

public class ConsentimentoDadosDto
{
    public string Tipo { get; set; } = string.Empty;
    public string VersaoDocumento { get; set; } = string.Empty;
    public DateTime AceitoEm { get; set; }
    public string? Origem { get; set; }
    public DateTime? RevogadoEm { get; set; }
}

/// <summary>Resultado da anonimização de um titular (LGPD, direito ao esquecimento).</summary>
public class AnonimizacaoResultadoDto
{
    public int PessoaId { get; set; }
    public string NomeAnonimizado { get; set; } = string.Empty;
    public DateTime AnonimizadoEm { get; set; }
    public int RegistrosAfetados { get; set; }
}
