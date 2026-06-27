using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace WhatsFlow.Infrastructure.Data;

public class WhatsFlowDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public WhatsFlowDbContext(DbContextOptions<WhatsFlowDbContext> options) : this(options, new DefaultTenantContext())
    {
    }

    public WhatsFlowDbContext(DbContextOptions<WhatsFlowDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public int CurrentTenantId => _tenantContext.TenantId ?? Tenant.InitialTenantId;
    public bool IgnoreTenantFilters { get; set; }

    // Carimba TenantId no INSERT de qualquer ITenantEntity que não teve o tenant
    // setado explicitamente (TenantId == 0). Rede de segurança contra vazamento
    // entre igrejas — vale para API e Worker (ambos usam este DbContext).
    private void StampTenantId()
    {
        var tenantId = CurrentTenantId;
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == 0)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenantId();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTenantId();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantDomain> TenantDomains { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ConfiguracaoMensagem> ConfiguracoesMensagens { get; set; }
    public DbSet<MensagemAgendada> MensagensAgendadas { get; set; }
    public DbSet<ComunicacaoTemplate> ComunicacaoTemplates { get; set; }
    public DbSet<ComunicacaoCampanha> ComunicacaoCampanhas { get; set; }
    public DbSet<ComunicacaoCampanhaCanal> ComunicacaoCampanhaCanais { get; set; }
    public DbSet<ComunicacaoEntrega> ComunicacaoEntregas { get; set; }
    public DbSet<ComunicacaoAutomacao> ComunicacaoAutomacoes { get; set; }
    public DbSet<ComunicacaoPreferencia> ComunicacaoPreferencias { get; set; }
    public DbSet<ComunicacaoSegmento> ComunicacaoSegmentos { get; set; }
    public DbSet<Contato> Contatos { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ContatoTag> ContatoTags { get; set; }
    public DbSet<WhatsAppAccount> WhatsAppAccounts { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    public DbSet<MessageLog> MessageLogs { get; set; }
    public DbSet<Plano> Planos { get; set; }
    public DbSet<Assinatura> Assinaturas { get; set; }
    public DbSet<Fatura> Faturas { get; set; }
    public DbSet<EventoWebhookBilling> EventosWebhookBilling { get; set; }
    public DbSet<VerificacaoEmail> VerificacoesEmail { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<NotificacaoUsuario> NotificacoesUsuarios { get; set; }
    public DbSet<PerfilAcesso> PerfisAcesso { get; set; }
    public DbSet<PerfilAcessoPermissao> PerfisAcessoPermissoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.NomeExibicao).HasMaxLength(150);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(120);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.FaviconUrl).HasMaxLength(500);
            entity.Property(e => e.CorPrimaria).HasMaxLength(20);
            entity.Property(e => e.CorSecundaria).HasMaxLength(20);
            entity.Property(e => e.IsRootTenant).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.HasData(new Tenant
            {
                Id = Tenant.InitialTenantId,
                Nome = Tenant.InitialTenantName,
                NomeExibicao = Tenant.InitialTenantName,
                Slug = Tenant.InitialTenantSlug,
                LogoUrl = null,
                FaviconUrl = null,
                CorPrimaria = "#25D366",
                CorSecundaria = "#075E54",
                Status = TenantStatus.Active,
                FusoHorario = "America/Sao_Paulo",
                IsRootTenant = true,
                Ativo = true,
                DataCriacao = new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<TenantDomain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsPrimary).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.HasIndex(e => e.Domain).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Domains)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(30);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.UserEmail).HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(60);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
            entity.HasIndex(e => new { e.TenantId, e.EntityName, e.EntityId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade ConfiguracaoMensagem
        modelBuilder.Entity<ConfiguracaoMensagem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TextoMensagem).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.DiasAposVisita).IsRequired();
            entity.Property(e => e.HorarioEnvio).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade MensagemAgendada
        modelBuilder.Entity<MensagemAgendada>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.DataAgendamento).IsRequired();
            entity.Property(e => e.DataEnvio).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.TextoFinal).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.LogErro).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            // Relacionamentos
            entity.HasOne(e => e.Contato)
                  .WithMany()
                  .HasForeignKey(e => e.ContatoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ConfiguracaoMensagem)
                  .WithMany(c => c.MensagensAgendadas)
                  .HasForeignKey(e => e.ConfiguracaoMensagemId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.Status, e.DataEnvio });
            entity.HasIndex(e => new { e.TenantId, e.ContatoId, e.DataEnvio });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComunicacaoTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Objetivo).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Assunto).HasMaxLength(200);
            entity.Property(e => e.Corpo).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.CorpoHtml).HasMaxLength(12000);
            entity.Property(e => e.VariaveisPermitidas).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Canal).IsRequired();
            entity.Property(e => e.Versao).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);
        });

        modelBuilder.Entity<ComunicacaoCampanha>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Objetivo).IsRequired().HasMaxLength(40);
            entity.Property(e => e.PublicoAlvo).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Origem).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.CriadoPorUsuario)
                .WithMany()
                .HasForeignKey(e => e.CriadoPorUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Segmento)
                .WithMany()
                .HasForeignKey(e => e.SegmentoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ComunicacaoCampanhaCanal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Canal).IsRequired();
            entity.Property(e => e.Prioridade).IsRequired();

            entity.HasOne(e => e.ComunicacaoCampanha)
                .WithMany(c => c.Canais)
                .HasForeignKey(e => e.ComunicacaoCampanhaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ComunicacaoEntrega>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Canal).IsRequired();
            entity.Property(e => e.DestinoResolvido).IsRequired().HasMaxLength(300);
            entity.Property(e => e.RemetenteResolvido).HasMaxLength(200);
            entity.Property(e => e.ConteudoFinal).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.ConteudoHtmlFinal).HasMaxLength(12000);
            entity.Property(e => e.MidiaUrl).HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Erro).HasMaxLength(1000);
            entity.Property(e => e.ChaveDedupe).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.ComunicacaoCampanha)
                .WithMany(c => c.Entregas)
                .HasForeignKey(e => e.ComunicacaoCampanhaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contato)
                .WithMany()
                .HasForeignKey(e => e.ContatoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.Status, e.Canal, e.DataCriacao });
            entity.HasIndex(e => e.ChaveDedupe);
        });

        modelBuilder.Entity<ComunicacaoAutomacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Gatilho).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SegmentoAlvo).HasMaxLength(100);
            entity.Property(e => e.Canal).IsRequired();
            entity.Property(e => e.DelayMinutos).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ComunicacaoPreferencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Canal).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.OrigemConsentimento).HasMaxLength(60);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.ContatoId, e.Canal }).IsUnique();

            entity.HasOne(e => e.Contato)
                .WithMany()
                .HasForeignKey(e => e.ContatoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComunicacaoSegmento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Descricao).HasMaxLength(300);
            entity.Property(e => e.PublicoAlvo).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.Padrao).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);
        });

        // Configuração da entidade Equipe
        // Configuração da entidade HubCasa
        // Configuração da entidade Fornecedor
        // Configuração da entidade CategoriaDespesa
        // Configuração da entidade CategoriaReceita
        // Configuração da entidade ContaBancaria
        // Configuração da entidade CentroCusto
        // Configuração da entidade Projeto
        // Configuração da entidade Despesa
        // Configuração da entidade Receita
        // Configuração da entidade Cargo
        // Configuração da entidade Voluntario
        // Configuração da entidade PerfilAcesso
        modelBuilder.Entity<PerfilAcesso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descricao).HasMaxLength(300);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade PerfilAcessoPermissao
        modelBuilder.Entity<PerfilAcessoPermissao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Recurso).IsRequired().HasMaxLength(80);
            entity.HasIndex(e => new { e.TenantId, e.PerfilAcessoId, e.Recurso }).IsUnique();

            entity.HasOne(e => e.PerfilAcesso)
                  .WithMany(p => p.Permissoes)
                  .HasForeignKey(e => e.PerfilAcessoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Relacionamento Usuario -> PerfilAcesso
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasOne(u => u.PerfilAcesso)
                  .WithMany(p => p.Usuarios)
                  .HasForeignKey(u => u.PerfilAcessoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Evento
        // Configuração da entidade EventoRecorrencia
        // Configuração da entidade EventoOcorrencia
        // Configuração da entidade Escala (uma por EventoOcorrencia + Equipe)
        // Configuração da entidade EscalaItem
        // EscalaModelo (modelo de escala por evento + equipe)
        // Configuração da entidade DestaqueSite
        // Configuração da entidade ConfiguracaoPortal (singleton)
        // Configuração da entidade CategoriaNoticia
        // Configuração da entidade Noticia
        // Configuração da entidade Contato (modelo rico — substitui Pessoa/Visitante)
        modelBuilder.Entity<Contato>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.TelefoneWhatsApp).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Documento).HasMaxLength(30);
            entity.Property(e => e.Organizacao).HasMaxLength(150);
            entity.Property(e => e.Observacoes).HasMaxLength(2000);
            entity.Property(e => e.Origem).HasMaxLength(60);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.OptIn).IsRequired();
            entity.Property(e => e.CriadoEm).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.TelefoneWhatsApp }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Cor).HasMaxLength(9);
            entity.Property(e => e.CriadoEm).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade ContatoTag (N:N Contato <-> Tag)
        modelBuilder.Entity<ContatoTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();

            entity.HasIndex(e => new { e.ContatoId, e.TagId }).IsUnique();

            entity.HasOne(e => e.Contato)
                .WithMany(c => c.ContatoTags)
                .HasForeignKey(e => e.ContatoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.ContatoTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração da entidade WhatsAppAccount
        modelBuilder.Entity<WhatsAppAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.PhoneNumberId).HasMaxLength(120);
            entity.Property(e => e.BusinessAccountId).HasMaxLength(120);
            entity.Property(e => e.AccessTokenProtegido).HasMaxLength(2000);
            entity.Property(e => e.WebhookSecret).HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CriadoEm).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade WebhookEvent (log bruto de webhooks)
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(60);
            entity.Property(e => e.ProviderMessageId).HasMaxLength(150);
            entity.Property(e => e.RawPayload).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Erro).HasMaxLength(1000);
            entity.Property(e => e.CriadoEm).IsRequired();

            entity.HasIndex(e => new { e.Provider, e.ProviderMessageId });
            entity.HasIndex(e => new { e.TenantId, e.Status, e.CriadoEm });

            entity.HasOne(e => e.WhatsAppAccount)
                .WithMany()
                .HasForeignKey(e => e.WhatsAppAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuração da entidade MessageLog (histórico append-only de entregas)
        modelBuilder.Entity<MessageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ProviderMessageId).HasMaxLength(150);
            entity.Property(e => e.ErrorCode).HasMaxLength(60);
            entity.Property(e => e.Detalhe).HasMaxLength(1000);
            entity.Property(e => e.CriadoEm).IsRequired();

            entity.HasIndex(e => e.ComunicacaoEntregaId);

            entity.HasOne(e => e.ComunicacaoEntrega)
                .WithMany(ce => ce.Logs)
                .HasForeignKey(e => e.ComunicacaoEntregaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração da entidade ConsentimentoRegistro (trilha de consentimento LGPD)
        // Configuração da entidade SolicitacaoTitular (requisições de titular - LGPD)
        // ===== Billing (assinatura da plataforma) =====
        modelBuilder.Entity<Plano>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(60);
            entity.Property(e => e.Descricao).HasMaxLength(500);
            entity.Property(e => e.PrecoMensal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PrecoAnual).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Slug).IsUnique();

            // Seed dos planos da landing VerboPlus. PREÇOS SÃO PLACEHOLDERS — revisar.
            entity.HasData(
                new Plano { Id = 1, Nome = "Essencial", Slug = "essencial", Descricao = "Para igrejas começando a se organizar.", PrecoMensal = 49.90m, PrecoAnual = 499.00m, Ativo = true, Ordem = 1, DataCriacao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Plano { Id = 2, Nome = "Organização", Slug = "organizacao", Descricao = "Para igrejas em crescimento que precisam de gestão completa.", PrecoMensal = 99.90m, PrecoAnual = 999.00m, Ativo = true, Ordem = 2, DataCriacao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Plano { Id = 3, Nome = "Crescimento", Slug = "crescimento", Descricao = "Para igrejas com múltiplos ministérios e alto volume.", PrecoMensal = 199.90m, PrecoAnual = 1999.00m, Ativo = true, Ordem = 3, DataCriacao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        });

        modelBuilder.Entity<Assinatura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Ciclo).IsRequired();
            entity.Property(e => e.Valor).HasColumnType("decimal(18,2)");
            entity.Property(e => e.GatewayCustomerId).HasMaxLength(120);
            entity.Property(e => e.GatewaySubscriptionId).HasMaxLength(120);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Plano)
                .WithMany()
                .HasForeignKey(e => e.PlanoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.GatewaySubscriptionId);
        });

        modelBuilder.Entity<Fatura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Valor).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.GatewayPaymentId).HasMaxLength(120);
            entity.Property(e => e.LinkPagamento).HasMaxLength(500);
            entity.Property(e => e.PixCopiaECola).HasMaxLength(2000);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Assinatura)
                .WithMany()
                .HasForeignKey(e => e.AssinaturaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => e.GatewayPaymentId);
        });

        modelBuilder.Entity<EventoWebhookBilling>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GatewayEventId).HasMaxLength(120);
            entity.Property(e => e.Evento).IsRequired().HasMaxLength(80);
            entity.Property(e => e.GatewayPaymentId).HasMaxLength(120);
            entity.Property(e => e.GatewaySubscriptionId).HasMaxLength(120);
            entity.Property(e => e.Observacao).HasMaxLength(500);
            entity.HasIndex(e => e.GatewayEventId);
        });

        modelBuilder.Entity<VerificacaoEmail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.UsuarioId).IsRequired();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(80);
            entity.Property(e => e.ExpiraEm).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Configuração da entidade InscricaoEvento
        // Configuração da entidade Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Telefone).HasMaxLength(20);
            entity.Property(e => e.WhatsApp).HasMaxLength(20);
            entity.Property(e => e.EmailLogin).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SenhaHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TipoUsuario).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.IsPlatformAdmin).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(u => u.Tenant)
                .WithMany()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // E-mail de login é único GLOBALMENTE (login self-service só por e-mail+senha).
            entity.HasIndex(e => e.EmailLogin).IsUnique();
        });

        modelBuilder.Entity<NotificacaoUsuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).IsRequired();
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Mensagem).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Link).HasMaxLength(300);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.UsuarioId, e.DataLeitura, e.DataCriacao });

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Notificacoes)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração da entidade CategoriaMidia
        // Configuração da entidade GaleriaFoto
        // Configuração da entidade GaleriaFotoItem (fotos listadas a partir do banco para funcionar com mesmo DB em dev)
        // Configuração da entidade CriancaDetalhe
        // Configuração da entidade ResponsavelCrianca
        // Configuração da entidade KidsCheckin
        // Configuração da entidade KidsNotificacao
        // Configuração da entidade KidsDeviceToken
        // Dados iniciais para ConfiguracaoMensagem (datas fixas para evitar warnings de migração)
        var seedDate = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ConfiguracaoMensagem>().HasData(
            new ConfiguracaoMensagem
            {
                Id = 1,
                TenantId = Tenant.InitialTenantId,
                Nome = "Boas-vindas",
                TextoMensagem = "Olá {Nome}! Que alegria ter você conosco na igreja! Esperamos vê-lo novamente em breve. Deus abençoe!",
                DiasAposVisita = 1,
                HorarioEnvio = new TimeSpan(10, 0, 0), // 10:00
                Ativo = true,
                DataCriacao = seedDate
            },
            new ConfiguracaoMensagem
            {
                Id = 2,
                TenantId = Tenant.InitialTenantId,
                Nome = "Convite para retorno",
                TextoMensagem = "Oi {Nome}! Sentimos sua falta na igreja. Que tal nos visitar novamente neste domingo? Será um prazer recebê-lo!",
                DiasAposVisita = 7,
                HorarioEnvio = new TimeSpan(18, 0, 0), // 18:00
                Ativo = true,
                DataCriacao = seedDate
            }
        );

        // Configuração da entidade Enquete
        // Configuração da entidade EnqueteOpcao
        // Configuração da entidade EnqueteVoto
        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var tenantEntityTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(entityType => typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.ClrType)
            .ToList();

        foreach (var entityType in tenantEntityTypes)
        {
            var method = typeof(WhatsFlowDbContext)
                .GetMethod(nameof(ApplyTenantQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        Expression<Func<TEntity, bool>> filter = entity =>
            IgnoreTenantFilters || entity.TenantId == CurrentTenantId;

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
