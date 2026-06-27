using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using WhatsFlow.BackgroundWorker;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Infrastructure.Data;
using WhatsFlow.Infrastructure.Repositories;
using WhatsFlow.Infrastructure.Services;

var host = Host.CreateDefaultBuilder(args)
    // Valida o grafo de DI no startup: se um serviço usado por um scheduler não
    // puder ser construído (registro faltando), o Worker falha imediatamente e
    // de forma visível (logado no Sentry), em vez de quebrar no meio de um job.
    .UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateOnBuild = true;
        options.ValidateScopes = true;
    })
    .ConfigureServices((ctx, services) =>
    {
        // ==========================
        // DATABASE CONFIGURATION
        // ==========================

        var databaseProvider = ctx.Configuration["Database:Provider"] ?? "SqlServer";
        var connectionString = ctx.Configuration.GetConnectionString("DefaultConnection");

        if (databaseProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase) ||
            databaseProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        services.AddDbContext<WhatsFlowDbContext>(options =>
        {
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

        services.AddScoped<TenantScopeOverride>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantScopeOverride>());

        services.AddScoped<IContatoRepository, ContatoRepository>();
        services.AddScoped<IWhatsAppAccountRepository, WhatsAppAccountRepository>();
        services.AddScoped<IMessageLogRepository, MessageLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConfiguracaoMensagemRepository, ConfiguracaoMensagemRepository>();
        services.AddScoped<IMensagemAgendadaRepository, MensagemAgendadaRepository>();

        services.AddScoped<IConfiguracaoMensagemService, ConfiguracaoMensagemService>();
        services.AddScoped<IMensagemAgendadaService, MensagemAgendadaService>();
        services.AddSingleton<ISchedulerExecutionMonitor, SchedulerExecutionMonitor>();

        services.Configure<MessageSchedulerSettings>(
            ctx.Configuration.GetSection(MessageSchedulerSettings.SectionName));
        services.Configure<EvolutionApiSettings>(
            ctx.Configuration.GetSection("EvolutionApi"));
        services.Configure<PublicAppUrlSettings>(
            ctx.Configuration.GetSection(PublicAppUrlSettings.SectionName));

        services.AddHttpClient<IEvolutionApiService, EvolutionApiService>();

        // Billing (ciclo automático: trial→inadimplente→suspensa)
        services.Configure<EmailSettings>(
            ctx.Configuration.GetSection(EmailSettings.SectionName));
        services.Configure<BillingSettings>(
            ctx.Configuration.GetSection(BillingSettings.SectionName));
        services.Configure<BillingSchedulerSettings>(
            ctx.Configuration.GetSection(BillingSchedulerSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IBillingCycleService, BillingCycleService>();

        // ==========================
        // Grafo de comunicação consumido pelos schedulers (lembretes/processamento).
        // O canal Push é exclusivo da API (Firebase) e não é registrado aqui.
        // ==========================
        services.AddScoped<ICurrentUserContext, WorkerCurrentUserContext>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<INotificacaoUsuarioRepository, NotificacaoUsuarioRepository>();
        services.AddScoped<IComunicacaoCampanhaRepository, ComunicacaoCampanhaRepository>();
        services.AddScoped<IComunicacaoEntregaRepository, ComunicacaoEntregaRepository>();
        services.AddScoped<IComunicacaoPreferenciaRepository, ComunicacaoPreferenciaRepository>();

        services.AddScoped<INotificacaoUsuarioService, NotificacaoUsuarioService>();
        services.AddScoped<IComunicacaoPreferenciaService, ComunicacaoPreferenciaService>();
        services.AddScoped<IComunicacaoEntregaService, ComunicacaoEntregaService>();
        services.AddScoped<IComunicacaoProcessamentoService, ComunicacaoProcessamentoService>();
        services.AddScoped<IComunicacaoAutomacaoService, ComunicacaoAutomacaoService>();
        services.AddScoped<IComunicacaoCanalProvider, ComunicacaoWhatsAppCanalProvider>();
        services.AddScoped<IComunicacaoCanalProvider, ComunicacaoEmailCanalProvider>();
        services.AddScoped<IComunicacaoCanalProvider, ComunicacaoNotificacaoInternaCanalProvider>();

        services.AddHostedService<MessageSchedulerService>();
        services.AddHostedService<BillingSchedulerService>();
    })
    .ConfigureLogging((ctx, log) =>
    {
        log.AddConsole();
        // Observabilidade: envia erros logados (LogError) dos jobs ao Sentry.
        // No-op se "Sentry:Dsn" estiver vazio. Não envia PII.
        log.AddSentry(o =>
        {
            o.Dsn = ctx.Configuration["Sentry:Dsn"] ?? string.Empty;
            o.Environment = ctx.Configuration["Sentry:Environment"] ?? "Production";
            o.SendDefaultPii = false;
            o.MinimumEventLevel = LogLevel.Error;
        });
    })
    .Build();

await host.RunAsync();
