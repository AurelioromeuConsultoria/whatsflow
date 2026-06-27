using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Security;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class KidsRegistrationService : IKidsRegistrationService
{
    private readonly WhatsFlowDbContext _context;
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KidsRegistrationService> _logger;

    public KidsRegistrationService(
        WhatsFlowDbContext context,
        IAuthService authService,
        IUnitOfWork unitOfWork,
        ILogger<KidsRegistrationService> logger)
    {
        _context = context;
        _authService = authService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginResponseDto> RegistrarResponsavelAsync(RegistrarResponsavelDto dto)
    {
        PasswordPolicy.Validar(dto.Senha);

        var slug = dto.TenantSlug.Trim().ToLowerInvariant();
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug && t.Ativo)
            ?? throw new ArgumentException($"Organização não encontrada: '{dto.TenantSlug}'");

        var emailNorm = dto.Email.Trim().ToLowerInvariant();

        var emailEmUso = await _context.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.EmailLogin.ToLower() == emailNorm && u.TenantId == tenant.Id);
        if (emailEmUso)
            throw new InvalidOperationException("Este e-mail já está cadastrado nesta organização.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var pessoa = new Pessoa
            {
                TenantId = tenant.Id,
                Nome = dto.Nome.Trim(),
                Email = emailNorm,
                Telefone = dto.Telefone?.Trim(),
                WhatsApp = dto.WhatsApp?.Trim(),
                TipoPessoa = TipoPessoa.Adulto,
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };
            _context.Pessoas.Add(pessoa);
            await _context.SaveChangesAsync();

            var usuario = new Usuario
            {
                TenantId = tenant.Id,
                PessoaId = pessoa.Id,
                EmailLogin = emailNorm,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                TipoUsuario = TipoUsuario.Portal,
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Responsável registrado. TenantId={TenantId} PessoaId={PessoaId}",
                tenant.Id, pessoa.Id);
        });

        // Auto-login após registro
        return await _authService.LoginAsync(new LoginDto
        {
            Email = emailNorm,
            Senha = dto.Senha,
            TenantSlug = slug
        });
    }
}
