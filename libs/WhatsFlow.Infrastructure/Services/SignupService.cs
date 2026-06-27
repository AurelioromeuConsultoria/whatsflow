using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Security;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class SignupService : ISignupService
{
    private const string PlanoPadraoSlug = "organizacao";
    private const int VerificacaoValidaHoras = 48;

    private readonly WhatsFlowDbContext _context;
    private readonly ITenantManagementService _tenantManagement;
    private readonly IBillingService _billing;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEmailService _emailService;
    private readonly PublicAppUrlSettings _appUrl;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SignupService> _logger;

    public SignupService(
        WhatsFlowDbContext context,
        ITenantManagementService tenantManagement,
        IBillingService billing,
        IUsuarioRepository usuarioRepository,
        IEmailService emailService,
        IOptions<PublicAppUrlSettings> appUrl,
        IOptions<EmailSettings> emailSettings,
        ILogger<SignupService> logger)
    {
        _context = context;
        _tenantManagement = tenantManagement;
        _billing = billing;
        _usuarioRepository = usuarioRepository;
        _emailService = emailService;
        _appUrl = appUrl.Value;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<SignupResultDto> SignupAsync(SignupDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        PasswordPolicy.Validar(dto.Senha);

        var jaExiste = await _usuarioRepository.GetByEmailAsync(email);
        if (jaExiste != null)
        {
            throw new InvalidOperationException("Este e-mail já está em uso.");
        }

        var slugPlano = string.IsNullOrWhiteSpace(dto.PlanoSlug) ? PlanoPadraoSlug : dto.PlanoSlug.Trim().ToLowerInvariant();
        var plano = await _context.Planos.FirstOrDefaultAsync(p => p.Slug == slugPlano && p.Ativo)
            ?? await _context.Planos.FirstOrDefaultAsync(p => p.Slug == PlanoPadraoSlug && p.Ativo)
            ?? throw new InvalidOperationException("Nenhum plano disponível para assinatura.");

        var slug = await GerarSlugUnicoAsync(dto.NomeIgreja);

        // 1) Provisiona tenant + admin INATIVOS (ativam só após confirmar o e-mail).
        var prov = await _tenantManagement.ProvisionAsync(new ProvisionTenantDto
        {
            Nome = dto.NomeIgreja.Trim(),
            Slug = slug,
            AdminNome = dto.AdminNome.Trim(),
            AdminEmail = email,
            AdminEmailLogin = email,
            AdminSenha = dto.Senha,
            AdminTelefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim(),
            AtivarImediatamente = false
        });

        var agora = DateTime.UtcNow;
        var token = Guid.NewGuid().ToString("N");

        // 2) Token de verificação.
        _context.Set<VerificacaoEmail>().Add(new VerificacaoEmail
        {
            TenantId = prov.Tenant.Id,
            UsuarioId = prov.UsuarioId,
            Token = token,
            ExpiraEm = agora.AddHours(VerificacaoValidaHoras),
            DataCriacao = agora
        });

        // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)

        await _context.SaveChangesAsync();

        // 4) Assinatura em trial.
        await _billing.AssinarAsync(new AssinarTenantDto
        {
            TenantId = prov.Tenant.Id,
            PlanoId = plano.Id,
            Ciclo = dto.Ciclo,
            NomeCliente = dto.NomeIgreja.Trim(),
            Email = email,
            Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim()
        });

        // 5) E-mail de verificação (best-effort).
        var link = $"{_appUrl.ApiBaseUrl?.TrimEnd('/')}/api/signup/confirmar?token={token}";
        await EnviarVerificacaoAsync(email, dto.AdminNome.Trim(), link);

        return new SignupResultDto
        {
            Status = "pendente_verificacao",
            Email = email,
            Slug = slug,
            LinkConfirmacao = _emailSettings.Enabled ? null : link
        };
    }

    public async Task<ConfirmacaoEmailResultDto> ConfirmarAsync(string token)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var verificacao = await _context.Set<VerificacaoEmail>().FirstOrDefaultAsync(v => v.Token == token);
            if (verificacao == null)
            {
                return new ConfirmacaoEmailResultDto { Confirmado = false, Mensagem = "Link de confirmação inválido." };
            }
            if (verificacao.ConfirmadoEm != null)
            {
                return new ConfirmacaoEmailResultDto { Confirmado = true, Mensagem = "E-mail já confirmado. Você já pode entrar." };
            }
            if (verificacao.ExpiraEm < DateTime.UtcNow)
            {
                return new ConfirmacaoEmailResultDto { Confirmado = false, Mensagem = "Link de confirmação expirado." };
            }

            verificacao.ConfirmadoEm = DateTime.UtcNow;

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == verificacao.TenantId);
            if (tenant != null)
            {
                tenant.Ativo = true;
            }

            var usuario = await _context.Set<Usuario>().FirstOrDefaultAsync(u => u.Id == verificacao.UsuarioId);
            if (usuario != null)
            {
                usuario.Ativo = true;
            }

            await _context.SaveChangesAsync();
            return new ConfirmacaoEmailResultDto { Confirmado = true, Mensagem = "E-mail confirmado! Sua organização está ativa." };
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    private async Task<string> GerarSlugUnicoAsync(string nome)
    {
        var baseSlug = Slugify(nome);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "igreja";
        }

        var slug = baseSlug;
        var sufixo = 1;
        while (await _context.Tenants.AnyAsync(t => t.Slug == slug))
        {
            sufixo++;
            slug = $"{baseSlug}-{sufixo}";
        }
        return slug;
    }

    private static string Slugify(string texto)
    {
        var normalizado = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark)
            {
                continue; // remove acentos
            }
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (c is ' ' or '-' or '_')
            {
                sb.Append('-');
            }
        }

        var slug = sb.ToString();
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }
        return slug.Trim('-');
    }

    private async Task EnviarVerificacaoAsync(string email, string nome, string link)
    {
        try
        {
            await _emailService.SendAsync(new EmailMessage
            {
                To = email,
                Subject = "Confirme seu e-mail — Verbo+",
                HtmlBody = EmailTemplates.VerificacaoEmail(nome, link, VerificacaoValidaHoras)
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de verificação para {Email}.", email);
        }
    }
}
