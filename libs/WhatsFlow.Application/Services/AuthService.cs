using System.IdentityModel.Tokens.Jwt;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Security;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
    Task<UsuarioDto> GetUsuarioLogadoAsync(int usuarioId);
    Task AlterarSenhaAsync(int usuarioId, AlterarSenhaDto dto);
}

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditLogService _auditLogService;
    private static readonly ConcurrentDictionary<string, string> _refreshTokens = new(); // Em produção, usar Redis ou banco

    public AuthService(IUsuarioRepository usuarioRepository, IConfiguration configuration, ILogger<AuthService> logger, IAuditLogService auditLogService)
    {
        _usuarioRepository = usuarioRepository;
        _configuration = configuration;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var tenantSlug = string.IsNullOrWhiteSpace(dto.TenantSlug)
            ? null
            : dto.TenantSlug.Trim();
        var usuario = await _usuarioRepository.GetByEmailAsync(dto.Email, tenantSlug);

        // Lockout (anti força-bruta), parametrizável via config "LoginLockout".
        var lockoutHabilitado = !string.Equals(_configuration["LoginLockout:Habilitado"], "false", StringComparison.OrdinalIgnoreCase);
        var maxTentativas = int.TryParse(_configuration["LoginLockout:MaxTentativas"], out var mt) ? mt : 5;
        var bloqueioMinutos = int.TryParse(_configuration["LoginLockout:BloqueioMinutos"], out var bm) ? bm : 15;

        if (lockoutHabilitado && usuario?.BloqueadoAte is { } bloqueadoAte && bloqueadoAte > DateTime.UtcNow)
        {
            _logger.LogWarning("Login bloqueado por excesso de tentativas. UsuarioId={UsuarioId}", usuario.Id);
            throw new UnauthorizedAccessException("Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde.");
        }

        var senhaValida = usuario != null && BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash);
        if (!senhaValida)
        {
            if (lockoutHabilitado && usuario != null)
            {
                await RegistrarFalhaLoginAsync(usuario, maxTentativas, bloqueioMinutos);
            }
            _logger.LogWarning("Falha de login. Email={Email} TenantSlug={TenantSlug}", dto.Email, tenantSlug);
            throw new UnauthorizedAccessException("Email ou senha inválidos");
        }

        // Tenant inativo = signup self-service ainda não confirmado por e-mail.
        if (usuario.Tenant is { Ativo: false })
        {
            _logger.LogWarning("Login bloqueado: tenant não ativado. UsuarioId={UsuarioId}", usuario.Id);
            throw new UnauthorizedAccessException("Confirme seu e-mail para ativar o acesso da sua organização.");
        }

        if (!usuario.Ativo)
        {
            _logger.LogWarning("Tentativa de login com usuário inativo. UsuarioId={UsuarioId} Email={EmailLogin}", usuario.Id, usuario.EmailLogin);
            throw new UnauthorizedAccessException("Usuário inativo");
        }

        // Login bem-sucedido: zera o lockout.
        if (usuario.TentativasLoginFalhas != 0 || usuario.BloqueadoAte != null)
        {
            usuario.TentativasLoginFalhas = 0;
            usuario.BloqueadoAte = null;
        }

        // Atualizar último acesso
        usuario.UltimoAcesso = DateTime.Now;
        await _usuarioRepository.UpdateAsync(usuario);

        var token = GenerateJwtToken(usuario);
        var refreshToken = GenerateRefreshToken();

        // Armazenar refresh token (em produção, usar banco ou Redis)
        _refreshTokens[refreshToken] = usuario.Id.ToString();

        _logger.LogInformation(
            "Login realizado com sucesso. TenantId={TenantId} UsuarioId={UsuarioId} PessoaId={PessoaId} TipoUsuario={TipoUsuario}",
            usuario.TenantId,
            usuario.Id,
            usuario.PessoaId,
            usuario.TipoUsuario);
        await _auditLogService.RecordAsync(
            "Auth",
            usuario.Id.ToString(),
            "Login",
            new { usuario.Id, usuario.PessoaId, usuario.TipoUsuario });

        return new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600, // 1 hora
            Usuario = MapToUsuarioDto(usuario)
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var usuarioIdStr))
        {
            _logger.LogWarning("Refresh token inválido.");
            throw new UnauthorizedAccessException("Refresh token inválido");
        }

        var usuarioId = int.Parse(usuarioIdStr);
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);

        if (usuario == null || !usuario.Ativo)
        {
            _refreshTokens.TryRemove(refreshToken, out _);
            _logger.LogWarning("Refresh token rejeitado por usuário ausente ou inativo. UsuarioId={UsuarioId}", usuarioId);
            throw new UnauthorizedAccessException("Usuário não encontrado ou inativo");
        }

        var newToken = GenerateJwtToken(usuario);
        var newRefreshToken = GenerateRefreshToken();

        // Remover token antigo e adicionar novo
        _refreshTokens.TryRemove(refreshToken, out _);
        _refreshTokens[newRefreshToken] = usuarioId.ToString();

        _logger.LogInformation("Refresh token renovado com sucesso. UsuarioId={UsuarioId}", usuario.Id);
        await _auditLogService.RecordAsync(
            "Auth",
            usuario.Id.ToString(),
            "RefreshToken",
            new { usuario.Id });

        return new LoginResponseDto
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600,
            Usuario = MapToUsuarioDto(usuario)
        };
    }

    public async Task<UsuarioDto> GetUsuarioLogadoAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario == null) throw new ArgumentException("Usuário não encontrado");

        return MapToUsuarioDto(usuario);
    }

    public async Task AlterarSenhaAsync(int usuarioId, AlterarSenhaDto dto)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario == null) throw new ArgumentException("Usuário não encontrado");

        if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.SenhaHash))
        {
            _logger.LogWarning("Falha ao alterar senha por senha atual inválida. UsuarioId={UsuarioId}", usuarioId);
            throw new UnauthorizedAccessException("Senha atual incorreta");
        }

        PasswordPolicy.Validar(dto.NovaSenha);

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
        await _usuarioRepository.UpdateAsync(usuario);
        _logger.LogInformation("Senha alterada com sucesso. UsuarioId={UsuarioId}", usuarioId);
        await _auditLogService.RecordAsync(
            "Usuario",
            usuarioId.ToString(),
            "AlterarSenha",
            new { UsuarioId = usuarioId });
    }

    private async Task RegistrarFalhaLoginAsync(Usuario usuario, int maxTentativas, int bloqueioMinutos)
    {
        var agora = DateTime.UtcNow;

        // Se havia um bloqueio que já expirou, recomeça a contagem do zero.
        if (usuario.BloqueadoAte is { } ate && ate <= agora)
        {
            usuario.TentativasLoginFalhas = 0;
            usuario.BloqueadoAte = null;
        }

        usuario.TentativasLoginFalhas++;

        if (usuario.TentativasLoginFalhas >= maxTentativas)
        {
            usuario.BloqueadoAte = agora.AddMinutes(bloqueioMinutos);
            usuario.TentativasLoginFalhas = 0; // o bloqueio passa a governar
            _logger.LogWarning("Conta bloqueada por {Minutos}min após {Max} tentativas. UsuarioId={UsuarioId}",
                bloqueioMinutos, maxTentativas, usuario.Id);
        }

        await _usuarioRepository.UpdateAsync(usuario);
    }

    private string GenerateJwtToken(Usuario usuario)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "sua-chave-secreta-super-segura-com-pelo-menos-32-caracteres");
        var issuer = _configuration["Jwt:Issuer"] ?? "WhatsFlow";
        var audience = _configuration["Jwt:Audience"] ?? "WhatsFlow";

        var nome = usuario.Pessoa?.Nome ?? string.Empty;
        var email = usuario.Pessoa?.Email ?? usuario.EmailLogin;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email),
            new Claim("TenantId", usuario.TenantId.ToString()),
            new Claim("TenantSlug", usuario.Tenant?.Slug ?? Tenant.InitialTenantSlug),
            new Claim("TenantNome", usuario.Tenant?.Nome ?? Tenant.InitialTenantName),
            new Claim("TenantNomeExibicao", usuario.Tenant?.NomeExibicao ?? string.Empty),
            new Claim("TenantLogoUrl", usuario.Tenant?.LogoUrl ?? string.Empty),
            new Claim("TenantFaviconUrl", usuario.Tenant?.FaviconUrl ?? string.Empty),
            new Claim("TenantCorPrimaria", usuario.Tenant?.CorPrimaria ?? string.Empty),
            new Claim("TenantCorSecundaria", usuario.Tenant?.CorSecundaria ?? string.Empty),
            new Claim("IsRootTenant", usuario.Tenant?.IsRootTenant.ToString().ToLowerInvariant() ?? "false"),
            new Claim("TipoUsuario", usuario.TipoUsuario.ToString()),
            new Claim("TipoUsuarioId", ((int)usuario.TipoUsuario).ToString()),
            new Claim("IsPlatformAdmin", IsPlatformAdmin(usuario).ToString().ToLowerInvariant())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static UsuarioDto MapToUsuarioDto(Usuario u)
    {
        return new UsuarioDto
        {
            Id = u.Id,
            TenantId = u.TenantId,
            TenantSlug = u.Tenant?.Slug ?? Tenant.InitialTenantSlug,
            TenantNome = u.Tenant?.Nome ?? Tenant.InitialTenantName,
            TenantNomeExibicao = u.Tenant?.NomeExibicao,
            TenantLogoUrl = u.Tenant?.LogoUrl,
            TenantFaviconUrl = u.Tenant?.FaviconUrl,
            TenantCorPrimaria = u.Tenant?.CorPrimaria,
            TenantCorSecundaria = u.Tenant?.CorSecundaria,
            IsRootTenant = u.Tenant?.IsRootTenant ?? false,
            IsPlatformAdmin = IsPlatformAdmin(u),
            PessoaId = u.PessoaId,
            Nome = u.Pessoa?.Nome ?? string.Empty,
            Email = u.Pessoa?.Email ?? string.Empty,
            EmailLogin = u.EmailLogin,
            TipoUsuario = u.TipoUsuario,
            TipoUsuarioDescricao = GetTipoUsuarioDescricao(u.TipoUsuario),
            Ativo = u.Ativo,
            DataCriacao = u.DataCriacao,
            UltimoAcesso = u.UltimoAcesso,
            PerfilAcessoId = u.PerfilAcessoId,
            PerfilAcessoNome = u.PerfilAcesso?.Nome,
            Permissoes = u.PerfilAcesso?.Permissoes.Select(p => new PermissaoPerfilDto
            {
                Id = p.Id,
                Recurso = p.Recurso,
                PodeVer = p.PodeVer,
                PodeEditar = p.PodeEditar,
                PodeExcluir = p.PodeExcluir
            }).ToList() ?? new List<PermissaoPerfilDto>()
        };
    }

    private static string GetTipoUsuarioDescricao(TipoUsuario tipo)
    {
        return tipo switch
        {
            TipoUsuario.Admin => "Administrador",
            TipoUsuario.Portal => "Portal",
            TipoUsuario.Ambos => "Administrador e Portal",
            _ => "Desconhecido"
        };
    }

    private static bool IsPlatformAdmin(Usuario usuario)
    {
        return usuario.IsPlatformAdmin;
    }
}
