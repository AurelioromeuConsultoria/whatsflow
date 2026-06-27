namespace WhatsFlow.Application.DTOs;

public class CampanhaAniversarioConfiguracaoDto
{
    public int Id { get; set; }
    public bool Ativo { get; set; }
    public string? ImagemUrl { get; set; }
    public string MensagemTemplate { get; set; } = string.Empty;
    public string HorarioEnvio { get; set; } = "09:00";
    public DateTime DataAtualizacao { get; set; }
    public int TotalEnviosRecentes { get; set; }
    public CampanhaAniversarioMetricasDto Metricas { get; set; } = new();
    public CampanhaAniversarioHistoricoFiltroDto Filtros { get; set; } = new();
    public List<CampanhaAniversarioEnvioDto> EnviosRecentes { get; set; } = new();
}

public class AtualizarCampanhaAniversarioDto
{
    public bool Ativo { get; set; } = true;
    public string? ImagemUrl { get; set; }
    public string MensagemTemplate { get; set; } = string.Empty;
    public string HorarioEnvio { get; set; } = "09:00";
}

public class EnviarTesteCampanhaAniversarioDto
{
    public string Nome { get; set; } = "Teste";
    public string WhatsApp { get; set; } = string.Empty;
}

public class CampanhaAniversarioEnvioTesteResultadoDto
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? Detalhes { get; set; }
}

public class CampanhaAniversarioHistoricoFiltroDto
{
    public string? Busca { get; set; }
    public string? Status { get; set; }
    public int Limit { get; set; } = 50;
}

public class CampanhaAniversarioEnvioDto
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public string NomePessoa { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public int AnoReferencia { get; set; }
    public DateTime DataAniversario { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Tentativas { get; set; }
    public DateTime? DataUltimaTentativa { get; set; }
    public DateTime? DataEnvioSucesso { get; set; }
    public string? LogErro { get; set; }
}

public class CampanhaAniversarioMetricasDto
{
    public int TotalHistorico { get; set; }
    public int TotalEnviadosAnoAtual { get; set; }
    public int TotalFalhasAnoAtual { get; set; }
    public int TotalPendentesAnoAtual { get; set; }
    public int TotalEnviadosHoje { get; set; }
    public int TotalFalhasHoje { get; set; }
}

public class CampanhaAniversarioProcessamentoResultadoDto
{
    public int TotalElegiveis { get; set; }
    public int TotalEnviados { get; set; }
    public int TotalIgnorados { get; set; }
    public int TotalFalhas { get; set; }
}

public class CampanhaAniversarioReenvioResultadoDto
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int EnvioId { get; set; }
    public string? MessageId { get; set; }
    public string? Detalhes { get; set; }
}
