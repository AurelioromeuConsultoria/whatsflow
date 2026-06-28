using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WhatsFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComunicacaoSegmentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PublicoAlvo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FiltrosJson = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacaoSegmentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComunicacaoTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Canal = table.Column<int>(type: "integer", nullable: false),
                    Assunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Corpo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CorpoHtml = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VariaveisPermitidas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProviderTemplateId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacaoTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventosWebhookBilling",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GatewayEventId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Evento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    GatewayPaymentId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    GatewaySubscriptionId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    Processado = table.Column<bool>(type: "boolean", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecebidoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosWebhookBilling", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Planos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrecoMensal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrecoAnual = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxUsuarios = table.Column<int>(type: "integer", nullable: true),
                    MaxMembros = table.Column<int>(type: "integer", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerificacoesEmail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConfirmadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificacoesEmail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComunicacaoAutomacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Gatilho = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SegmentoAlvo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Canal = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: true),
                    DelayMinutos = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacaoAutomacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicacaoAutomacoes_ComunicacaoTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ComunicacaoTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomeExibicao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FaviconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorPrimaria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CorSecundaria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlanoId = table.Column<int>(type: "integer", nullable: true),
                    LimiteMensalMensagens = table.Column<int>(type: "integer", nullable: false),
                    LimiteContatos = table.Column<int>(type: "integer", nullable: false),
                    FusoHorario = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsRootTenant = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_Planos_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "Planos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PlanoId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Ciclo = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MetodoPagamento = table.Column<int>(type: "integer", nullable: true),
                    TrialFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TrialAvisoEnviadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VigenciaInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProximaCobranca = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    InadimplenteDesde = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SuspensaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CanceladaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GatewayCustomerId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    GatewaySubscriptionId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Planos_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "Planos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UserEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesMensagens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TextoMensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DiasAposVisita = table.Column<int>(type: "integer", nullable: false),
                    HorarioEnvio = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesMensagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracoesMensagens_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TelefoneWhatsApp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Organizacao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Origem = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OptIn = table.Column<bool>(type: "boolean", nullable: false),
                    DataOptIn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataOptOut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contatos_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerfisAcesso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfisAcesso_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Cor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantDomains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDomains_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumberId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    BusinessAccountId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AccessTokenProtegido = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WebhookSecret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfiguracoesJson = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Faturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AssinaturaId = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Vencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PagaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GatewayPaymentId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LinkPagamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PixCopiaECola = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Faturas_Assinaturas_AssinaturaId",
                        column: x => x.AssinaturaId,
                        principalTable: "Assinaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Faturas_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComunicacaoPreferencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ContatoId = table.Column<int>(type: "integer", nullable: false),
                    Canal = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrigemConsentimento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacaoPreferencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicacaoPreferencias_Contatos_ContatoId",
                        column: x => x.ContatoId,
                        principalTable: "Contatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MensagensAgendadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ContatoId = table.Column<int>(type: "integer", nullable: false),
                    ConfiguracaoMensagemId = table.Column<int>(type: "integer", nullable: false),
                    DataAgendamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TextoFinal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DataProcessamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LogErro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensAgendadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensAgendadas_ConfiguracoesMensagens_ConfiguracaoMensa~",
                        column: x => x.ConfiguracaoMensagemId,
                        principalTable: "ConfiguracoesMensagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MensagensAgendadas_Contatos_ContatoId",
                        column: x => x.ContatoId,
                        principalTable: "Contatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensagensAgendadas_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerfisAcessoPermissoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PerfilAcessoId = table.Column<int>(type: "integer", nullable: false),
                    Recurso = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PodeVer = table.Column<bool>(type: "boolean", nullable: false),
                    PodeEditar = table.Column<bool>(type: "boolean", nullable: false),
                    PodeExcluir = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisAcessoPermissoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfisAcessoPermissoes_PerfisAcesso_PerfilAcessoId",
                        column: x => x.PerfilAcessoId,
                        principalTable: "PerfisAcesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerfisAcessoPermissoes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WhatsApp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DataNascimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EmailLogin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TipoUsuario = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    IsPlatformAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    PerfilAcessoId = table.Column<int>(type: "integer", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UltimoAcesso = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TentativasLoginFalhas = table.Column<int>(type: "integer", nullable: false),
                    BloqueadoAte = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_PerfisAcesso_PerfilAcessoId",
                        column: x => x.PerfilAcessoId,
                        principalTable: "PerfisAcesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuarios_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContatoTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ContatoId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContatoTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContatoTags_Contatos_ContatoId",
                        column: x => x.ContatoId,
                        principalTable: "Contatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContatoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppAccountId = table.Column<int>(type: "integer", nullable: true),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Erro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookEvents_WhatsAppAccounts_WhatsAppAccountId",
                        column: x => x.WhatsAppAccountId,
                        principalTable: "WhatsAppAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComunicacaoCampanhas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PublicoAlvo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: true),
                    SegmentoId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Origem = table.Column<int>(type: "integer", nullable: false),
                    DataAgendamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CriadoPorUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    TotalDestinatarios = table.Column<int>(type: "integer", nullable: false),
                    TotalEnviadas = table.Column<int>(type: "integer", nullable: false),
                    TotalFalhas = table.Column<int>(type: "integer", nullable: false),
                    TotalEntregues = table.Column<int>(type: "integer", nullable: false),
                    TotalLidas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacaoCampanhas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicacaoCampanhas_ComunicacaoSegmentos_SegmentoId",
                        column: x => x.SegmentoId,
                        principalTable: "ComunicacaoSegmentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComunicacaoCampanhas_ComunicacaoTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ComunicacaoTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComunicacaoCampanhas_Usuarios_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificacoesUsuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Link = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataLeitura = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacoesUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacoesUsuarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunicacaoCampanhaCanais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ComunicacaoCampanhaId = table.Column<int>(type: "integer", nullable: false),
                    Canal = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: true),
                    Prioridade = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacaoCampanhaCanais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicacaoCampanhaCanais_ComunicacaoCampanhas_ComunicacaoC~",
                        column: x => x.ComunicacaoCampanhaId,
                        principalTable: "ComunicacaoCampanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunicacaoCampanhaCanais_ComunicacaoTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ComunicacaoTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComunicacaoEntregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ComunicacaoCampanhaId = table.Column<int>(type: "integer", nullable: true),
                    ContatoId = table.Column<int>(type: "integer", nullable: true),
                    TemplateId = table.Column<int>(type: "integer", nullable: true),
                    Canal = table.Column<int>(type: "integer", nullable: false),
                    DestinoResolvido = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RemetenteResolvido = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConteudoFinal = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ConteudoHtmlFinal = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    MidiaUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Erro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChaveDedupe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AgendadoPara = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EntregueEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LidoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacaoEntregas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicacaoEntregas_ComunicacaoCampanhas_ComunicacaoCampanh~",
                        column: x => x.ComunicacaoCampanhaId,
                        principalTable: "ComunicacaoCampanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunicacaoEntregas_ComunicacaoTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ComunicacaoTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComunicacaoEntregas_Contatos_ContatoId",
                        column: x => x.ContatoId,
                        principalTable: "Contatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MessageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ComunicacaoEntregaId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Detalhe = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageLogs_ComunicacaoEntregas_ComunicacaoEntregaId",
                        column: x => x.ComunicacaoEntregaId,
                        principalTable: "ComunicacaoEntregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Planos",
                columns: new[] { "Id", "Ativo", "DataCriacao", "Descricao", "MaxMembros", "MaxUsuarios", "Nome", "Ordem", "PrecoAnual", "PrecoMensal", "Slug" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Para igrejas começando a se organizar.", null, null, "Essencial", 1, 499.00m, 49.90m, "essencial" },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Para igrejas em crescimento que precisam de gestão completa.", null, null, "Organização", 2, 999.00m, 99.90m, "organizacao" },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Para igrejas com múltiplos ministérios e alto volume.", null, null, "Crescimento", 3, 1999.00m, 199.90m, "crescimento" }
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Ativo", "CorPrimaria", "CorSecundaria", "DataAtualizacao", "DataCriacao", "Documento", "Email", "FaviconUrl", "FusoHorario", "IsRootTenant", "LimiteContatos", "LimiteMensalMensagens", "LogoUrl", "Nome", "NomeExibicao", "PlanoId", "Slug", "Status", "Telefone" },
                values: new object[] { 1, true, "#25D366", "#075E54", null, new DateTime(2026, 4, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "America/Sao_Paulo", true, 0, 0, null, "WhatsFlow Demo", "WhatsFlow Demo", null, "demo", 1, null });

            migrationBuilder.InsertData(
                table: "ConfiguracoesMensagens",
                columns: new[] { "Id", "Ativo", "DataCriacao", "DiasAposVisita", "HorarioEnvio", "Nome", "TenantId", "TextoMensagem" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, new TimeSpan(0, 10, 0, 0, 0), "Boas-vindas", 1, "Olá {Nome}! Que alegria ter você conosco na igreja! Esperamos vê-lo novamente em breve. Deus abençoe!" },
                    { 2, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, new TimeSpan(0, 18, 0, 0, 0), "Convite para retorno", 1, "Oi {Nome}! Sentimos sua falta na igreja. Que tal nos visitar novamente neste domingo? Será um prazer recebê-lo!" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_GatewaySubscriptionId",
                table: "Assinaturas",
                column: "GatewaySubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_PlanoId",
                table: "Assinaturas",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_TenantId",
                table: "Assinaturas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoAutomacoes_TemplateId",
                table: "ComunicacaoAutomacoes",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoCampanhaCanais_ComunicacaoCampanhaId",
                table: "ComunicacaoCampanhaCanais",
                column: "ComunicacaoCampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoCampanhaCanais_TemplateId",
                table: "ComunicacaoCampanhaCanais",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoCampanhas_CriadoPorUsuarioId",
                table: "ComunicacaoCampanhas",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoCampanhas_SegmentoId",
                table: "ComunicacaoCampanhas",
                column: "SegmentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoCampanhas_TemplateId",
                table: "ComunicacaoCampanhas",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoEntregas_ChaveDedupe",
                table: "ComunicacaoEntregas",
                column: "ChaveDedupe");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoEntregas_ComunicacaoCampanhaId",
                table: "ComunicacaoEntregas",
                column: "ComunicacaoCampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoEntregas_ContatoId",
                table: "ComunicacaoEntregas",
                column: "ContatoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoEntregas_Status_Canal_DataCriacao",
                table: "ComunicacaoEntregas",
                columns: new[] { "Status", "Canal", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoEntregas_TemplateId",
                table: "ComunicacaoEntregas",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacaoPreferencias_ContatoId_Canal",
                table: "ComunicacaoPreferencias",
                columns: new[] { "ContatoId", "Canal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesMensagens_TenantId_Nome",
                table: "ConfiguracoesMensagens",
                columns: new[] { "TenantId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_Contatos_TenantId_TelefoneWhatsApp",
                table: "Contatos",
                columns: new[] { "TenantId", "TelefoneWhatsApp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContatoTags_ContatoId_TagId",
                table: "ContatoTags",
                columns: new[] { "ContatoId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContatoTags_TagId",
                table: "ContatoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosWebhookBilling_GatewayEventId",
                table: "EventosWebhookBilling",
                column: "GatewayEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_AssinaturaId",
                table: "Faturas",
                column: "AssinaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_GatewayPaymentId",
                table: "Faturas",
                column: "GatewayPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_TenantId_Status",
                table: "Faturas",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensAgendadas_ConfiguracaoMensagemId",
                table: "MensagensAgendadas",
                column: "ConfiguracaoMensagemId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensAgendadas_ContatoId",
                table: "MensagensAgendadas",
                column: "ContatoId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensAgendadas_TenantId_ContatoId_DataEnvio",
                table: "MensagensAgendadas",
                columns: new[] { "TenantId", "ContatoId", "DataEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensAgendadas_TenantId_Status_DataEnvio",
                table: "MensagensAgendadas",
                columns: new[] { "TenantId", "Status", "DataEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_ComunicacaoEntregaId",
                table: "MessageLogs",
                column: "ComunicacaoEntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesUsuarios_UsuarioId_DataLeitura_DataCriacao",
                table: "NotificacoesUsuarios",
                columns: new[] { "UsuarioId", "DataLeitura", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_PerfisAcesso_TenantId_Nome",
                table: "PerfisAcesso",
                columns: new[] { "TenantId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfisAcessoPermissoes_PerfilAcessoId",
                table: "PerfisAcessoPermissoes",
                column: "PerfilAcessoId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfisAcessoPermissoes_TenantId_PerfilAcessoId_Recurso",
                table: "PerfisAcessoPermissoes",
                columns: new[] { "TenantId", "PerfilAcessoId", "Recurso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planos_Slug",
                table: "Planos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TenantId_Nome",
                table: "Tags",
                columns: new[] { "TenantId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_Domain",
                table: "TenantDomains",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TenantId",
                table: "TenantDomains",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PlanoId",
                table: "Tenants",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmailLogin",
                table: "Usuarios",
                column: "EmailLogin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PerfilAcessoId",
                table: "Usuarios",
                column: "PerfilAcessoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacoesEmail_Token",
                table: "VerificacoesEmail",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Provider_ProviderMessageId",
                table: "WebhookEvents",
                columns: new[] { "Provider", "ProviderMessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_TenantId_Status_CriadoEm",
                table: "WebhookEvents",
                columns: new[] { "TenantId", "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_WhatsAppAccountId",
                table: "WebhookEvents",
                column: "WhatsAppAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppAccounts_TenantId_Nome",
                table: "WhatsAppAccounts",
                columns: new[] { "TenantId", "Nome" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ComunicacaoAutomacoes");

            migrationBuilder.DropTable(
                name: "ComunicacaoCampanhaCanais");

            migrationBuilder.DropTable(
                name: "ComunicacaoPreferencias");

            migrationBuilder.DropTable(
                name: "ContatoTags");

            migrationBuilder.DropTable(
                name: "EventosWebhookBilling");

            migrationBuilder.DropTable(
                name: "Faturas");

            migrationBuilder.DropTable(
                name: "MensagensAgendadas");

            migrationBuilder.DropTable(
                name: "MessageLogs");

            migrationBuilder.DropTable(
                name: "NotificacoesUsuarios");

            migrationBuilder.DropTable(
                name: "PerfisAcessoPermissoes");

            migrationBuilder.DropTable(
                name: "TenantDomains");

            migrationBuilder.DropTable(
                name: "VerificacoesEmail");

            migrationBuilder.DropTable(
                name: "WebhookEvents");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Assinaturas");

            migrationBuilder.DropTable(
                name: "ConfiguracoesMensagens");

            migrationBuilder.DropTable(
                name: "ComunicacaoEntregas");

            migrationBuilder.DropTable(
                name: "WhatsAppAccounts");

            migrationBuilder.DropTable(
                name: "ComunicacaoCampanhas");

            migrationBuilder.DropTable(
                name: "Contatos");

            migrationBuilder.DropTable(
                name: "ComunicacaoSegmentos");

            migrationBuilder.DropTable(
                name: "ComunicacaoTemplates");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "PerfisAcesso");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Planos");
        }
    }
}
