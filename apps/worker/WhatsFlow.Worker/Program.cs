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

        services.AddScoped<IPessoaRepository, PessoaRepository>();
        services.AddScoped<IPessoaPerfilRepository, PessoaPerfilRepository>();
        services.AddScoped<IVisitanteRepository, VisitanteRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConfiguracaoMensagemRepository, ConfiguracaoMensagemRepository>();
        services.AddScoped<IMensagemAgendadaRepository, MensagemAgendadaRepository>();
        services.AddScoped<IEquipeRepository, EquipeRepository>();
        services.AddScoped<ICargoRepository, CargoRepository>();
        services.AddScoped<IVoluntarioRepository, VoluntarioRepository>();
        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<IEventoOcorrenciaRepository, EventoOcorrenciaRepository>();
        services.AddScoped<IEscalaRepository, EscalaRepository>();
        services.AddScoped<IConfiguracaoCampanhaAniversarioRepository, ConfiguracaoCampanhaAniversarioRepository>();
        services.AddScoped<IEnvioCampanhaAniversarioRepository, EnvioCampanhaAniversarioRepository>();

        services.AddScoped<IVisitanteService, VisitanteService>();
        services.AddScoped<IConfiguracaoMensagemService, ConfiguracaoMensagemService>();
        services.AddScoped<IMensagemAgendadaService, MensagemAgendadaService>();
        services.AddScoped<IEquipeService, EquipeService>();
        services.AddScoped<ICargoService, CargoService>();
        services.AddScoped<IVoluntarioService, VoluntarioService>();
        services.AddScoped<IEventoOcorrenciaService, EventoOcorrenciaService>();
        services.AddScoped<IEscalaService, EscalaService>();
        services.AddScoped<ICampanhaAniversarioService, CampanhaAniversarioService>();
        services.AddSingleton<ISchedulerExecutionMonitor, SchedulerExecutionMonitor>();

        services.Configure<MessageSchedulerSettings>(
            ctx.Configuration.GetSection(MessageSchedulerSettings.SectionName));
        services.Configure<EscalaSchedulerSettings>(
            ctx.Configuration.GetSection(EscalaSchedulerSettings.SectionName));
        services.Configure<BirthdayCampaignSchedulerSettings>(
            ctx.Configuration.GetSection(BirthdayCampaignSchedulerSettings.SectionName));
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
        // Grafo do EscalaService (lembretes de escala no EscalaSchedulerService).
        // O EscalaService depende de auditoria + automação de comunicação, que por
        // sua vez puxam repositórios/serviços de comunicação. Sem isto, o scheduler
        // falha ao resolver IEscalaService ("No constructor can be instantiated").
        // O canal Push é exclusivo da API (Firebase) e não é registrado aqui.
        // ==========================
        services.AddScoped<ICurrentUserContext, WorkerCurrentUserContext>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IEscalaModeloRepository, EscalaModeloRepository>();
        services.AddScoped<IIndisponibilidadeVoluntarioRepository, IndisponibilidadeVoluntarioRepository>();
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
        services.AddHostedService<EscalaSchedulerService>();
        services.AddHostedService<BirthdayCampaignSchedulerService>();
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
