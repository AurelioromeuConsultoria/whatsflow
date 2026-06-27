using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WhatsFlow.API.Swagger;
using WhatsFlow.API.Services;
using WhatsFlow.Infrastructure.Data;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Infrastructure.Repositories;
using WhatsFlow.Application.Services;
using WhatsFlow.Infrastructure.Services;
using WhatsFlow.Application.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Observabilidade (Sentry). Lê a seção "Sentry" do config; se o DSN estiver vazio,
// o SDK fica desligado (no-op). Não envia PII (SendDefaultPii=false).
builder.WebHost.UseSentry();

// ==========================
// DATABASE CONFIGURATION
// ==========================

var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (databaseProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase) ||
    databaseProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();

builder.Services.AddDbContext<WhatsFlowDbContext>((sp, options) =>
{
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
    switch (databaseProvider.ToLowerInvariant())
    {
        case "postgresql":
        case "postgres":
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure());
            break;

        case "sqlserver":
        default:
            options.UseSqlServer(connectionString);
            break;
    }
});

builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"])
    .AddCheck<EvolutionApiConfigurationHealthCheck>(
        name: "evolution_api_configuration",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "config"])
    .AddCheck<EmailConfigurationHealthCheck>(
        name: "email_configuration",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "config"])
    .AddCheck<MessageSchedulerConfigurationHealthCheck>(
        name: "message_scheduler_configuration",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "config"]);

// ==========================
// DEPENDENCY INJECTION
// ==========================

// Repositories
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IBillingCycleService, BillingCycleService>();
builder.Services.AddScoped<ISignupService, SignupService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IConfiguracaoMensagemRepository, ConfiguracaoMensagemRepository>();
builder.Services.AddScoped<IMensagemAgendadaRepository, MensagemAgendadaRepository>();
builder.Services.AddScoped<IComunicacaoTemplateRepository, ComunicacaoTemplateRepository>();
builder.Services.AddScoped<IComunicacaoCampanhaRepository, ComunicacaoCampanhaRepository>();
builder.Services.AddScoped<IComunicacaoEntregaRepository, ComunicacaoEntregaRepository>();
builder.Services.AddScoped<IComunicacaoPreferenciaRepository, ComunicacaoPreferenciaRepository>();
builder.Services.AddScoped<IComunicacaoSegmentoRepository, ComunicacaoSegmentoRepository>();
builder.Services.AddScoped<IContatoRepository, ContatoRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IWhatsAppAccountRepository, WhatsAppAccountRepository>();
builder.Services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
builder.Services.AddScoped<IMessageLogRepository, MessageLogRepository>();
builder.Services.AddScoped<ITenantLookupRepository, TenantLookupRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<INotificacaoUsuarioRepository, NotificacaoUsuarioRepository>();
builder.Services.AddScoped<IPerfilAcessoRepository, PerfilAcessoRepository>();
builder.Services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();

// Services
builder.Services.AddScoped<IConfiguracaoMensagemService, ConfiguracaoMensagemService>();
builder.Services.AddScoped<IMensagemAgendadaService, MensagemAgendadaService>();
builder.Services.AddScoped<IComunicacaoTemplateService, ComunicacaoTemplateService>();
builder.Services.AddScoped<IComunicacaoCampanhaService, ComunicacaoCampanhaService>();
builder.Services.AddScoped<IComunicacaoEntregaService, ComunicacaoEntregaService>();
builder.Services.AddScoped<IComunicacaoPreferenciaService, ComunicacaoPreferenciaService>();
builder.Services.AddScoped<IComunicacaoSegmentoService, ComunicacaoSegmentoService>();
builder.Services.AddScoped<IComunicacaoAudienceResolver, ComunicacaoAudienceResolver>();
builder.Services.AddScoped<IComunicacaoProcessamentoService, ComunicacaoProcessamentoService>();
builder.Services.AddScoped<IComunicacaoAutomacaoService, ComunicacaoAutomacaoService>();
builder.Services.AddScoped<IPlanLimitService, PlanLimitService>();
builder.Services.AddScoped<IWhatsAppWebhookProcessingService, WhatsAppWebhookProcessingService>();
// Providers de WhatsApp (abstração IWhatsAppProvider) + resolver por conta/tenant
builder.Services.AddScoped<WhatsFlow.Application.Services.WhatsApp.IWhatsAppProvider, WhatsFlow.Application.Services.WhatsApp.FakeWhatsAppProvider>();
builder.Services.AddScoped<WhatsFlow.Application.Services.WhatsApp.IWhatsAppProvider, WhatsFlow.Application.Services.WhatsApp.EvolutionWhatsAppProvider>();
builder.Services.AddScoped<WhatsFlow.Application.Services.WhatsApp.IWhatsAppProviderResolver, WhatsFlow.Application.Services.WhatsApp.WhatsAppProviderResolver>();
builder.Services.AddScoped<IComunicacaoCanalProvider, ComunicacaoWhatsAppCanalProvider>();
builder.Services.AddScoped<IComunicacaoCanalProvider, ComunicacaoEmailCanalProvider>();
builder.Services.AddScoped<IComunicacaoCanalProvider, ComunicacaoNotificacaoInternaCanalProvider>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddScoped<TenantScopeOverride>();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddHttpClient<IAsaasBillingClient, AsaasBillingClient>();
builder.Services.AddScoped<IContatoService, ContatoService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IWhatsAppAccountService, WhatsAppAccountService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<INotificacaoUsuarioService, NotificacaoUsuarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPerfilAcessoService, PerfilAcessoService>();
builder.Services.AddScoped<ITenantManagementService, TenantManagementService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddSingleton<ISchedulerExecutionMonitor, SchedulerExecutionMonitor>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.Configure<EvolutionApiSettings>(
    builder.Configuration.GetSection("EvolutionApi"));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.Configure<BillingSettings>(
    builder.Configuration.GetSection(BillingSettings.SectionName));
builder.Services.Configure<AsaasBillingSettings>(
    builder.Configuration.GetSection(AsaasBillingSettings.SectionName));

builder.Services.Configure<MessageSchedulerSettings>(
    builder.Configuration.GetSection(MessageSchedulerSettings.SectionName));

builder.Services.Configure<PublicAppUrlSettings>(
    builder.Configuration.GetSection(PublicAppUrlSettings.SectionName));

builder.Services.AddHttpClient<IEvolutionApiService, EvolutionApiService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddHttpClient();

// ==========================
// STORAGE DE ARQUIVOS
// ==========================

builder.Services.Configure<WhatsFlow.Application.Services.StorageSettings>(
    builder.Configuration.GetSection(WhatsFlow.Application.Services.StorageSettings.SectionName));

var storageProvider = builder.Configuration["Storage:Provider"] ?? "local";
if (string.Equals(storageProvider, "s3", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<WhatsFlow.Application.Interfaces.IFileStorageService,
        WhatsFlow.Infrastructure.Services.S3FileStorageService>();
else
    builder.Services.AddSingleton<WhatsFlow.Application.Interfaces.IFileStorageService,
        WhatsFlow.Infrastructure.Services.LocalFileStorageService>();

// ==========================
// JWT AUTH
// ==========================

var jwtKey = builder.Configuration["Jwt:Key"];
// Recusa subir com chave ausente, vazia ou com o placeholder conhecido (segurança).
const string jwtPlaceholder = "sua-chave-secreta-super-segura-com-pelo-menos-32-caracteres-para-jwt";
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == jwtPlaceholder)
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada ou usando o valor placeholder. Defina a variável de ambiente Jwt__Key com uma chave forte (ex.: openssl rand -base64 48).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Rate limiting por IP — signup (anti-abuso) e login (anti brute-force).
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("signup", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Aceitar camelCase do frontend (ex.: aceitaInscricoes) no binding para propriedades C# (AceitaInscricoes)
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sistema Igreja API", Version = "v1" });
    
    // Adicionar filtro customizado para lidar com upload de arquivos (IFormFile)
    c.OperationFilter<FileUploadOperationFilter>();
    
    // Configurar suporte para upload de arquivos (IFormFile) - fallback
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
    
    // Configurar suporte para List<IFormFile> - fallback
    c.MapType<List<IFormFile>>(() => new OpenApiSchema
    {
        Type = "array",
        Items = new OpenApiSchema
        {
            Type = "string",
            Format = "binary"
        }
    });
    
    // Configurar segurança JWT no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================
// CORS
// ==========================

const string CorsPolicyName = "DefaultCors";
var allowedCorsOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    // Admin local (Vite). Domínios de produção vêm de Cors:AllowedOrigins (appsettings/env).
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:3000",
    "http://localhost:4173"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                    return false;

                if (allowedCorsOrigins.Contains(origin))
                    return true;

                // Permitir qualquer porta em localhost/127.0.0.1 (ambiente de desenvolvimento)
                if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
                    return true;

                if (origin.StartsWith("http://127.0.0.1:") || origin.StartsWith("https://127.0.0.1:"))
                    return true;

                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

var app = builder.Build();

// Migrations + seed do tenant demo (admin + conta WhatsApp Fake). Idempotente; gated por config.
await WhatsFlow.API.Data.DemoDataSeeder.RunAsync(app.Services, app.Configuration,
    app.Services.GetRequiredService<ILogger<Program>>());

// Middleware CORS explícito - garante headers em TODA resposta (evita falhas com proxy/ordem)
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].FirstOrDefault();
    var isAllowedOrigin =
        !string.IsNullOrEmpty(origin) &&
        (allowedCorsOrigins.Contains(origin) ||
         origin.StartsWith("http://localhost:") ||
         origin.StartsWith("https://localhost:") ||
         origin.StartsWith("http://127.0.0.1:") ||
         origin.StartsWith("https://127.0.0.1:"));

    if (isAllowedOrigin)
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, PATCH, DELETE, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, Accept, X-Tenant-Id, X-Tenant-Slug");
    }

    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

app.UseRouting();
app.UseCors(CorsPolicyName);
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();

// ==========================
// UPLOADS — arquivos estáticos (storage local)
// Quando Storage:Provider=s3, este bloco é ignorado:
// os arquivos são servidos diretamente pelo bucket (CDN ou URL pré-assinada).
// ==========================

var uploadsPath = WhatsFlow.Infrastructure.Services.LocalFileStorageService
    .ResolveUploadsPath(app.Environment.ContentRootPath, app.Configuration);

try
{
    Directory.CreateDirectory(uploadsPath);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Erro ao criar pasta de uploads em {UploadsPath}", uploadsPath);
}

app.Logger.LogInformation("UploadsPath definido como: {UploadsPath}", uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        var origin = ctx.Context.Request.Headers["Origin"].FirstOrDefault();
        if (!string.IsNullOrEmpty(origin) && allowedCorsOrigins.Contains(origin))
        {
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
        }
    }
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<WhatsFlow.API.Middleware.SubscriptionGatingMiddleware>();
app.UseMiddleware<WhatsFlow.API.Permissions.PermissionMiddleware>();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    error = entry.Value.Exception?.Message,
                    tags = entry.Value.Tags
                })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.MapControllers();

app.Run();
