using System.ComponentModel.DataAnnotations;

namespace WhatsFlow.Domain.Entities;

public class EnvioCampanhaAniversario : ITenantEntity
{
    public int Id { get; set; }

    public int TenantId { get; set; } = Tenant.InitialTenantId;
    public virtual Tenant Tenant { get; set; } = null!;

    public int PessoaId { get; set; }
    public virtual Pessoa Pessoa { get; set; } = null!;

    public int AnoReferencia { get; set; }

    public DateTime DataAniversario { get; set; }

    public StatusEnvioCampanhaAniversario Status { get; set; } = StatusEnvioCampanhaAniversario.Pendente;

    public int Tentativas { get; set; }

    public DateTime? DataUltimaTentativa { get; set; }

    public DateTime? DataEnvioSucesso { get; set; }

    [MaxLength(20)]
    public string? WhatsAppUtilizado { get; set; }

    [MaxLength(1000)]
    public string? ImagemUrlUtilizada { get; set; }

    [MaxLength(4000)]
    public string? MensagemUtilizada { get; set; }

    [MaxLength(1000)]
    public string? LogErro { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}

public enum StatusEnvioCampanhaAniversario
{
    Pendente = 1,
    EmProcessamento = 2,
    Enviado = 3,
    Erro = 4
}
