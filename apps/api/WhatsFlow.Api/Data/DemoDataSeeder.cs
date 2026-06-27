using Microsoft.EntityFrameworkCore;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;
using WhatsFlow.Infrastructure.Services;

namespace WhatsFlow.API.Data;

/// <summary>
/// Aplica migrations (se Database:RunMigrations) e semeia dados mínimos para o tenant demo
/// (perfil Administrador + usuário admin + conta WhatsApp Fake). Idempotente — só cria o que falta.
/// Controlado por Seed:DemoData (default true). Não cria nada além do tenant raiz (Id=1).
/// </summary>
public static class DemoDataSeeder
{
    public const string AdminEmail = "admin@whatsflow.app";
    public const string AdminSenhaPadrao = "Whatsflow@2026";

    public static async Task RunAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WhatsFlowDbContext>();

        try
        {
            if (config.GetValue("Database:RunMigrations", false))
            {
                await db.Database.MigrateAsync();
            }

            if (!config.GetValue("Seed:DemoData", true))
            {
                return;
            }

            var tenantId = Tenant.InitialTenantId;

            var perfil = await db.PerfisAcesso.FirstOrDefaultAsync(p => p.Nome == "Administrador");
            if (perfil == null)
            {
                perfil = new PerfilAcesso
                {
                    TenantId = tenantId,
                    Nome = "Administrador",
                    Descricao = "Acesso total (demo).",
                    DataCriacao = DateTime.UtcNow,
                    Permissoes = TenantManagementService.DefaultAdminResources
                        .Select(r => new PerfilAcessoPermissao
                        {
                            TenantId = tenantId,
                            Recurso = r,
                            PodeVer = true,
                            PodeEditar = true,
                            PodeExcluir = true
                        })
                        .ToList()
                };
                db.PerfisAcesso.Add(perfil);
                await db.SaveChangesAsync();
            }

            if (!await db.Usuarios.AnyAsync(u => u.EmailLogin == AdminEmail))
            {
                db.Usuarios.Add(new Usuario
                {
                    TenantId = tenantId,
                    Nome = "Admin WhatsFlow",
                    Email = AdminEmail,
                    EmailLogin = AdminEmail,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(AdminSenhaPadrao),
                    TipoUsuario = TipoUsuario.Admin,
                    Ativo = true,
                    IsPlatformAdmin = true,
                    PerfilAcessoId = perfil.Id,
                    DataCriacao = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Seed: usuário admin demo criado ({Email} / senha padrão).", AdminEmail);
            }

            if (!await db.WhatsAppAccounts.AnyAsync())
            {
                db.WhatsAppAccounts.Add(new WhatsAppAccount
                {
                    TenantId = tenantId,
                    Nome = "Conta Demo (Fake)",
                    Provider = WhatsAppProviderType.Fake,
                    Status = WhatsAppAccountStatus.Ativa,
                    CriadoEm = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Seed: conta WhatsApp Fake criada para o tenant demo.");
            }
        }
        catch (Exception ex)
        {
            // Não derruba a API se o banco estiver indisponível no boot (dev-friendly).
            logger.LogWarning(ex, "Seed/migrate de inicialização falhou (banco indisponível?). A API seguirá iniciando.");
        }
    }
}
