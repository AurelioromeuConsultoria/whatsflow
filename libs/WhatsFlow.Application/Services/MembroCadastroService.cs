using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface IMembroCadastroService
{
    Task<CadastroMembroResultadoDto> CadastrarAsync(CadastroMembroDto dto, string? ipOrigem = null);
}

public class CadastroMembroResultadoDto
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int? PessoaId { get; set; }
    public List<string> Avisos { get; set; } = new();
    public CadastroMembroCanalResultado? WhatsApp { get; set; }
    public CadastroMembroCanalResultado? Email { get; set; }
}

public class MembroCadastroService : IMembroCadastroService
{
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IPessoaPerfilRepository _perfilRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICadastroMembroNotificationService _notificationService;
    private readonly IConsentimentoRegistroRepository? _consentimentoRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<MembroCadastroService> _logger;

    public MembroCadastroService(
        IPessoaRepository pessoaRepository,
        IPessoaPerfilRepository perfilRepository,
        IUnitOfWork unitOfWork,
        ICadastroMembroNotificationService notificationService,
        ILogger<MembroCadastroService> logger)
        : this(pessoaRepository, perfilRepository, unitOfWork, notificationService, new DefaultTenantContext(), logger)
    {
    }

    public MembroCadastroService(
        IPessoaRepository pessoaRepository,
        IPessoaPerfilRepository perfilRepository,
        IUnitOfWork unitOfWork,
        ICadastroMembroNotificationService notificationService,
        ITenantContext tenantContext,
        ILogger<MembroCadastroService> logger,
        IConsentimentoRegistroRepository? consentimentoRepository = null)
    {
        _pessoaRepository = pessoaRepository;
        _perfilRepository = perfilRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _consentimentoRepository = consentimentoRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CadastroMembroResultadoDto> CadastrarAsync(CadastroMembroDto dto, string? ipOrigem = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return new CadastroMembroResultadoDto { Sucesso = false, Mensagem = "Nome é obrigatório" };

        if (string.IsNullOrWhiteSpace(dto.AceiteTermosVersao))
            return new CadastroMembroResultadoDto { Sucesso = false, Mensagem = "É necessário aceitar os Termos de Uso e a Política de Privacidade" };

        var whatsAppNormalizado = NormalizarTelefone(dto.WhatsApp);
        if (string.IsNullOrWhiteSpace(whatsAppNormalizado) || whatsAppNormalizado.Length < 10)
            return new CadastroMembroResultadoDto { Sucesso = false, Mensagem = "WhatsApp inválido. Informe um número com DDD." };

        if (string.IsNullOrWhiteSpace(dto.Email))
            return new CadastroMembroResultadoDto { Sucesso = false, Mensagem = "Email é obrigatório" };
        if (!IsValidEmail(dto.Email))
            return new CadastroMembroResultadoDto { Sucesso = false, Mensagem = "Email inválido" };
        if (!dto.DataNascimento.HasValue)
            return new CadastroMembroResultadoDto { Sucesso = false, Mensagem = "Data de nascimento é obrigatória" };
        if (dto.DataNascimento.HasValue && dto.DataNascimento.Value.Date > DateTime.UtcNow.Date)
            return new CadastroMembroResultadoDto { Sucesso = false, Mensagem = "Data de nascimento não pode ser futura" };

        var pessoaId = 0;
        var pessoaJaExistia = false;
        var nomeDestino = dto.Nome.Trim();
        string? emailDestino = dto.Email.Trim();
        var whatsAppDestino = whatsAppNormalizado;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pessoa = await _pessoaRepository.GetByEmailAsync(dto.Email.Trim());

            if (pessoa == null)
            {
                pessoa = new Pessoa
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    Nome = dto.Nome.Trim(),
                    Email = dto.Email.Trim(),
                    WhatsApp = whatsAppNormalizado,
                    DataNascimento = dto.DataNascimento,
                    TipoPessoa = InferirTipoPessoa(dto.DataNascimento),
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };
                pessoa = await _pessoaRepository.CreateWithoutSaveAsync(pessoa);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                pessoaJaExistia = true;
                var atualizado = false;
                if (string.IsNullOrWhiteSpace(pessoa.Nome) && !string.IsNullOrWhiteSpace(dto.Nome))
                {
                    pessoa.Nome = dto.Nome.Trim();
                    atualizado = true;
                }
                if (string.IsNullOrWhiteSpace(pessoa.WhatsApp) && !string.IsNullOrWhiteSpace(whatsAppNormalizado))
                {
                    pessoa.WhatsApp = whatsAppNormalizado;
                    atualizado = true;
                }
                if (pessoa.DataNascimento == null && dto.DataNascimento.HasValue)
                {
                    pessoa.DataNascimento = dto.DataNascimento;
                    pessoa.TipoPessoa = InferirTipoPessoa(dto.DataNascimento);
                    atualizado = true;
                }
                if (atualizado)
                {
                    await _pessoaRepository.UpdateWithoutSaveAsync(pessoa);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            pessoaId = pessoa.Id;
            nomeDestino = pessoa.Nome;
            emailDestino = pessoa.Email;
            whatsAppDestino = pessoa.WhatsApp ?? whatsAppDestino;

            var perfilMembro = await _perfilRepository.GetPerfilAtivoAsync(pessoa.Id, PerfilPessoa.Membro);
            if (perfilMembro == null)
            {
                var novoPerfil = new PessoaPerfil
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    PessoaId = pessoa.Id,
                    Perfil = PerfilPessoa.Membro,
                    DataInicio = DateTime.UtcNow,
                    DataFim = null
                };
                await _perfilRepository.CreateWithoutSaveAsync(novoPerfil);
            }

            // Trilha de consentimento LGPD: registra o aceite dos Termos e da Política de Privacidade.
            if (_consentimentoRepository != null && !string.IsNullOrWhiteSpace(dto.AceiteTermosVersao))
            {
                var versao = dto.AceiteTermosVersao.Trim();
                var tenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;

                foreach (var tipo in new[] { TipoConsentimento.PoliticaPrivacidade, TipoConsentimento.TermosDeUso })
                {
                    await _consentimentoRepository.CreateWithoutSaveAsync(new ConsentimentoRegistro
                    {
                        TenantId = tenantId,
                        PessoaId = pessoa.Id,
                        Tipo = tipo,
                        VersaoDocumento = versao,
                        AceitoEm = DateTime.UtcNow,
                        IpOrigem = ipOrigem,
                        Origem = "cadastro_publico",
                        ConcedidoPorPessoaId = pessoa.Id
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
        });

        CadastroMembroNotificationResult? notificationResult = null;

        try
        {
            notificationResult = await _notificationService.NotifySuccessAsync(new CadastroMembroNotification
            {
                Nome = nomeDestino,
                Email = emailDestino,
                WhatsApp = whatsAppDestino
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha inesperada ao disparar notificações do cadastro público para a pessoa {PessoaId}", pessoaId);
        }

        return new CadastroMembroResultadoDto
        {
            Sucesso = true,
            Mensagem = pessoaJaExistia
                ? "Cadastro vinculado a uma pessoa já existente e atualizado com sucesso."
                : "Cadastro realizado com sucesso!",
            PessoaId = pessoaId,
            Avisos = notificationResult?.GetAvisos() ?? new List<string>(),
            WhatsApp = notificationResult?.WhatsApp,
            Email = notificationResult?.Email
        };
    }

    private static TipoPessoa InferirTipoPessoa(DateTime? dataNascimento)
    {
        if (!dataNascimento.HasValue)
            return TipoPessoa.Adulto;

        var hoje = DateTime.UtcNow.Date;
        var nascimento = dataNascimento.Value.Date;
        var idade = hoje.Year - nascimento.Year;

        if (nascimento > hoje.AddYears(-idade))
            idade--;

        return idade < 18 ? TipoPessoa.Crianca : TipoPessoa.Adulto;
    }

    private static string NormalizarTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return string.Empty;
        return new string(telefone.Where(char.IsDigit).ToArray());
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
}
