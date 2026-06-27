using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class TenantManagementService : ITenantManagementService
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly string[] DefaultAdminResources =
    [
        "dashboard",
        "usuarios",
        "perfis-acesso",
        "pessoas",
        "perfis",
        "visitantes",
        "configuracoes-mensagens",
        "mensagens-agendadas",
        "comunicacao",
        "equipes",
        "cargos",
        "voluntarios",
        "eventos",
        "inscricoes-eventos",
        "portal",
        "noticias",
        "categorias-noticias",
        "contatos",
        "destaques-site",
        "categorias-midias",
        "galerias-fotos",
        "enquetes",
        "kids",
        "hub",
        "financeiro",
        "fornecedores"
    ];

    private readonly WhatsFlowDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantManagementService> _logger;
    private readonly IAuditLogService _auditLogService;

    public TenantManagementService(
        WhatsFlowDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<TenantManagementService> logger,
        IAuditLogService auditLogService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<IEnumerable<TenantDto>> GetAllAsync()
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .Include(t => t.Domains)
            .OrderBy(t => t.Nome)
            .ToListAsync();

        var tenantIds = tenants.Select(t => t.Id).ToList();

        var usuarioCounts = await _context.Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => tenantIds.Contains(u.TenantId))
            .GroupBy(u => u.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                TotalUsuarios = g.Count(),
                TotalAdministradores = g.Count(u => u.TipoUsuario == TipoUsuario.Admin || u.TipoUsuario == TipoUsuario.Ambos)
            })
            .ToListAsync();

        var pessoaCounts = await _context.Pessoas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => tenantIds.Contains(p.TenantId))
            .GroupBy(p => p.TenantId)
            .Select(g => new { TenantId = g.Key, TotalPessoas = g.Count() })
            .ToListAsync();

        var activity = await _context.AuditLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(log => tenantIds.Contains(log.TenantId))
            .GroupBy(log => log.TenantId)
            .Select(g => new { TenantId = g.Key, UltimaAtividadeEm = g.Max(x => x.CreatedAt) })
            .ToListAsync();

        var usuarioLookup = usuarioCounts.ToDictionary(x => x.TenantId);
        var pessoaLookup = pessoaCounts.ToDictionary(x => x.TenantId);
        var activityLookup = activity.ToDictionary(x => x.TenantId);

        return tenants.Select(tenant =>
        {
            var totalUsuarios = usuarioLookup.TryGetValue(tenant.Id, out var userStats) ? userStats.TotalUsuarios : 0;
            var totalAdministradores = usuarioLookup.TryGetValue(tenant.Id, out userStats) ? userStats.TotalAdministradores : 0;
            var totalPessoas = pessoaLookup.TryGetValue(tenant.Id, out var peopleStats) ? peopleStats.TotalPessoas : 0;
            var ultimaAtividadeEm = activityLookup.TryGetValue(tenant.Id, out var activityStats)
                ? (DateTime?)activityStats.UltimaAtividadeEm
                : null;

            return MapToDto(
                tenant,
                totalUsuarios: totalUsuarios,
                totalAdministradores: totalAdministradores,
                totalPessoas: totalPessoas,
                ultimaAtividadeEm: ultimaAtividadeEm);
        });
    }

    public async Task<TenantDto?> GetByIdAsync(int id)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Include(t => t.Domains)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant is null)
        {
            return null;
        }

        var metrics = await GetTenantMetricsAsync(id);
        return MapToDto(
            tenant,
            totalUsuarios: metrics.TotalUsuarios,
            totalAdministradores: metrics.TotalAdministradores,
            totalPessoas: metrics.TotalPessoas,
            ultimaAtividadeEm: metrics.UltimaAtividadeEm);
    }

    public async Task<ProvisionTenantResultDto> ProvisionAsync(ProvisionTenantDto dto)
    {
        ValidateProvisioningRequest(dto);

        var normalizedSlug = dto.Slug.Trim().ToLowerInvariant();
        var normalizedDomain = string.IsNullOrWhiteSpace(dto.DominioPrimario)
            ? null
            : dto.DominioPrimario.Trim().ToLowerInvariant();

        if (await _context.Tenants.AnyAsync(t => t.Slug == normalizedSlug))
        {
            throw new ArgumentException("Já existe um tenant com este slug.");
        }

        if (normalizedDomain is not null && await _context.TenantDomains.AnyAsync(d => d.Domain == normalizedDomain))
        {
            throw new ArgumentException("Já existe um tenant usando este domínio.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var tenant = new Tenant
            {
                Nome = dto.Nome.Trim(),
                NomeExibicao = NormalizeOptional(dto.NomeExibicao) ?? dto.Nome.Trim(),
                Slug = normalizedSlug,
                LogoUrl = NormalizeOptional(dto.LogoUrl),
                FaviconUrl = NormalizeOptional(dto.FaviconUrl),
                CorPrimaria = NormalizeColor(dto.CorPrimaria),
                CorSecundaria = NormalizeColor(dto.CorSecundaria),
                IsRootTenant = false,
                Ativo = dto.AtivarImediatamente,
                DataCriacao = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            if (normalizedDomain is not null)
            {
                _context.TenantDomains.Add(new TenantDomain
                {
                    TenantId = tenant.Id,
                    Domain = normalizedDomain,
                    IsPrimary = true,
                    Ativo = true
                });
            }

            var perfilAdmin = new PerfilAcesso
            {
                TenantId = tenant.Id,
                Nome = "Administrador",
                Descricao = "Perfil administrativo padrão do tenant.",
                DataCriacao = DateTime.Now,
                Permissoes = DefaultAdminResources
                    .Select(resource => new PerfilAcessoPermissao
                    {
                        TenantId = tenant.Id,
                        Recurso = resource,
                        PodeVer = true,
                        PodeEditar = true,
                        PodeExcluir = true
                    })
                    .ToList()
            };

            _context.PerfisAcesso.Add(perfilAdmin);

            var pessoaAdmin = new Pessoa
            {
                TenantId = tenant.Id,
                Nome = dto.AdminNome.Trim(),
                Email = dto.AdminEmail.Trim(),
                Telefone = string.IsNullOrWhiteSpace(dto.AdminTelefone) ? null : dto.AdminTelefone.Trim(),
                WhatsApp = string.IsNullOrWhiteSpace(dto.AdminWhatsApp) ? null : dto.AdminWhatsApp.Trim(),
                TipoPessoa = TipoPessoa.Adulto,
                Ativo = true,
                DataCriacao = DateTime.Now
            };

            _context.Pessoas.Add(pessoaAdmin);
            await _context.SaveChangesAsync();

            var usuarioAdmin = new Usuario
            {
                TenantId = tenant.Id,
                PessoaId = pessoaAdmin.Id,
                EmailLogin = dto.AdminEmailLogin.Trim(),
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminSenha),
                TipoUsuario = TipoUsuario.Admin,
                IsPlatformAdmin = false,
                Ativo = dto.AtivarImediatamente,
                PerfilAcessoId = perfilAdmin.Id,
                DataCriacao = DateTime.Now
            };

            _context.Usuarios.Add(usuarioAdmin);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tenant provisionado. TenantId={TenantId} Slug={TenantSlug} UsuarioAdminId={UsuarioAdminId}",
                tenant.Id,
                tenant.Slug,
                usuarioAdmin.Id);
            await _auditLogService.RecordAsync(
                "Tenant",
                tenant.Id.ToString(),
                "ProvisionarTenant",
                new
                {
                    tenant.Id,
                    tenant.Nome,
                    tenant.NomeExibicao,
                    tenant.Slug,
                    DominioPrimario = normalizedDomain,
                    tenant.Ativo,
                    UsuarioAdminId = usuarioAdmin.Id,
                    PessoaAdminId = pessoaAdmin.Id,
                    PerfilAcessoId = perfilAdmin.Id
                });

            return new ProvisionTenantResultDto
            {
                Tenant = MapToDto(tenant, normalizedDomain),
                PerfilAcessoId = perfilAdmin.Id,
                PessoaId = pessoaAdmin.Id,
                UsuarioId = usuarioAdmin.Id
            };
        });
    }

    public async Task<TenantDto> UpdateAsync(int id, AtualizarTenantDto dto)
    {
        ValidateUpdateRequest(dto);

        var tenant = await _context.Tenants
            .Include(t => t.Domains)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant is null)
        {
            throw new ArgumentException("Tenant não encontrado.");
        }

        var normalizedSlug = dto.Slug.Trim().ToLowerInvariant();
        var normalizedDomain = string.IsNullOrWhiteSpace(dto.DominioPrimario)
            ? null
            : dto.DominioPrimario.Trim().ToLowerInvariant();
        var previousState = new
        {
            tenant.Nome,
            tenant.NomeExibicao,
            tenant.Slug,
            DominioPrimario = tenant.Domains.FirstOrDefault(d => d.IsPrimary && d.Ativo)?.Domain
                ?? tenant.Domains.FirstOrDefault(d => d.Ativo)?.Domain,
            tenant.LogoUrl,
            tenant.FaviconUrl,
            tenant.CorPrimaria,
            tenant.CorSecundaria
        };

        if (await _context.Tenants.AnyAsync(t => t.Id != id && t.Slug == normalizedSlug))
        {
            throw new ArgumentException("Já existe outro tenant com este slug.");
        }

        if (normalizedDomain is not null && await _context.TenantDomains.AnyAsync(d => d.TenantId != id && d.Domain == normalizedDomain))
        {
            throw new ArgumentException("Já existe outro tenant usando este domínio.");
        }

        tenant.Nome = dto.Nome.Trim();
        tenant.NomeExibicao = NormalizeOptional(dto.NomeExibicao) ?? dto.Nome.Trim();
        tenant.Slug = normalizedSlug;
        tenant.LogoUrl = NormalizeOptional(dto.LogoUrl);
        tenant.FaviconUrl = NormalizeOptional(dto.FaviconUrl);
        tenant.CorPrimaria = NormalizeColor(dto.CorPrimaria);
        tenant.CorSecundaria = NormalizeColor(dto.CorSecundaria);

        var existingPrimaryDomain = tenant.Domains.FirstOrDefault(d => d.IsPrimary);
        if (normalizedDomain is null)
        {
            if (existingPrimaryDomain is not null)
            {
                _context.TenantDomains.Remove(existingPrimaryDomain);
            }
        }
        else if (existingPrimaryDomain is null)
        {
            _context.TenantDomains.Add(new TenantDomain
            {
                TenantId = tenant.Id,
                Domain = normalizedDomain,
                IsPrimary = true,
                Ativo = true
            });
        }
        else
        {
            existingPrimaryDomain.Domain = normalizedDomain;
            existingPrimaryDomain.Ativo = true;
            existingPrimaryDomain.IsPrimary = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Tenant atualizado. TenantId={TenantId} Slug={TenantSlug}",
            tenant.Id,
            tenant.Slug);
        await _auditLogService.RecordAsync(
            "Tenant",
            tenant.Id.ToString(),
            "AtualizarTenant",
            new
            {
                Antes = previousState,
                Depois = new
                {
                    tenant.Nome,
                    tenant.NomeExibicao,
                    tenant.Slug,
                    DominioPrimario = normalizedDomain,
                    tenant.LogoUrl,
                    tenant.FaviconUrl,
                    tenant.CorPrimaria,
                    tenant.CorSecundaria
                }
            });

        return MapToDto(tenant, normalizedDomain);
    }

    public async Task<TenantDto> UpdateStatusAsync(int id, AtualizarTenantStatusDto dto)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Domains)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant is null)
        {
            throw new ArgumentException("Tenant não encontrado.");
        }

        if (tenant.IsRootTenant && !dto.Ativo)
        {
            throw new ArgumentException("O tenant inicial Mang Guarulhos não pode ser desativado.");
        }

        tenant.Ativo = dto.Ativo;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Status do tenant atualizado. TenantId={TenantId} Ativo={Ativo}",
            tenant.Id,
            tenant.Ativo);
        await _auditLogService.RecordAsync(
            "Tenant",
            tenant.Id.ToString(),
            dto.Ativo ? "AtivarTenant" : "InativarTenant",
            new
            {
                tenant.Id,
                tenant.Nome,
                tenant.Slug,
                tenant.Ativo
            });

        return MapToDto(tenant);
    }

    public async Task DeleteAsync(int id)
    {
        var previousIgnoreTenantFilters = _context.IgnoreTenantFilters;
        TenantDeletionAuditSnapshot? tenantSnapshot = null;
        _context.IgnoreTenantFilters = true;

        try
        {
            var tenant = await _context.Tenants
                .Include(t => t.Domains)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant is null)
            {
                throw new ArgumentException("Tenant não encontrado.");
            }

            if (tenant.IsRootTenant)
            {
                throw new ArgumentException("O tenant inicial Mang Guarulhos não pode ser excluído.");
            }

            tenantSnapshot = new TenantDeletionAuditSnapshot
            {
                TenantId = tenant.Id,
                Nome = tenant.Nome,
                NomeExibicao = tenant.NomeExibicao,
                Slug = tenant.Slug,
                Ativo = tenant.Ativo,
                IsRootTenant = tenant.IsRootTenant,
                DominioPrimario = tenant.Domains.FirstOrDefault(d => d.IsPrimary && d.Ativo)?.Domain
                    ?? tenant.Domains.FirstOrDefault(d => d.Ativo)?.Domain
            };

            var hasOperationalData = await HasOperationalDataAsync(id);
            if (hasOperationalData)
            {
                throw new ArgumentException("O tenant possui dados operacionais e não pode ser excluído. Inative-o em vez disso.");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var auditLogs = await _context.AuditLogs.Where(x => x.TenantId == id).ToListAsync();
                var usuarios = await _context.Usuarios.Where(x => x.TenantId == id).ToListAsync();
                var perfis = await _context.PerfisAcesso
                    .Include(x => x.Permissoes)
                    .Where(x => x.TenantId == id)
                    .ToListAsync();
                var pessoas = await _context.Pessoas.Where(x => x.TenantId == id).ToListAsync();
                var domains = await _context.TenantDomains.Where(x => x.TenantId == id).ToListAsync();

                if (auditLogs.Count > 0) _context.AuditLogs.RemoveRange(auditLogs);
                if (usuarios.Count > 0) _context.Usuarios.RemoveRange(usuarios);
                await _context.SaveChangesAsync();

                var permissoes = perfis.SelectMany(x => x.Permissoes).ToList();
                if (permissoes.Count > 0) _context.PerfisAcessoPermissoes.RemoveRange(permissoes);
                if (perfis.Count > 0) _context.PerfisAcesso.RemoveRange(perfis);
                await _context.SaveChangesAsync();

                if (pessoas.Count > 0) _context.Pessoas.RemoveRange(pessoas);
                if (domains.Count > 0) _context.TenantDomains.RemoveRange(domains);
                await _context.SaveChangesAsync();

                // Clear tracked references before removing the tenant itself to avoid
                // EF trying to sever required relationships in a single change set.
                _context.ChangeTracker.Clear();

                var tenantToDelete = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
                if (tenantToDelete is not null)
                {
                    _context.Tenants.Remove(tenantToDelete);
                    await _context.SaveChangesAsync();
                }
            });
        }
        finally
        {
            _context.IgnoreTenantFilters = previousIgnoreTenantFilters;
        }

        _logger.LogInformation("Tenant excluído. TenantId={TenantId}", id);
        await _auditLogService.RecordAsync(
            "Tenant",
            id.ToString(),
            "ExcluirTenant",
            new
            {
                TenantId = tenantSnapshot.TenantId,
                tenantSnapshot.Nome,
                tenantSnapshot.NomeExibicao,
                tenantSnapshot.Slug,
                tenantSnapshot.Ativo,
                tenantSnapshot.IsRootTenant,
                tenantSnapshot.DominioPrimario
            });
    }

    private static void ValidateProvisioningRequest(ProvisionTenantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome do tenant é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.Slug))
        {
            throw new ArgumentException("Slug do tenant é obrigatório.");
        }

        var normalizedSlug = dto.Slug.Trim().ToLowerInvariant();
        if (!SlugRegex.IsMatch(normalizedSlug))
        {
            throw new ArgumentException("Slug inválido. Use apenas letras minúsculas, números e hífens.");
        }

        if (string.IsNullOrWhiteSpace(dto.AdminNome))
        {
            throw new ArgumentException("Nome do administrador inicial é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.AdminEmail))
        {
            throw new ArgumentException("Email do administrador inicial é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.AdminEmailLogin))
        {
            throw new ArgumentException("Email de login do administrador inicial é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.AdminSenha) || dto.AdminSenha.Length < 6)
        {
            throw new ArgumentException("A senha inicial deve ter pelo menos 6 caracteres.");
        }

        ValidateBranding(dto.CorPrimaria, dto.CorSecundaria);
    }

    private static void ValidateUpdateRequest(AtualizarTenantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Nome do tenant é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.Slug))
        {
            throw new ArgumentException("Slug do tenant é obrigatório.");
        }

        var normalizedSlug = dto.Slug.Trim().ToLowerInvariant();
        if (!SlugRegex.IsMatch(normalizedSlug))
        {
            throw new ArgumentException("Slug inválido. Use apenas letras minúsculas, números e hífens.");
        }

        ValidateBranding(dto.CorPrimaria, dto.CorSecundaria);
    }

    private async Task<bool> HasOperationalDataAsync(int tenantId)
    {
        // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
        return await _context.Visitantes.AnyAsync(x => x.TenantId == tenantId)
            || await _context.ConfiguracoesMensagens.AnyAsync(x => x.TenantId == tenantId)
            || await _context.MensagensAgendadas.AnyAsync(x => x.TenantId == tenantId)
            || await _context.Contatos.AnyAsync(x => x.TenantId == tenantId);
    }

    private static void ValidateBranding(string? corPrimaria, string? corSecundaria)
    {
        if (!IsValidColor(corPrimaria))
        {
            throw new ArgumentException("Cor primária inválida. Use formato hexadecimal como #111827.");
        }

        if (!IsValidColor(corSecundaria))
        {
            throw new ArgumentException("Cor secundária inválida. Use formato hexadecimal como #374151.");
        }
    }

    private static bool IsValidColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return true;
        }

        return Regex.IsMatch(color.Trim(), "^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{3})$");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeColor(string? color)
        => string.IsNullOrWhiteSpace(color) ? null : color.Trim().ToUpperInvariant();

    private async Task<TenantMetrics> GetTenantMetricsAsync(int tenantId)
    {
        var usuarioStats = await _context.Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .GroupBy(u => u.TenantId)
            .Select(g => new
            {
                TotalUsuarios = g.Count(),
                TotalAdministradores = g.Count(u => u.TipoUsuario == TipoUsuario.Admin || u.TipoUsuario == TipoUsuario.Ambos)
            })
            .FirstOrDefaultAsync();

        var totalPessoas = await _context.Pessoas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .CountAsync();

        var ultimaAtividadeEm = await _context.AuditLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(log => log.TenantId == tenantId)
            .MaxAsync(log => (DateTime?)log.CreatedAt);

        return new TenantMetrics(
            usuarioStats?.TotalUsuarios ?? 0,
            usuarioStats?.TotalAdministradores ?? 0,
            totalPessoas,
            ultimaAtividadeEm);
    }

    private static TenantOperationalSnapshot BuildOperationalSnapshot(
        Tenant tenant,
        string? primaryDomain,
        int totalUsuarios,
        int totalAdministradores,
        int totalPessoas)
    {
        var identidadeOk = !string.IsNullOrWhiteSpace(tenant.Nome) && !string.IsNullOrWhiteSpace(tenant.Slug);
        var brandingOk = !string.IsNullOrWhiteSpace(tenant.CorPrimaria) && !string.IsNullOrWhiteSpace(tenant.CorSecundaria);
        var dominioOk = !string.IsNullOrWhiteSpace(primaryDomain);
        var adminOk = totalAdministradores > 0;
        var baseOperacionalOk = totalUsuarios > 0 && totalPessoas > 0;

        var concluidos = 0;
        if (identidadeOk) concluidos++;
        if (brandingOk) concluidos++;
        if (dominioOk) concluidos++;
        if (adminOk) concluidos++;
        if (baseOperacionalOk) concluidos++;

        const int total = 5;
        var percentual = total == 0 ? 0 : (int)Math.Round((double)concluidos / total * 100);

        var snapshot = new TenantOperationalSnapshot
        {
            OnboardingIdentidadeOk = identidadeOk,
            OnboardingBrandingOk = brandingOk,
            OnboardingDominioOk = dominioOk,
            OnboardingAdminOk = adminOk,
            OnboardingBaseOperacionalOk = baseOperacionalOk,
            OnboardingConcluidos = concluidos,
            OnboardingTotal = total,
            OnboardingPercentual = percentual
        };

        if (!tenant.Ativo)
        {
            snapshot.StatusOperacional = "Inativo";
            snapshot.StatusOperacionalChave = "inativo";
            snapshot.StatusOperacionalTom = "secondary";
            return snapshot;
        }

        if (concluidos == total)
        {
            snapshot.StatusOperacional = "Homologação inicial";
            snapshot.StatusOperacionalChave = "homologacao-inicial";
            snapshot.StatusOperacionalTom = "default";
            return snapshot;
        }

        if (concluidos >= 3)
        {
            snapshot.StatusOperacional = "Onboarding";
            snapshot.StatusOperacionalChave = "onboarding";
            snapshot.StatusOperacionalTom = "outline";
            return snapshot;
        }

        if (concluidos >= 1)
        {
            snapshot.StatusOperacional = "Provisionado";
            snapshot.StatusOperacionalChave = "provisionado";
            snapshot.StatusOperacionalTom = "secondary";
            return snapshot;
        }

        snapshot.StatusOperacional = "Rascunho";
        snapshot.StatusOperacionalChave = "rascunho";
        snapshot.StatusOperacionalTom = "secondary";
        return snapshot;
    }

    private static TenantDto MapToDto(
        Tenant tenant,
        string? primaryDomain = null,
        int totalUsuarios = 0,
        int totalAdministradores = 0,
        int totalPessoas = 0,
        DateTime? ultimaAtividadeEm = null)
    {
        var resolvedPrimaryDomain = primaryDomain
            ?? tenant.Domains.FirstOrDefault(d => d.IsPrimary && d.Ativo)?.Domain
            ?? tenant.Domains.FirstOrDefault(d => d.Ativo)?.Domain;
        var operationalSnapshot = BuildOperationalSnapshot(
            tenant,
            resolvedPrimaryDomain,
            totalUsuarios,
            totalAdministradores,
            totalPessoas);

        return new TenantDto
        {
            Id = tenant.Id,
            Nome = tenant.Nome,
            NomeExibicao = tenant.NomeExibicao,
            Slug = tenant.Slug,
            LogoUrl = tenant.LogoUrl,
            FaviconUrl = tenant.FaviconUrl,
            CorPrimaria = tenant.CorPrimaria,
            CorSecundaria = tenant.CorSecundaria,
            IsRootTenant = tenant.IsRootTenant,
            CanDelete = !tenant.IsRootTenant,
            CanDeactivate = !tenant.IsRootTenant,
            Ativo = tenant.Ativo,
            DataCriacao = tenant.DataCriacao,
            DominioPrimario = resolvedPrimaryDomain,
            TotalUsuarios = totalUsuarios,
            TotalAdministradores = totalAdministradores,
            TotalPessoas = totalPessoas,
            UltimaAtividadeEm = ultimaAtividadeEm,
            StatusOperacional = operationalSnapshot.StatusOperacional,
            StatusOperacionalChave = operationalSnapshot.StatusOperacionalChave,
            StatusOperacionalTom = operationalSnapshot.StatusOperacionalTom,
            OnboardingPercentual = operationalSnapshot.OnboardingPercentual,
            OnboardingConcluidos = operationalSnapshot.OnboardingConcluidos,
            OnboardingTotal = operationalSnapshot.OnboardingTotal,
            OnboardingIdentidadeOk = operationalSnapshot.OnboardingIdentidadeOk,
            OnboardingBrandingOk = operationalSnapshot.OnboardingBrandingOk,
            OnboardingDominioOk = operationalSnapshot.OnboardingDominioOk,
            OnboardingAdminOk = operationalSnapshot.OnboardingAdminOk,
            OnboardingBaseOperacionalOk = operationalSnapshot.OnboardingBaseOperacionalOk
        };
    }

    private sealed record TenantMetrics(
        int TotalUsuarios,
        int TotalAdministradores,
        int TotalPessoas,
        DateTime? UltimaAtividadeEm);

    private sealed class TenantOperationalSnapshot
    {
        public string StatusOperacional { get; set; } = "Rascunho";
        public string StatusOperacionalChave { get; set; } = "rascunho";
        public string StatusOperacionalTom { get; set; } = "secondary";
        public int OnboardingPercentual { get; set; }
        public int OnboardingConcluidos { get; set; }
        public int OnboardingTotal { get; set; }
        public bool OnboardingIdentidadeOk { get; set; }
        public bool OnboardingBrandingOk { get; set; }
        public bool OnboardingDominioOk { get; set; }
        public bool OnboardingAdminOk { get; set; }
        public bool OnboardingBaseOperacionalOk { get; set; }
    }

    private sealed class TenantDeletionAuditSnapshot
    {
        public int TenantId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? NomeExibicao { get; set; }
        public string Slug { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public bool IsRootTenant { get; set; }
        public string? DominioPrimario { get; set; }
    }
}
