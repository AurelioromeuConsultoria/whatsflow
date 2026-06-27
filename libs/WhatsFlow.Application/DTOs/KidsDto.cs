using System.ComponentModel.DataAnnotations;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.DTOs;

// DTOs para Criança
public class CriancaDto
{
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    
    // Detalhes
    public string? Alergias { get; set; }
    public string? RestricoesAlimentares { get; set; }
    public string? Observacoes { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
    public DateTime DataCadastro { get; set; }
    
    // Relacionamentos
    public List<ResponsavelCriancaDto> Responsaveis { get; set; } = new();
    public bool EstaCheckedIn { get; set; }
    public KidsCheckinDto? CheckinAtual { get; set; }
}

public class CreateCriancaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data de nascimento é obrigatória")]
    public DateTime DataNascimento { get; set; }

    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [MaxLength(500)]
    public string? Alergias { get; set; }

    [MaxLength(500)]
    public string? RestricoesAlimentares { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    public List<ResponsavelRequest>? Responsaveis { get; set; }

    /// <summary>
    /// Versão do termo de consentimento parental concedido por um responsável (ex.: "v1").
    /// Obrigatório para LGPD (dado sensível de menor): sem consentimento parental não há cadastro.
    /// </summary>
    [Required(ErrorMessage = "É necessário o consentimento parental para cadastrar uma criança")]
    [MaxLength(20)]
    public string ConsentimentoParentalVersao { get; set; } = string.Empty;
}

public class ResponsavelRequest
{
    // Se o responsável já existe, fornecer o ID
    public int? ResponsavelPessoaId { get; set; }

    // Se o responsável não existe, fornecer dados para criar
    [MaxLength(100)]
    public string? Nome { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "PodeRetirar é obrigatório")]
    public bool PodeRetirar { get; set; } = true;

    [MaxLength(50)]
    public string? Parentesco { get; set; }
}

public class UpdateCriancaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data de nascimento é obrigatória")]
    public DateTime DataNascimento { get; set; }

    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [MaxLength(500)]
    public string? Alergias { get; set; }

    [MaxLength(500)]
    public string? RestricoesAlimentares { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }
}

// DTOs para Responsáveis
public class ResponsavelCriancaDto
{
    public int Id { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public int ResponsavelPessoaId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    public string? ResponsavelTelefone { get; set; }
    public string? ResponsavelWhatsApp { get; set; }
    public string? ResponsavelEmail { get; set; }
    public bool PodeRetirar { get; set; }
    public string? Parentesco { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCadastro { get; set; }
}

public class CreateResponsavelRequest
{
    [Required(ErrorMessage = "ResponsavelPessoaId é obrigatório")]
    public int ResponsavelPessoaId { get; set; }

    [Required(ErrorMessage = "PodeRetirar é obrigatório")]
    public bool PodeRetirar { get; set; } = true;

    [MaxLength(50)]
    public string? Parentesco { get; set; }
}

public class UpdateResponsavelRequest
{
    public bool? PodeRetirar { get; set; }

    [MaxLength(50)]
    public string? Parentesco { get; set; }

    public bool? Ativo { get; set; }
}

// DTOs para Check-in/Check-out
public class CheckinRequest
{
    [Required(ErrorMessage = "CriancaPessoaId é obrigatório")]
    public int CriancaPessoaId { get; set; }

    [Required(ErrorMessage = "Método é obrigatório")]
    [MaxLength(20)]
    public string Metodo { get; set; } = "ADMIN"; // "QR", "PIN", "ADMIN"

    public int? CheckinByPessoaId { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }
}

public class CheckoutRequest
{
    [Required(ErrorMessage = "CriancaPessoaId é obrigatório")]
    public int CriancaPessoaId { get; set; }

    [Required(ErrorMessage = "CodigoSessao é obrigatório")]
    [MaxLength(50)]
    public string CodigoSessao { get; set; } = string.Empty;

    [Required(ErrorMessage = "CheckoutByPessoaId é obrigatório")]
    public int CheckoutByPessoaId { get; set; }

    [MaxLength(20)]
    public string? Metodo { get; set; } // "QR", "PIN", "ADMIN"
}

public class CheckinResponse
{
    public int CheckinId { get; set; }
    public string CodigoSessao { get; set; } = string.Empty;
    public string? TokenRetirada { get; set; }
    public string? PinRetirada { get; set; }
    public DateTime? TokenRetiradaExpiraEm { get; set; }
    public DateTime CheckinTime { get; set; }
    public List<NotificacaoCriadaDto> Notificacoes { get; set; } = new();
}

public class NotificacaoCriadaDto
{
    public int ResponsavelPessoaId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class KidsCheckinDto
{
    public int Id { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public DateTime CheckinTime { get; set; }
    public DateTime? CheckoutTime { get; set; }
    public int? CheckinByPessoaId { get; set; }
    public string? CheckinByNome { get; set; }
    public int? CheckoutByPessoaId { get; set; }
    public string? CheckoutByNome { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string CodigoSessao { get; set; } = string.Empty;
    public string? TokenRetirada { get; set; }
    public string? PinRetirada { get; set; }
    public DateTime? TokenRetiradaExpiraEm { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? RetiradaConfirmadaPorPessoaId { get; set; }
    public string? RetiradaMetodo { get; set; }
    public bool RetiradaEmModoExcecao { get; set; }
    public string? RetiradaMotivoExcecao { get; set; }
    public string? RetiradaPessoaNome { get; set; }
    public string? Observacoes { get; set; }
}

public class ValidarRetiradaRequest
{
    [MaxLength(80)]
    public string? Token { get; set; }

    [MaxLength(10)]
    public string? Pin { get; set; }
}

public class ConfirmarRetiradaRequest
{
    [Required(ErrorMessage = "CheckinId é obrigatório")]
    public int CheckinId { get; set; }

    [MaxLength(80)]
    public string? Token { get; set; }

    [MaxLength(10)]
    public string? Pin { get; set; }

    [Required(ErrorMessage = "ResponsavelPessoaId é obrigatório")]
    public int ResponsavelPessoaId { get; set; }

    [Required(ErrorMessage = "Método é obrigatório")]
    [MaxLength(20)]
    public string Metodo { get; set; } = "QR";

    [MaxLength(500)]
    public string? Observacoes { get; set; }
}

public class RetiradaExcecaoRequest
{
    [Required(ErrorMessage = "CheckinId é obrigatório")]
    public int CheckinId { get; set; }

    [Required(ErrorMessage = "Nome da pessoa retirando é obrigatório")]
    [MaxLength(200)]
    public string PessoaRetirandoNome { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PessoaRetirandoDocumento { get; set; }

    [Required(ErrorMessage = "Motivo é obrigatório")]
    [MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Observacoes { get; set; }
}

public class RetiradaAutorizadoDto
{
    public int ResponsavelPessoaId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    public string? Parentesco { get; set; }
    public bool PodeRetirar { get; set; }
}

public class RetiradaValidacaoDto
{
    public int CheckinId { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public string? SalaId { get; set; }
    public DateTime CheckinTime { get; set; }
    public DateTime? TokenRetiradaExpiraEm { get; set; }
    public bool Expirado { get; set; }
    public string MetodoValidado { get; set; } = string.Empty;
    public List<string> MetodosDisponiveis { get; set; } = new();
    public List<RetiradaAutorizadoDto> ResponsaveisAutorizados { get; set; } = new();
}

public class KidsPainelOperacionalDto
{
    public int TotalPresentes { get; set; }
    public int TotalPendentesRetirada { get; set; }
    public int TotalRetiradasHoje { get; set; }
    public int TotalAlertasCriticos { get; set; }
    public List<KidsPainelSalaDto> Salas { get; set; } = new();
    public List<KidsPainelCriancaDto> CriancasPresentes { get; set; } = new();
    public List<KidsPainelCriancaDto> Pendencias { get; set; } = new();
    public List<KidsPainelCriancaDto> AlertasCriticos { get; set; } = new();
}

public class KidsPainelCriancaDto
{
    public int CriancaPessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? SalaId { get; set; }
    public DateTime CheckinTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool TemAlergia { get; set; }
    public bool TemRestricao { get; set; }
    public bool TemObservacaoCritica { get; set; }
    public bool TokenRetiradaAtivo { get; set; }
    public bool RetiradaEmModoExcecao { get; set; }
}

public class KidsPainelSalaDto
{
    public string SalaId { get; set; } = "Sem sala";
    public int TotalPresentes { get; set; }
    public int TotalAlertasCriticos { get; set; }
    public int TotalPendentesRetirada { get; set; }
}

public class CriarKidsOcorrenciaRequest
{
    [Required(ErrorMessage = "CriancaPessoaId é obrigatório")]
    public int CriancaPessoaId { get; set; }

    public int? CheckinId { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório")]
    [MaxLength(40)]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Titulo é obrigatório")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descricao é obrigatória")]
    [MaxLength(2000)]
    public string Descricao { get; set; } = string.Empty;

    public bool RequerContatoResponsavel { get; set; }
    public bool VisivelAoResponsavel { get; set; }
}

public class AtualizarKidsOcorrenciaRequest
{
    [MaxLength(2000)]
    public string? Descricao { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    public bool? ContatoResponsavelRealizado { get; set; }
    public bool? VisivelAoResponsavel { get; set; }
}

public class KidsOcorrenciaDto
{
    public int Id { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public int? CheckinId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool RequerContatoResponsavel { get; set; }
    public DateTime? ContatoResponsavelRealizadoEm { get; set; }
    public string? ContatoResponsavelPorNome { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
    public int RegistradoPorPessoaId { get; set; }
    public string RegistradoPorNome { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? EncerradoEm { get; set; }
    public string? EncerradoPorNome { get; set; }
    public bool VisivelAoResponsavel { get; set; }
}

public class KidsOcorrenciaResumoDto
{
    public int Id { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}

public class KidsSalaDto
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int? CapacidadeMaxima { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class CreateKidsSalaRequest
{
    [Required(ErrorMessage = "Id é obrigatório")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    public int? CapacidadeMaxima { get; set; }
    public bool Ativo { get; set; } = true;
}

public class UpdateKidsSalaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    public int? CapacidadeMaxima { get; set; }
    public bool Ativo { get; set; } = true;
}

public class KidsTurmaDto
{
    public string Id { get; set; } = string.Empty;
    public string SalaId { get; set; } = string.Empty;
    public string? SalaNome { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? CapacidadeMaxima { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class CreateKidsTurmaRequest
{
    [Required(ErrorMessage = "Id é obrigatório")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "SalaId é obrigatório")]
    [MaxLength(50)]
    public string SalaId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    public int? CapacidadeMaxima { get; set; }
    public bool Ativo { get; set; } = true;
}

public class UpdateKidsTurmaRequest
{
    [Required(ErrorMessage = "SalaId é obrigatório")]
    [MaxLength(50)]
    public string SalaId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    public int? CapacidadeMaxima { get; set; }
    public bool Ativo { get; set; } = true;
}

public class KidsIndicadoresDto
{
    public int DiasAnalisados { get; set; }
    public int TotalCriancasAtivas { get; set; }
    public int TotalResponsaveisAtivos { get; set; }
    public int TotalSalasAtivas { get; set; }
    public int TotalTurmasAtivas { get; set; }
    public int TotalCheckinsPeriodo { get; set; }
    public decimal MediaCheckinsPorDia { get; set; }
    public int TotalRetiradasQr { get; set; }
    public int TotalRetiradasPin { get; set; }
    public int TotalRetiradasExcecao { get; set; }
    public int TotalOcorrenciasAbertas { get; set; }
    public int TotalCriancasPresentesAgora { get; set; }
}

// DTOs do contexto do responsável (`me/*`)
// Estes contratos existem para separar explicitamente a visão do responsável
// da visão administrativa/operacional do módulo.
public class MinhaCriancaResumoDto
{
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
    public bool EstaCheckedIn { get; set; }
    public MeuCheckinResumoDto? CheckinAtual { get; set; }
    public bool TemAlertaCritico { get; set; }
    public string? FotoUrl { get; set; }
}

public class MinhaCriancaDetalheDto
{
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
    public string? Alergias { get; set; }
    public string? RestricoesAlimentares { get; set; }
    public string? ObservacoesVisiveisAoResponsavel { get; set; }
    public bool EstaCheckedIn { get; set; }
    public MeuCheckinResumoDto? CheckinAtual { get; set; }
    public List<MeuCheckinResumoDto> HistoricoRecente { get; set; } = new();
    public string? FotoUrl { get; set; }
}

public class MeuCheckinResumoDto
{
    public int Id { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public DateTime CheckinTime { get; set; }
    public DateTime? CheckoutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SalaId { get; set; }
    public string? TokenRetirada { get; set; }
    public string? PinRetirada { get; set; }
    public DateTime? TokenRetiradaExpiraEm { get; set; }
}

public class MeuHistoricoPagedDto
{
    public List<MeuCheckinResumoDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore => Page * PageSize < Total;
}

public class CreateKidsPreCheckinRequest
{
    [Required(ErrorMessage = "CriancaPessoaId é obrigatório")]
    public int CriancaPessoaId { get; set; }

    public int? EventoOcorrenciaId { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }
}

public class ConfirmKidsPreCheckinRequest
{
    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    [MaxLength(500)]
    public string? ObservacoesEquipe { get; set; }
}

public class CancelKidsPreCheckinRequest
{
    [MaxLength(500)]
    public string? Motivo { get; set; }
}

public class ValidarKidsPreCheckinRequest
{
    [MaxLength(80)]
    public string? QrToken { get; set; }

    [MaxLength(20)]
    public string? CodigoCurto { get; set; }
}

public class KidsPreCheckinDto
{
    public int Id { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public int ResponsavelPessoaId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    public int? EventoOcorrenciaId { get; set; }
    public int? CheckinId { get; set; }
    public DateTime? EventoDataHoraInicio { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
    public string QrToken { get; set; } = string.Empty;
    public string CodigoCurto { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public string? ObservacoesResponsavel { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? ConfirmadoEm { get; set; }
    public string? ConfirmadoPorNome { get; set; }
    public DateTime? CanceladoEm { get; set; }
    public string? CanceladoPorNome { get; set; }
    public string? CancelamentoMotivo { get; set; }
}

public class KidsConteudoAulaAnexoDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string NomeExibicao { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? StoragePath { get; set; }
    public string? MimeType { get; set; }
    public long? TamanhoBytes { get; set; }
    public int Ordem { get; set; }
}

public class CreateKidsConteudoAulaAnexoRequest
{
    [Required(ErrorMessage = "Tipo é obrigatório")]
    [MaxLength(20)]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome de exibição é obrigatório")]
    [MaxLength(200)]
    public string NomeExibicao { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? StoragePath { get; set; }

    [MaxLength(120)]
    public string? MimeType { get; set; }

    public long? TamanhoBytes { get; set; }
    public int Ordem { get; set; }
}

public class CreateKidsConteudoAulaRequest
{
    [Required(ErrorMessage = "Título é obrigatório")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Tema { get; set; }

    [MaxLength(300)]
    public string? Versiculo { get; set; }

    [Required(ErrorMessage = "Resumo é obrigatório")]
    [MaxLength(4000)]
    public string Resumo { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? AtividadeEmCasa { get; set; }

    [MaxLength(1000)]
    public string? ObservacaoResponsavel { get; set; }

    [Required(ErrorMessage = "Data de referência é obrigatória")]
    public DateTime DataReferencia { get; set; }

    public int? EventoOcorrenciaId { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    public List<CreateKidsConteudoAulaAnexoRequest> Anexos { get; set; } = new();
}

public class UpdateKidsConteudoAulaRequest
{
    [Required(ErrorMessage = "Título é obrigatório")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Tema { get; set; }

    [MaxLength(300)]
    public string? Versiculo { get; set; }

    [Required(ErrorMessage = "Resumo é obrigatório")]
    [MaxLength(4000)]
    public string Resumo { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? AtividadeEmCasa { get; set; }

    [MaxLength(1000)]
    public string? ObservacaoResponsavel { get; set; }

    [Required(ErrorMessage = "Data de referência é obrigatória")]
    public DateTime DataReferencia { get; set; }

    public int? EventoOcorrenciaId { get; set; }

    [MaxLength(50)]
    public string? SalaId { get; set; }

    [MaxLength(50)]
    public string? TurmaId { get; set; }

    public List<CreateKidsConteudoAulaAnexoRequest> Anexos { get; set; } = new();
}

public class KidsConteudoAulaAdminDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Tema { get; set; }
    public string? Versiculo { get; set; }
    public string Resumo { get; set; } = string.Empty;
    public string? AtividadeEmCasa { get; set; }
    public string? ObservacaoResponsavel { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DataReferencia { get; set; }
    public int? EventoOcorrenciaId { get; set; }
    public DateTime? EventoDataHoraInicio { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
    public DateTime? PublicadoEm { get; set; }
    public int? PublicadoPorPessoaId { get; set; }
    public string? PublicadoPorNome { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public List<KidsConteudoAulaAnexoDto> Anexos { get; set; } = new();
}

public class MeuConteudoAulaDto
{
    public int Id { get; set; }
    public int CriancaPessoaId { get; set; }
    public string CriancaNome { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Tema { get; set; }
    public string? Versiculo { get; set; }
    public string Resumo { get; set; } = string.Empty;
    public string? AtividadeEmCasa { get; set; }
    public string? ObservacaoResponsavel { get; set; }
    public DateTime DataReferencia { get; set; }
    public string? SalaId { get; set; }
    public string? TurmaId { get; set; }
    public DateTime? PublicadoEm { get; set; }
    public List<KidsConteudoAulaAnexoDto> Anexos { get; set; } = new();
}

// DTOs para Notificações
public class KidsNotificacaoDto
{
    public int Id { get; set; }
    public int? CriancaPessoaId { get; set; }
    public string? CriancaNome { get; set; }
    public int ResponsavelPessoaId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public DateTime? EnviadoEm { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LidoEm { get; set; }
    public bool FoiLido { get; set; }
    public int? CriadoByPessoaId { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class MeuAvisoKidsDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public int? CriancaPessoaId { get; set; }
    public string? CriancaNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public DateTime? LidoEm { get; set; }
    public bool FoiLido { get; set; }
}

public class CreateKidsAvisoRequest
{
    [Required(ErrorMessage = "Titulo é obrigatório")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mensagem é obrigatória")]
    [MaxLength(1000)]
    public string Mensagem { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Tipo { get; set; } = "AVISO_GERAL";

    [MaxLength(20)]
    public string Destino { get; set; } = "GERAL";

    public List<int> CriancaPessoaIds { get; set; } = new();
    public List<int> ResponsavelPessoaIds { get; set; } = new();
}

// Registro de token FCM para push
public class RegisterDeviceTokenRequest
{
    [Required(ErrorMessage = "Token é obrigatório")]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Platform { get; set; } = "Android"; // "Android" ou "iOS"
}

public class UnregisterDeviceTokenRequest
{
    [Required(ErrorMessage = "Token é obrigatório")]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
}
