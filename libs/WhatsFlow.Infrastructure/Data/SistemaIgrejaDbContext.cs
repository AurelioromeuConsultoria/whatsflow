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
    public DbSet<Pessoa> Pessoas { get; set; }
    public DbSet<PessoaPerfil> PessoasPerfis { get; set; }
    public DbSet<Visitante> Visitantes { get; set; }
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
    public DbSet<Equipe> Equipes { get; set; }
    public DbSet<HubCasa> HubCasas { get; set; }
    public DbSet<Fornecedor> Fornecedores { get; set; }
    public DbSet<CategoriaDespesa> CategoriasDespesas { get; set; }
    public DbSet<CategoriaReceita> CategoriasReceitas { get; set; }
    public DbSet<FinalidadeDoacao> FinalidadesDoacao { get; set; }
    public DbSet<DoacaoOnline> DoacoesOnline { get; set; }
    public DbSet<GivingProviderConfig> GivingProviderConfigs { get; set; }
    public DbSet<ContaBancaria> ContasBancarias { get; set; }
    public DbSet<CentroCusto> CentrosCustos { get; set; }
    public DbSet<Projeto> Projetos { get; set; }
    public DbSet<CategoriaPatrimonio> CategoriasPatrimonio { get; set; }
    public DbSet<PatrimonioItem> PatrimonioItens { get; set; }
    public DbSet<PatrimonioMovimentacao> PatrimonioMovimentacoes { get; set; }
    public DbSet<Despesa> Despesas { get; set; }
    public DbSet<Receita> Receitas { get; set; }
    public DbSet<OrcamentoCategoria> OrcamentoCategorias { get; set; }
    public DbSet<Cargo> Cargos { get; set; }
    public DbSet<Voluntario> Voluntarios { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<EventoRecorrencia> EventosRecorrencias { get; set; }
    public DbSet<EventoOcorrencia> EventosOcorrencias { get; set; }
    public DbSet<Escala> Escalas { get; set; }
    public DbSet<EscalaItem> EscalasItens { get; set; }
    public DbSet<EscalaModelo> EscalasModelos { get; set; }
    public DbSet<EscalaModeloItem> EscalasModelosItens { get; set; }
    public DbSet<IndisponibilidadeVoluntario> IndisponibilidadesVoluntarios { get; set; }
    public DbSet<SolicitacaoTrocaEscala> SolicitacoesTrocasEscalas { get; set; }
    public DbSet<DestaqueSite> DestaquesSite { get; set; }
    public DbSet<ConfiguracaoPortal> ConfiguracoesPortal { get; set; }
    public DbSet<ConfiguracaoCampanhaAniversario> ConfiguracoesCampanhaAniversario { get; set; }
    public DbSet<EnvioCampanhaAniversario> EnviosCampanhaAniversario { get; set; }
    public DbSet<CategoriaNoticia> CategoriasNoticias { get; set; }
    public DbSet<Noticia> Noticias { get; set; }
    public DbSet<Contato> Contatos { get; set; }
    public DbSet<ConsentimentoRegistro> ConsentimentosRegistros { get; set; }
    public DbSet<SolicitacaoTitular> SolicitacoesTitular { get; set; }
    public DbSet<Plano> Planos { get; set; }
    public DbSet<Assinatura> Assinaturas { get; set; }
    public DbSet<Fatura> Faturas { get; set; }
    public DbSet<EventoWebhookBilling> EventosWebhookBilling { get; set; }
    public DbSet<VerificacaoEmail> VerificacoesEmail { get; set; }
    public DbSet<InscricaoEvento> InscricoesEventos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<NotificacaoUsuario> NotificacoesUsuarios { get; set; }
    public DbSet<PerfilAcesso> PerfisAcesso { get; set; }
    public DbSet<PerfilAcessoPermissao> PerfisAcessoPermissoes { get; set; }
    public DbSet<CategoriaMidia> CategoriasMidias { get; set; }
    public DbSet<GaleriaFoto> GaleriasFotos { get; set; }
    public DbSet<GaleriaFotoItem> GaleriasFotosItens { get; set; }
    public DbSet<Enquete> Enquetes { get; set; }
    public DbSet<EnqueteOpcao> EnqueteOpcoes { get; set; }
    public DbSet<EnqueteVoto> EnqueteVotos { get; set; }
    
    // Kids
    public DbSet<CriancaDetalhe> CriancasDetalhes { get; set; }
    public DbSet<ResponsavelCrianca> ResponsaveisCriancas { get; set; }
    public DbSet<KidsCheckin> KidsCheckins { get; set; }
    public DbSet<KidsPreCheckin> KidsPreCheckins { get; set; }
    public DbSet<KidsConteudoAula> KidsConteudosAula { get; set; }
    public DbSet<KidsConteudoAulaAnexo> KidsConteudosAulaAnexos { get; set; }
    public DbSet<KidsNotificacao> KidsNotificacoes { get; set; }
    public DbSet<KidsDeviceToken> KidsDeviceTokens { get; set; }
    public DbSet<KidsOcorrencia> KidsOcorrencias { get; set; }
    public DbSet<KidsSala> KidsSalas { get; set; }
    public DbSet<KidsTurma> KidsTurmas { get; set; }

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
                CorPrimaria = "#111827",
                CorSecundaria = "#374151",
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

        // Configuração da entidade Pessoa
        modelBuilder.Entity<Pessoa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Telefone).HasMaxLength(20);
            entity.Property(e => e.WhatsApp).HasMaxLength(20);
            entity.Property(e => e.TipoPessoa).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade PessoaPerfil
        modelBuilder.Entity<PessoaPerfil>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PessoaId).IsRequired();
            entity.Property(e => e.Perfil).IsRequired();
            entity.Property(e => e.DataInicio).IsRequired();

            entity.HasOne(p => p.Pessoa)
                  .WithMany(p => p.Perfis)
                  .HasForeignKey(p => p.PessoaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Visitante
        modelBuilder.Entity<Visitante>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PessoaId).IsRequired();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DataVisita).IsRequired();
            entity.Property(e => e.DataCadastro).IsRequired();

            entity.HasOne(v => v.Pessoa)
                  .WithMany(p => p.Visitantes)
                  .HasForeignKey(v => v.PessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Tenant)
                .WithMany()
                .HasForeignKey(v => v.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<ConfiguracaoCampanhaAniversario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.ImagemUrl).HasMaxLength(500);
            entity.Property(e => e.MensagemTemplate).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.HorarioEnvio).IsRequired();
            entity.Property(e => e.DataAtualizacao).IsRequired();
            entity.HasIndex(e => e.TenantId).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EnvioCampanhaAniversario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.AnoReferencia).IsRequired();
            entity.Property(e => e.DataAniversario).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Tentativas).IsRequired();
            entity.Property(e => e.WhatsAppUtilizado).HasMaxLength(20);
            entity.Property(e => e.ImagemUrlUtilizada).HasMaxLength(1000);
            entity.Property(e => e.MensagemUtilizada).HasMaxLength(4000);
            entity.Property(e => e.LogErro).HasMaxLength(1000);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.PessoaId, e.AnoReferencia }).IsUnique();

            entity.HasOne(e => e.Pessoa)
                .WithMany()
                .HasForeignKey(e => e.PessoaId)
                .OnDelete(DeleteBehavior.Cascade);

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
            entity.HasOne(e => e.Visitante)
                  .WithMany(v => v.MensagensAgendadas)
                  .HasForeignKey(e => e.VisitanteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ConfiguracaoMensagem)
                  .WithMany(c => c.MensagensAgendadas)
                  .HasForeignKey(e => e.ConfiguracaoMensagemId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.Status, e.DataEnvio });
            entity.HasIndex(e => new { e.TenantId, e.VisitanteId, e.DataEnvio });

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

            entity.HasOne(e => e.DestinatarioPessoa)
                .WithMany()
                .HasForeignKey(e => e.DestinatarioPessoaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DestinatarioVisitante)
                .WithMany()
                .HasForeignKey(e => e.DestinatarioVisitanteId)
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

            entity.HasIndex(e => new { e.PessoaId, e.Canal }).IsUnique();

            entity.HasOne(e => e.Pessoa)
                .WithMany()
                .HasForeignKey(e => e.PessoaId)
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
        modelBuilder.Entity<Equipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Area).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.LiderUsuario)
                  .WithMany()
                  .HasForeignKey(e => e.LiderUsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade HubCasa
        modelBuilder.Entity<HubCasa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EnderecoCompleto).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Anfitriao).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.AbertoPor)
                  .WithMany()
                  .HasForeignKey(e => e.AbertoPorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Lider)
                  .WithMany()
                  .HasForeignKey(e => e.LiderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Timoteo)
                  .WithMany()
                  .HasForeignKey(e => e.TimoteoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Fornecedor
        modelBuilder.Entity<Fornecedor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.RazaoSocial).HasMaxLength(200);
            entity.Property(e => e.CnpjCpf).HasMaxLength(20);
            entity.Property(e => e.InscricaoEstadual).HasMaxLength(30);
            entity.Property(e => e.Endereco).HasMaxLength(300);
            entity.Property(e => e.Telefone).HasMaxLength(30);
            entity.Property(e => e.Site).HasMaxLength(200);
            entity.Property(e => e.ContatoNome).HasMaxLength(150);
            entity.Property(e => e.ContatoCpf).HasMaxLength(20);
            entity.Property(e => e.ContatoWhatsApp).HasMaxLength(30);
            entity.Property(e => e.ContatoEmail).HasMaxLength(150);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade CategoriaDespesa
        modelBuilder.Entity<CategoriaDespesa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descricao).HasMaxLength(300);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade CategoriaReceita
        modelBuilder.Entity<CategoriaReceita>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descricao).HasMaxLength(300);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade ContaBancaria
        modelBuilder.Entity<ContaBancaria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Banco).HasMaxLength(100);
            entity.Property(e => e.Agencia).HasMaxLength(20);
            entity.Property(e => e.Conta).HasMaxLength(20);
            entity.Property(e => e.TipoConta).HasMaxLength(10);
            entity.Property(e => e.SaldoInicial).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade CentroCusto
        modelBuilder.Entity<CentroCusto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descricao).HasMaxLength(300);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Projeto
        modelBuilder.Entity<Projeto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Descricao).HasMaxLength(500);
            entity.Property(e => e.Orcamento).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CategoriaPatrimonio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descricao).HasMaxLength(300);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(
                new CategoriaPatrimonio { Id = 1, TenantId = Tenant.InitialTenantId, Nome = "Moveis", Descricao = "Moveis em geral", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 2, TenantId = Tenant.InitialTenantId, Nome = "Cadeiras e mesas", Descricao = "Cadeiras, mesas e similares", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 3, TenantId = Tenant.InitialTenantId, Nome = "Instrumentos musicais", Descricao = "Instrumentos e acessorios musicais", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 4, TenantId = Tenant.InitialTenantId, Nome = "Equipamentos de audio", Descricao = "Caixas, mesas, microfones e audio", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 5, TenantId = Tenant.InitialTenantId, Nome = "Equipamentos de video", Descricao = "Projetores, TVs, cameras e video", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 6, TenantId = Tenant.InitialTenantId, Nome = "Iluminacao", Descricao = "Refletores, spots e iluminacao", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 7, TenantId = Tenant.InitialTenantId, Nome = "Informatica", Descricao = "Notebooks, computadores e perifericos", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 8, TenantId = Tenant.InitialTenantId, Nome = "Eletrodomesticos", Descricao = "Geladeiras, micro-ondas e afins", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 9, TenantId = Tenant.InitialTenantId, Nome = "Veiculos", Descricao = "Carros, vans e motos", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 10, TenantId = Tenant.InitialTenantId, Nome = "Material infantil", Descricao = "Brinquedos, mobiliario e itens infantis", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 11, TenantId = Tenant.InitialTenantId, Nome = "Equipamentos de limpeza", Descricao = "Aspiradores, enceradeiras e afins", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 12, TenantId = Tenant.InitialTenantId, Nome = "Utensilios gerais", Descricao = "Itens de apoio e uso geral", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) },
                new CategoriaPatrimonio { Id = 13, TenantId = Tenant.InitialTenantId, Nome = "Patrimonio administrativo", Descricao = "Bens de escritorio e administracao", Ativo = true, DataCriacao = new DateTime(2026, 4, 1, 0, 0, 0) }
            );
        });

        modelBuilder.Entity<FinalidadeDoacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(140);
            entity.Property(e => e.DescricaoPublica).HasMaxLength(500);
            entity.Property(e => e.ImagemUrl).HasMaxLength(500);
            entity.Property(e => e.CorHex).HasMaxLength(40);
            entity.Property(e => e.ValoresSugeridos).HasMaxLength(300);
            entity.Property(e => e.ValorMinimo).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Ordem).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.VisivelPortal).IsRequired();
            entity.Property(e => e.PermiteAnonimo).IsRequired();
            entity.Property(e => e.PermitePix).IsRequired();
            entity.Property(e => e.PermiteCartaoCredito).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Ativo, e.VisivelPortal, e.Ordem });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CategoriaReceita)
                .WithMany()
                .HasForeignKey(e => e.CategoriaReceitaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ContaBancaria)
                .WithMany()
                .HasForeignKey(e => e.ContaBancariaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CentroCusto)
                .WithMany()
                .HasForeignKey(e => e.CentroCustoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Projeto)
                .WithMany()
                .HasForeignKey(e => e.ProjetoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DoacaoOnline>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.NomeDoador).IsRequired().HasMaxLength(120);
            entity.Property(e => e.WhatsApp).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(120);
            entity.Property(e => e.Documento).HasMaxLength(20);
            entity.Property(e => e.Anonima).IsRequired();
            entity.Property(e => e.Valor).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MetodoPagamento).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(40);
            entity.Property(e => e.ExternalPaymentId).HasMaxLength(120);
            entity.Property(e => e.ReciboToken).HasMaxLength(64);
            entity.Property(e => e.PixCopiaECola).HasMaxLength(2000);
            entity.Property(e => e.PixQrCodeUrl);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Status, e.DataCriacao });
            entity.HasIndex(e => new { e.TenantId, e.ExternalPaymentId });
            entity.HasIndex(e => new { e.TenantId, e.ReciboToken }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FinalidadeDoacao)
                .WithMany()
                .HasForeignKey(e => e.FinalidadeDoacaoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Pessoa)
                .WithMany()
                .HasForeignKey(e => e.PessoaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Receita)
                .WithMany()
                .HasForeignKey(e => e.ReceitaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GivingProviderConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.Environment).IsRequired();
            entity.Property(e => e.ApiKeyProtegida).HasMaxLength(4000);
            entity.Property(e => e.ApiKeyUltimosDigitos).HasMaxLength(40);
            entity.Property(e => e.WebhookUrl).HasMaxLength(500);
            entity.Property(e => e.WebhookSecretProtegido).HasMaxLength(200);
            entity.Property(e => e.PixEnabled).IsRequired();
            entity.Property(e => e.CreditCardEnabled).IsRequired();
            entity.Property(e => e.BoletoEnabled).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Provider }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PatrimonioItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Descricao).HasMaxLength(500);
            entity.Property(e => e.Marca).HasMaxLength(100);
            entity.Property(e => e.Modelo).HasMaxLength(100);
            entity.Property(e => e.NumeroSerie).HasMaxLength(100);
            entity.Property(e => e.Campus).HasMaxLength(100);
            entity.Property(e => e.Localizacao).HasMaxLength(150);
            entity.Property(e => e.MinisterioArea).HasMaxLength(100);
            entity.Property(e => e.TipoAquisicao).IsRequired().HasMaxLength(30);
            entity.Property(e => e.ValorAquisicao).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NumeroNotaFiscal).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.Property(e => e.EstadoConservacao).IsRequired().HasMaxLength(30);
            entity.Property(e => e.FotoUrl).HasMaxLength(500);
            entity.Property(e => e.DocumentoUrl).HasMaxLength(500);
            entity.Property(e => e.Observacoes).HasMaxLength(1000);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Codigo }).IsUnique();

            entity.HasOne(e => e.CategoriaPatrimonio)
                .WithMany(c => c.Itens)
                .HasForeignKey(e => e.CategoriaPatrimonioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResponsavelPessoa)
                .WithMany()
                .HasForeignKey(e => e.ResponsavelPessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Fornecedor)
                .WithMany()
                .HasForeignKey(e => e.FornecedorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Despesa)
                .WithMany()
                .HasForeignKey(e => e.DespesaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CentroCusto)
                .WithMany()
                .HasForeignKey(e => e.CentroCustoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Projeto)
                .WithMany()
                .HasForeignKey(e => e.ProjetoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PatrimonioMovimentacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.TipoMovimentacao).IsRequired().HasMaxLength(40);
            entity.Property(e => e.DataMovimentacao).IsRequired();
            entity.Property(e => e.Origem).HasMaxLength(150);
            entity.Property(e => e.Destino).HasMaxLength(150);
            entity.Property(e => e.ResponsavelOrigem).HasMaxLength(150);
            entity.Property(e => e.ResponsavelDestino).HasMaxLength(150);
            entity.Property(e => e.Observacoes).HasMaxLength(1000);
            entity.Property(e => e.UsuarioNome).HasMaxLength(150);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.PatrimonioItem)
                .WithMany(p => p.Movimentacoes)
                .HasForeignKey(e => e.PatrimonioItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.PatrimonioItemId, e.DataMovimentacao });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Despesa
        modelBuilder.Entity<Despesa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Descricao).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Valor).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.DataVencimento).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.ComprovanteUrl).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Fornecedor)
                  .WithMany()
                  .HasForeignKey(e => e.FornecedorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CategoriaDespesa)
                  .WithMany(c => c.Despesas)
                  .HasForeignKey(e => e.CategoriaDespesaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ContaBancaria)
                  .WithMany(c => c.Despesas)
                  .HasForeignKey(e => e.ContaBancariaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CentroCusto)
                  .WithMany(c => c.Despesas)
                  .HasForeignKey(e => e.CentroCustoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Projeto)
                  .WithMany(p => p.Despesas)
                  .HasForeignKey(e => e.ProjetoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Usuario)
                  .WithMany()
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.DataVencimento, e.Status });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Receita
        modelBuilder.Entity<Receita>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Descricao).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Valor).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.DataRecebimento).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.ComprovanteUrl).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.CategoriaReceita)
                  .WithMany(c => c.Receitas)
                  .HasForeignKey(e => e.CategoriaReceitaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ContaBancaria)
                  .WithMany(c => c.Receitas)
                  .HasForeignKey(e => e.ContaBancariaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CentroCusto)
                  .WithMany(c => c.Receitas)
                  .HasForeignKey(e => e.CentroCustoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Projeto)
                  .WithMany(p => p.Receitas)
                  .HasForeignKey(e => e.ProjetoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Usuario)
                  .WithMany()
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Pessoa)
                  .WithMany()
                  .HasForeignKey(e => e.PessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.DataRecebimento, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.PessoaId, e.DataRecebimento });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrcamentoCategoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Ano).IsRequired();
            entity.Property(e => e.Tipo).IsRequired();
            entity.Property(e => e.ValorOrcado).IsRequired().HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.CategoriaReceita)
                  .WithMany()
                  .HasForeignKey(e => e.CategoriaReceitaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CategoriaDespesa)
                  .WithMany()
                  .HasForeignKey(e => e.CategoriaDespesaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.Ano, e.Tipo });
        });

        // Configuração da entidade Cargo
        modelBuilder.Entity<Cargo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Voluntario
        modelBuilder.Entity<Voluntario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PessoaId).IsRequired();
            entity.Property(e => e.DataCadastro).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.PessoaId, e.EquipeId, e.CargoId }).IsUnique();

            entity.HasOne(v => v.Pessoa)
                  .WithMany(p => p.Voluntarios)
                  .HasForeignKey(v => v.PessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Equipe)
                  .WithMany(e => e.Voluntarios)
                  .HasForeignKey(v => v.EquipeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Cargo)
                  .WithMany(c => c.Voluntarios)
                  .HasForeignKey(v => v.CargoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Tenant)
                .WithMany()
                .HasForeignKey(v => v.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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
        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.ImagemDestaque).HasMaxLength(500);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.DataInicio).IsRequired();
            entity.Property(e => e.DataFim).IsRequired();
            entity.Property(e => e.Tipo).IsRequired().HasDefaultValue(TipoEvento.Evento);
            entity.Property(e => e.EhRecorrente).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.Ativo).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.AceitaInscricoes).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.ConfiguracaoFormularioInscricao).HasMaxLength(4000);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Titulo, e.DataInicio });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade EventoRecorrencia
        modelBuilder.Entity<EventoRecorrencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.DiaSemana).IsRequired();
            entity.Property(e => e.HoraInicio).IsRequired();
            entity.Property(e => e.Periodicidade).IsRequired();
            entity.Property(e => e.DataInicioVigencia).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Evento)
                  .WithMany(e => e.Recorrencias)
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.EventoId, e.Ativo });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade EventoOcorrencia
        modelBuilder.Entity<EventoOcorrencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.DataHoraInicio).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.GeradaAutomaticamente).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Evento)
                  .WithMany(e => e.Ocorrencias)
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EventoRecorrencia)
                  .WithMany(r => r.Ocorrencias)
                  .HasForeignKey(e => e.EventoRecorrenciaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.EventoId, e.DataHoraInicio });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Escala (uma por EventoOcorrencia + Equipe)
        modelBuilder.Entity<Escala>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.EventoOcorrencia)
                  .WithMany(eo => eo.Escalas)
                  .HasForeignKey(e => e.EventoOcorrenciaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Equipe)
                  .WithMany()
                  .HasForeignKey(e => e.EquipeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CriadoPorUsuario)
                  .WithMany()
                  .HasForeignKey(e => e.CriadoPorUsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.EventoOcorrenciaId, e.EquipeId }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade EscalaItem
        modelBuilder.Entity<EscalaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Ordem).IsRequired();
            entity.Property(e => e.ConflitoAprovado).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasDefaultValue(StatusEscalaItem.Pendente);
            entity.Property(e => e.MotivoExcecao).HasMaxLength(500);
            entity.Property(e => e.MotivoRecusa).HasMaxLength(500);
            entity.Property(e => e.ObservacaoOperacional).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Escala)
                  .WithMany(e => e.Itens)
                  .HasForeignKey(e => e.EscalaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Equipe)
                  .WithMany()
                  .HasForeignKey(e => e.EquipeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Cargo)
                  .WithMany()
                  .HasForeignKey(e => e.CargoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Pessoa)
                  .WithMany()
                  .HasForeignKey(e => e.PessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.VoluntarioId).IsRequired(false);
            entity.HasOne(e => e.Voluntario)
                  .WithMany()
                  .HasForeignKey(e => e.VoluntarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AprovadoPorUsuario)
                  .WithMany()
                  .HasForeignKey(e => e.AprovadoPorUsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RespondidoPorUsuario)
                  .WithMany()
                  .HasForeignKey(e => e.RespondidoPorUsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.EscalaId, e.PessoaId });
            entity.HasIndex(e => new { e.TenantId, e.EscalaId, e.VoluntarioId });
            entity.HasIndex(e => new { e.TenantId, e.EscalaId, e.EquipeId });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // EscalaModelo (modelo de escala por evento + equipe)
        modelBuilder.Entity<EscalaModelo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).HasMaxLength(200);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Evento)
                  .WithMany()
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Equipe)
                  .WithMany()
                  .HasForeignKey(e => e.EquipeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.EventoId, e.EquipeId }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EscalaModeloItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Quantidade).IsRequired();
            entity.Property(e => e.Ordem).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.EscalaModelo)
                  .WithMany(m => m.Itens)
                  .HasForeignKey(e => e.EscalaModeloId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Cargo)
                  .WithMany()
                  .HasForeignKey(e => e.CargoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.EscalaModeloId, e.Ordem });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IndisponibilidadeVoluntario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Voluntario)
                  .WithMany(v => v.Indisponibilidades)
                  .HasForeignKey(e => e.VoluntarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.VoluntarioId, e.Data }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SolicitacaoTrocaEscala>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.ObservacaoResposta).HasMaxLength(500);
            entity.Property(e => e.DataSolicitacao).IsRequired();

            entity.HasOne(e => e.EscalaItem)
                  .WithMany()
                  .HasForeignKey(e => e.EscalaItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.VoluntarioSolicitante)
                  .WithMany()
                  .HasForeignKey(e => e.VoluntarioSolicitanteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VoluntarioSubstituto)
                  .WithMany()
                  .HasForeignKey(e => e.VoluntarioSubstitutoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RespondidoPorUsuario)
                  .WithMany()
                  .HasForeignKey(e => e.RespondidoPorUsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.EscalaItemId, e.Status });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade DestaqueSite
        modelBuilder.Entity<DestaqueSite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Texto).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.Imagem).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Texto });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade ConfiguracaoPortal (singleton)
        modelBuilder.Entity<ConfiguracaoPortal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.TempoTransicaoCarrossel).IsRequired().HasDefaultValue(5);
            entity.Property(e => e.DataAtualizacao).IsRequired();
            entity.HasIndex(e => e.TenantId).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

        });

        // Configuração da entidade CategoriaNoticia
        modelBuilder.Entity<CategoriaNoticia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Noticia
        modelBuilder.Entity<Noticia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Texto).HasMaxLength(5000);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.Imagem).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(n => n.CategoriaNoticia)
                  .WithMany(c => c.Noticias)
                  .HasForeignKey(n => n.CategoriaNoticiaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Contato
        modelBuilder.Entity<Contato>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WhatsApp).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Membro).IsRequired();
            entity.Property(e => e.Mensagem).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade ConsentimentoRegistro (trilha de consentimento LGPD)
        modelBuilder.Entity<ConsentimentoRegistro>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Tipo).IsRequired();
            entity.Property(e => e.VersaoDocumento).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AceitoEm).IsRequired();
            entity.Property(e => e.IpOrigem).HasMaxLength(64);
            entity.Property(e => e.Origem).HasMaxLength(60);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Pessoa)
                .WithMany()
                .HasForeignKey(e => e.PessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ConcedidoPor)
                .WithMany()
                .HasForeignKey(e => e.ConcedidoPorPessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.PessoaId, e.Tipo });
        });

        // Configuração da entidade SolicitacaoTitular (requisições de titular - LGPD)
        modelBuilder.Entity<SolicitacaoTitular>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Tipo).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.NomeSolicitante).HasMaxLength(150);
            entity.Property(e => e.ContatoSolicitante).HasMaxLength(150);
            entity.Property(e => e.Canal).HasMaxLength(40);
            entity.Property(e => e.Descricao).HasMaxLength(2000);
            entity.Property(e => e.ResultadoObservacao).HasMaxLength(2000);
            entity.Property(e => e.SolicitadoEm).IsRequired();
            entity.Property(e => e.PrazoLimite).IsRequired();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Pessoa)
                .WithMany()
                .HasForeignKey(e => e.PessoaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.PrazoLimite });
        });

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
        modelBuilder.Entity<InscricaoEvento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WhatsApp).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.QuantidadeAcompanhantes).IsRequired();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DadosInscricao).HasMaxLength(2000);
            entity.Property(e => e.ObservacoesInternas).HasMaxLength(500);
            entity.Property(e => e.DataInscricao).IsRequired();

            entity.HasOne(i => i.Evento)
                  .WithMany(e => e.Inscricoes)
                  .HasForeignKey(i => i.EventoId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Índice para busca rápida
            entity.HasIndex(i => new { i.TenantId, i.EventoId, i.WhatsApp }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PessoaId).IsRequired();
            entity.Property(e => e.EmailLogin).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SenhaHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TipoUsuario).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.IsPlatformAdmin).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(u => u.Pessoa)
                  .WithOne(p => p.Usuario)
                  .HasForeignKey<Usuario>(u => u.PessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

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
        modelBuilder.Entity<CategoriaMidia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descricao).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade GaleriaFoto
        modelBuilder.Entity<GaleriaFoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.CaminhoDiretorio).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ImagemDestaque).HasMaxLength(500);
            entity.Property(e => e.QuantidadeFotos).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(g => g.Evento)
                  .WithMany()
                  .HasForeignKey(g => g.EventoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(g => g.CategoriaMidia)
                  .WithMany(c => c.Galerias)
                  .HasForeignKey(g => g.CategoriaMidiaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(g => new { g.TenantId, g.Nome, g.Data });

            entity.HasOne(g => g.Tenant)
                  .WithMany()
                  .HasForeignKey(g => g.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade GaleriaFotoItem (fotos listadas a partir do banco para funcionar com mesmo DB em dev)
        modelBuilder.Entity<GaleriaFotoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.NomeArquivo).IsRequired().HasMaxLength(260);
            entity.HasOne(e => e.GaleriaFoto)
                  .WithMany(g => g.Itens)
                  .HasForeignKey(e => e.GaleriaFotoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.GaleriaFotoId, e.NomeArquivo }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade CriancaDetalhe
        modelBuilder.Entity<CriancaDetalhe>(entity =>
        {
            entity.HasKey(e => e.PessoaId);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Alergias).HasMaxLength(500);
            entity.Property(e => e.RestricoesAlimentares).HasMaxLength(500);
            entity.Property(e => e.Observacoes).HasMaxLength(1000);
            entity.Property(e => e.SalaId).HasMaxLength(50);
            entity.Property(e => e.TurmaId).HasMaxLength(50);
            entity.Property(e => e.DataCadastro).IsRequired();

            entity.HasOne(c => c.Pessoa)
                  .WithOne(p => p.CriancaDetalhe)
                  .HasForeignKey<CriancaDetalhe>(c => c.PessoaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.PessoaId });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KidsSala>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(120);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KidsTurma>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.SalaId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(120);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Sala)
                  .WithMany(s => s.Turmas)
                  .HasForeignKey(e => e.SalaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.SalaId, e.Nome }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade ResponsavelCrianca
        modelBuilder.Entity<ResponsavelCrianca>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.CriancaPessoaId).IsRequired();
            entity.Property(e => e.ResponsavelPessoaId).IsRequired();
            entity.Property(e => e.PodeRetirar).IsRequired();
            entity.Property(e => e.Parentesco).HasMaxLength(50);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.DataCadastro).IsRequired();

            entity.HasOne(r => r.Crianca)
                  .WithMany(p => p.ResponsaveisComoCrianca)
                  .HasForeignKey(r => r.CriancaPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Responsavel)
                  .WithMany(p => p.ResponsaveisComoResponsavel)
                  .HasForeignKey(r => r.ResponsavelPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.TenantId, r.CriancaPessoaId, r.ResponsavelPessoaId }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade KidsCheckin
        modelBuilder.Entity<KidsCheckin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.CriancaPessoaId).IsRequired();
            entity.Property(e => e.CheckinTime).IsRequired();
            entity.Property(e => e.Metodo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CodigoSessao).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TokenRetirada).HasMaxLength(80);
            entity.Property(e => e.PinRetirada).HasMaxLength(10);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RetiradaMetodo).HasMaxLength(20);
            entity.Property(e => e.RetiradaMotivoExcecao).HasMaxLength(500);
            entity.Property(e => e.RetiradaPessoaNome).HasMaxLength(200);
            entity.Property(e => e.RetiradaPessoaDocumento).HasMaxLength(50);
            entity.Property(e => e.Observacoes).HasMaxLength(500);

            entity.HasOne(c => c.Crianca)
                  .WithMany(p => p.Checkins)
                  .HasForeignKey(c => c.CriancaPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CheckinBy)
                  .WithMany(p => p.CheckinsRealizadosPor)
                  .HasForeignKey(c => c.CheckinByPessoaId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.CheckoutBy)
                  .WithMany(p => p.CheckoutsRealizadosPor)
                  .HasForeignKey(c => c.CheckoutByPessoaId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(c => new { c.TenantId, c.CodigoSessao }).IsUnique();
            entity.HasIndex(c => new { c.TenantId, c.TokenRetirada });
            entity.HasIndex(c => new { c.TenantId, c.PinRetirada });
            entity.HasIndex(c => new { c.TenantId, c.CriancaPessoaId, c.Status });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KidsPreCheckin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.CriancaPessoaId).IsRequired();
            entity.Property(e => e.ResponsavelPessoaId).IsRequired();
            entity.Property(e => e.SalaId).HasMaxLength(50);
            entity.Property(e => e.TurmaId).HasMaxLength(50);
            entity.Property(e => e.QrToken).IsRequired().HasMaxLength(80);
            entity.Property(e => e.CodigoCurto).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ObservacoesResponsavel).HasMaxLength(500);
            entity.Property(e => e.CriadoEm).IsRequired();
            entity.Property(e => e.CancelamentoMotivo).HasMaxLength(500);

            entity.HasOne(e => e.Crianca)
                  .WithMany(p => p.PreCheckinsComoCrianca)
                  .HasForeignKey(e => e.CriancaPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Responsavel)
                  .WithMany(p => p.PreCheckinsComoResponsavel)
                  .HasForeignKey(e => e.ResponsavelPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EventoOcorrencia)
                  .WithMany()
                  .HasForeignKey(e => e.EventoOcorrenciaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Checkin)
                  .WithMany()
                  .HasForeignKey(e => e.CheckinId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ConfirmadoPor)
                  .WithMany(p => p.PreCheckinsConfirmadosPor)
                  .HasForeignKey(e => e.ConfirmadoPorPessoaId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.CanceladoPor)
                  .WithMany(p => p.PreCheckinsCanceladosPor)
                  .HasForeignKey(e => e.CanceladoPorPessoaId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => new { e.TenantId, e.QrToken }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.CodigoCurto }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.CriancaPessoaId, e.EventoOcorrenciaId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.ResponsavelPessoaId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.ExpiraEm, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.CheckinId });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KidsConteudoAula>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Tema).HasMaxLength(200);
            entity.Property(e => e.Versiculo).HasMaxLength(300);
            entity.Property(e => e.Resumo).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.AtividadeEmCasa).HasMaxLength(2000);
            entity.Property(e => e.ObservacaoResponsavel).HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DataReferencia).IsRequired();
            entity.Property(e => e.SalaId).HasMaxLength(50);
            entity.Property(e => e.TurmaId).HasMaxLength(50);
            entity.Property(e => e.CriadoEm).IsRequired();

            entity.HasOne(e => e.EventoOcorrencia)
                  .WithMany()
                  .HasForeignKey(e => e.EventoOcorrenciaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PublicadoPor)
                  .WithMany(p => p.ConteudosAulaPublicados)
                  .HasForeignKey(e => e.PublicadoPorPessoaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.Status, e.DataReferencia });
            entity.HasIndex(e => new { e.TenantId, e.SalaId, e.Status, e.DataReferencia });
            entity.HasIndex(e => new { e.TenantId, e.TurmaId, e.Status, e.DataReferencia });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KidsConteudoAulaAnexo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.ConteudoAulaId).IsRequired();
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.NomeExibicao).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.StoragePath).HasMaxLength(500);
            entity.Property(e => e.MimeType).HasMaxLength(120);
            entity.Property(e => e.CriadoEm).IsRequired();

            entity.HasOne(e => e.ConteudoAula)
                  .WithMany(c => c.Anexos)
                  .HasForeignKey(e => e.ConteudoAulaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.ConteudoAulaId, e.Ordem });
            entity.HasIndex(e => new { e.TenantId, e.Tipo });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade KidsNotificacao
        modelBuilder.Entity<KidsNotificacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.ResponsavelPessoaId).IsRequired();
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Origem).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Mensagem).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(n => n.Crianca)
                  .WithMany(p => p.NotificacoesComoCrianca)
                  .HasForeignKey(n => n.CriancaPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Responsavel)
                  .WithMany(p => p.NotificacoesComoResponsavel)
                  .HasForeignKey(n => n.ResponsavelPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => new { n.TenantId, n.CriancaPessoaId, n.Status });
            entity.HasIndex(n => new { n.TenantId, n.ResponsavelPessoaId, n.Status });
            entity.HasIndex(n => new { n.TenantId, n.ResponsavelPessoaId, n.LidoEm });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KidsOcorrencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.CriancaPessoaId).IsRequired();
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descricao).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SalaId).HasMaxLength(50);
            entity.Property(e => e.TurmaId).HasMaxLength(50);
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(o => o.Crianca)
                  .WithMany(p => p.KidsOcorrenciasComoCrianca)
                  .HasForeignKey(o => o.CriancaPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Checkin)
                  .WithMany()
                  .HasForeignKey(o => o.CheckinId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.RegistradoPor)
                  .WithMany(p => p.KidsOcorrenciasRegistradas)
                  .HasForeignKey(o => o.RegistradoPorPessoaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.ContatoResponsavelPor)
                  .WithMany(p => p.KidsOcorrenciasContatoResponsavel)
                  .HasForeignKey(o => o.ContatoResponsavelPorPessoaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.EncerradoPor)
                  .WithMany(p => p.KidsOcorrenciasEncerradas)
                  .HasForeignKey(o => o.EncerradoPorPessoaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(o => new { o.TenantId, o.CriancaPessoaId, o.DataCriacao });
            entity.HasIndex(o => new { o.TenantId, o.Status, o.DataCriacao });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade KidsDeviceToken
        modelBuilder.Entity<KidsDeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PessoaId).IsRequired();
            entity.Property(e => e.FcmToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(d => d.Pessoa)
                  .WithMany(p => p.KidsDeviceTokens)
                  .HasForeignKey(d => d.PessoaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(d => new { d.TenantId, d.PessoaId, d.FcmToken }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

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
        modelBuilder.Entity<Enquete>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.DataInicio).IsRequired();
            entity.Property(e => e.DataFim).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.PermitirMultiplaEscolha).IsRequired();
            entity.Property(e => e.PermitirVotoAnonimo).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.DataCriacao });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade EnqueteOpcao
        modelBuilder.Entity<EnqueteOpcao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.EnqueteId).IsRequired();
            entity.Property(e => e.Texto).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Ordem).IsRequired();
            entity.Property(e => e.DataCriacao).IsRequired();

            entity.HasOne(e => e.Enquete)
                  .WithMany(e => e.Opcoes)
                  .HasForeignKey(e => e.EnqueteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.EnqueteId, e.Ordem }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade EnqueteVoto
        modelBuilder.Entity<EnqueteVoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.EnqueteId).IsRequired();
            entity.Property(e => e.EnqueteOpcaoId).IsRequired();
            entity.Property(e => e.NomeAnonimo).HasMaxLength(100);
            entity.Property(e => e.DataVoto).IsRequired();

            entity.HasOne(e => e.Enquete)
                  .WithMany(e => e.Votos)
                  .HasForeignKey(e => e.EnqueteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Opcao)
                  .WithMany(e => e.Votos)
                  .HasForeignKey(e => e.EnqueteOpcaoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuario)
                  .WithMany()
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.TenantId, e.EnqueteId, e.UsuarioId });

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

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
