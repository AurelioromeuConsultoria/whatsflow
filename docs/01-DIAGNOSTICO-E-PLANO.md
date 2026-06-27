# WhatsFlow — Etapa 1: Diagnóstico e Plano de Adaptação

> Base analisada: `/Users/aurelioromeu/repos/AppIgreja` (produto "Verbo+").
> Destino: `/Users/aurelioromeu/repos/Malach/WhatsFlow`.
> Status: **diagnóstico — nada foi alterado ainda.**

---

## 0. Conclusão executiva

O AppIgreja **não é** "um front-end consumindo uma API .NET". Ele já é, na prática,
um **SaaS multi-tenant de comunicação com WhatsApp** — só que vestido de sistema de igreja.

Já existe e está **funcional**:

- Multi-tenancy real (isolamento por `TenantId`, query filters globais, carimbo automático no `SaveChanges`).
- Autenticação JWT + refresh, lockout, RBAC por recurso/ação (`PerfilAcesso`/`PerfilAcessoPermissao`).
- **Integração WhatsApp via Evolution API** + abstração de canal (`IComunicacaoCanalProvider`).
- Domínio de **Campanha / Template / Entrega / Segmento / Automação** (família `Comunicacao*`).
- **Worker** (`BackgroundWorker`) com schedulers de mensagens/campanhas.
- Auditoria (`AuditLog`), billing/assinatura (Plano/Assinatura/Fatura), health checks, Sentry.
- Front React 19 + Vite + Tailwind 4 + shadcn/ui, com telas de **Campanhas, Templates, Segmentos,
  Mensagens Agendadas**, multi-tenant no client (header `X-Tenant-Id`), tema, i18n, RBAC no menu.

**Portanto o trabalho do WhatsFlow é majoritariamente de **extração e poda**, não de construção:**
copiar o núcleo SaaS+WhatsApp, **renomear** `SistemaIgreja*`→`WhatsFlow*`, **remover** o domínio de
igreja (Kids, Eventos, Escalas, Voluntários, Financeiro, Portal/Notícias, Enquetes, Galerias) e
**renomear/ajustar** `Pessoa`→`Contato` e a família `Comunicacao*`→linguagem de produto
(Campanha, Template, Fila de Mensagens, Conta WhatsApp).

Reaproveitamento estimado: **~65–75% do backend e ~70% do frontend** prontos ou com pequena adaptação.

---

## 1. Tecnologias encontradas

### Backend (`AppIgreja/BackEnd`) — Clean Architecture, .NET 10

| Camada | Projeto | Conteúdo |
|---|---|---|
| Host API | `SistemaIgreja.API` | 66+ controllers, Middleware (Permissão, Subscription gating), Permissions, health checks, Swagger |
| Aplicação | `SistemaIgreja.Application` | Services, ~58 repositórios (interfaces+impl no Infra), ~71 DTOs, Configuration, Security |
| Domínio | `SistemaIgreja.Domain` | ~85 entidades, enums, `ITenantEntity` |
| Infra | `SistemaIgreja.Infrastructure` | `SistemaIgrejaDbContext` (~2.270 linhas), **132 migrations**, repositórios |
| Worker | `SistemaIgreja.BackgroundWorker` | `IHostedService` schedulers (mensagens, aniversário, escala, billing) |
| Testes | `tests/SistemaIgreja.API.Tests` | inclui testes de isolamento de tenant |

- **TFM real: `net10.0`** em todos os projetos (⚠️ você pediu .NET 9 — ver Riscos §6).
- EF Core 9 + **Npgsql (PostgreSQL)** — e também provider SQL Server (selecionável por config `Database:Provider`).
- Auth: `JwtBearer` + `System.IdentityModel.Tokens.Jwt` + **BCrypt**.
- Sem MediatR / AutoMapper / FluentValidation — **mapeamento e validação manuais** (padrão simples, fácil de seguir).
- Sentry (API + Worker), Swashbuckle, FirebaseAdmin (push), AWSSDK.S3, ImageSharp.
- Convenções: `[ApiController]` + `Route("api/[controller]")` + `[Authorize]`; paginação via `PagedResultDto<T>`.

### Frontend (`AppIgreja/FrontEnd`) — React 19 + Vite

- **Vite 6** + **React 19** + **JavaScript (sem TS)**.
- **Tailwind CSS 4** + **shadcn/ui** sobre **Radix** (58 componentes em `components/ui`).
- Router: `react-router-dom 7` (lazy + `Suspense`); estado por **Context API** (sem Redux/Zustand).
- HTTP: **Axios** com instância única + interceptors (JWT, refresh em 401, `402→/billing`, headers `X-Tenant-Id/Slug`).
- i18n: **i18next** (pt-BR padrão, en-US, es-ES); tema dark/light/`verbo` (`next-themes`); Recharts; Sentry; Vitest.
- ~36 grupos de páginas; guardas `ProtectedRoute` + `RequirePermission`.

---

## 2. Mapa de reaproveitamento

### ✅ Reusar praticamente como está (núcleo SaaS)

| Recurso | Onde (AppIgreja) |
|---|---|
| Multi-tenancy (`ITenantEntity`, query filters, stamp) | `Domain/Entities/ITenantEntity.cs`, `Infrastructure/Data/SistemaIgrejaDbContext.cs` |
| Resolução de tenant por request (JWT + header) | `API/Services/HttpTenantContext.cs`, `Application/Services/ITenantContext.cs` |
| Auth JWT + refresh + lockout | `Application/Services/AuthService.cs` |
| RBAC (perfil × recurso × ver/editar/excluir) | `Domain/Entities/PerfilAcesso.cs`, `API/Permissions/PermissionMiddleware.cs` |
| Auditoria | `Domain/Entities/AuditLog.cs`, `Application/Services/AuditLogService.cs` |
| **Abstração de canal de envio** | `Application/Services/ComunicacaoCanalProviders.cs` (`IComunicacaoCanalProvider`) |
| **Integração WhatsApp (Evolution)** | `Application/Services/EvolutionApiService.cs`, `Application/Configuration/EvolutionApiSettings.cs` |
| Worker + schedulers | `SistemaIgreja.BackgroundWorker/Program.cs`, `Infrastructure/Services/MessageSchedulerService.cs` |
| Billing/planos | `Domain/Entities/Plano.cs`, `Assinatura.cs`, `SubscriptionGatingMiddleware` |
| **Front: AuthContext, apiClient, Layout, ui/, ProtectedRoute, tema, i18n** | `FrontEnd/src/context`, `lib/apiClient.js`, `components/Layout`, `components/ui` |
| **Front: Campanhas / Templates / Segmentos / Mensagens Agendadas** | `FrontEnd/src/pages/Comunicacao`, `pages/MensagensAgendadas` |

### ⚙️ Renomear / adaptar (mapa de domínio)

| WhatsFlow (spec) | Origem no AppIgreja | Observação |
|---|---|---|
| `Contato` | `Pessoa` / `Visitante` / `Contato` | poda de campos de igreja; manter telefone/opt-in/tags |
| `Tag` + `ContatoTag` | (não existe — há `Segmento`) | criar (entidade simples) |
| `Template` | `ComunicacaoTemplate` | **já tem `Versao`, `VariaveisPermitidas`, `Status` (Rascunho…)** ✅ |
| `Campanha` | `ComunicacaoCampanha` | status, agendamento, contadores |
| `Fila de mensagens` / `message_logs` | `ComunicacaoEntrega` | já é fila DB com status Pendente→Reservado→Enviado/Entregue/Falhou |
| `Segmentação` | `ComunicacaoSegmento` | trocar regras de igreja por Tags/Opt-in/Status/Origem |
| `Conta WhatsApp / Provider` | `ConfiguracaoMensagem` + `EvolutionApiSettings` | virar entidade por-tenant com `Provider`, token protegido, webhook secret |
| `webhook_events` | (não existe como entidade) | **criar** (armazenar evento bruto + processamento) |
| `Automação` | `ComunicacaoAutomacao` | já existe; base para follow-up/cobrança/etc. |
| `Auditoria`, `Tenant`, `Usuario`, `Perfil` | iguais | reusar |

### ❌ Descartar (domínio de igreja)

Kids (todas), Eventos/Inscrições, Escalas/Voluntários/Equipes/Cargos, Financeiro
(Despesa/Receita/Contas/Centros/Projetos/Patrimônio), Portal (Notícias/Destaques/Galerias/Enquetes),
Pessoa-específicos. No front: as páginas correspondentes.

---

## 3. Arquitetura proposta (monorepo)

Atende ao seu pedido (`apps/` com `api` e `admin`) e mantém **API e Worker separáveis**:

```
WhatsFlow/
├─ apps/
│  ├─ api/                # WhatsFlow.Api  — host ASP.NET Core (Web API)
│  ├─ worker/             # WhatsFlow.Worker — host de background (separável/independente)
│  └─ admin/              # SPA React (Vite) — ex-"FrontEnd"
├─ libs/                  # bibliotecas .NET compartilhadas (API e Worker referenciam)
│  ├─ WhatsFlow.Domain
│  ├─ WhatsFlow.Application
│  └─ WhatsFlow.Infrastructure
├─ tests/
│  └─ WhatsFlow.Tests
├─ docs/
├─ docker/                # Dockerfiles + compose
├─ WhatsFlow.sln
└─ docker-compose.yml
```

> **Decisão a confirmar:** se você preferir tudo o que é .NET sob `apps/api`, dá para colapsar
> `libs/` dentro de `apps/api/src` e o worker em `apps/api/worker`. Recomendo `libs/` separado
> (mantém API e Worker como hosts magros sobre as mesmas libs — exatamente o "separável depois" que você pediu).

### ✔ Decisões confirmadas (2026-06-26)

- **.NET 10** mantido (sem downgrade).
- **Fila em banco + Worker** (sem Redis/Hangfire no MVP).
- Layout **`apps/{api,worker}` + `libs/`** (hosts magros sobre libs compartilhadas).

### Decisões técnicas recomendadas

1. **Fork seletivo, não in-place.** O `AppIgreja/BackEnd` tem `.git` próprio. WhatsFlow começa
   **git limpo**, copiando só o núcleo e podando. Não tocamos no AppIgreja.
2. **Fila: manter a fila em banco já existente (`ComunicacaoEntrega`/`message_queue`), processada pelo Worker.**
   Já é uma fila transacional com reserva de item e status — robusta e simples. **Não introduzir
   Redis+Hangfire no MVP** (evita overengineering, sua regra #10). Adicionar **lock distribuído**
   (advisory lock do Postgres ou Redis) só quando rodar Worker em mais de uma instância. Hangfire/Redis
   ficam como evolução documentada, não pré-requisito.
3. **`IWhatsAppProvider`.** Evoluir o atual `IComunicacaoCanalProvider` para a interface do produto
   (`SendTemplateMessageAsync`, `SendTextMessageAsync`, `GetMessageStatusAsync`, `ValidateWebhookAsync`,
   `ParseWebhookAsync`). Implementações: **Evolution (já existe)**, **Fake/Mock (novo, p/ dev)**, e
   **Cloud API oficial** preparada. Regra de negócio nunca acopla ao provider.
4. **Schedulers só no Worker** (corrige o risco de envio duplicado apontado no `SAAS_READINESS.md`).
5. **Segredos do provider protegidos** (`AccessToken` criptografado em repouso) e webhook com segredo/assinatura.
6. **Refresh tokens fora de memória** (hoje em memória) → tabela no Postgres.
7. **Branding:** `SistemaIgreja*`→`WhatsFlow*`, tema `verbo`→`whatsflow`, `app.name` nos locales.

---

## 4. Modelo de dados alvo (entidades/tabelas do MVP)

`tenants`, `users`, `roles`(perfis)+`role_permissions`, `contacts`, `tags`, `contact_tags`,
`templates`, `campaigns`, `campaign_recipients`, `message_queue`, `message_logs`,
`whatsapp_accounts`, `webhook_events`, `audit_logs`, `plans`+`subscriptions`.

Relacionamentos-chave: tudo operacional carrega `TenantId`; `campaign → template`,
`campaign → segment`, `campaign → campaign_recipients → message_queue → message_logs`,
`contact ⇄ tag` (N:N), `whatsapp_account → tenant` (1:1/1:N), `webhook_events → message`.

### Fluxos

- **Envio:** Worker pega item `Pendente` da fila → reserva (`Reservado`) → `IWhatsAppProvider.Send*` →
  grava `ProviderMessageId`/erro → status `Enviado`; webhook depois move p/ `Entregue`/`Lido`/`Falhou`;
  retentativa com `RetryCount` e backoff; respeita rate limit e limite do plano.
- **Campanha:** valida template `Approved` + conta WhatsApp ativa → resolve segmentação → filtra
  elegíveis (**opt-in**) → gera `campaign_recipients` → enfileira em `message_queue` → atualiza
  contadores; suporta pausar/cancelar/retomar.
- **Webhook:** endpoint recebe → **salva evento bruto** (`webhook_events`) → valida assinatura →
  processa idempotente → atualiza status da mensagem. Falha de processamento não perde o evento.

---

## 5. Plano de implementação (Etapas 2→6)

| Etapa | Entrega | Esforço aprox. |
|---|---|---|
| **2. Planejamento técnico** | ADRs (fila, provider, TFM), diagrama de entidades/fluxos, estrutura de pastas final | 0,5 sem |
| **3. Scaffolding + Banco** | criar monorepo `apps/`+`libs/`, copiar núcleo, **renomear namespaces**, podar domínio igreja; consolidar migrations PostgreSQL das 16 tabelas | 1–1,5 sem |
| **4. Backend** | `Contato`/`Tag`/`WhatsAppAccount`/`WebhookEvent`; `IWhatsAppProvider`+Fake; controllers de contato/tag/template/campanha/dashboard/webhook; schedulers no Worker; limites de plano | 2–3 sem |
| **5. Frontend (admin)** | rebrand; telas login, dashboard, contatos, tags, templates, campanhas+detalhe, conta WhatsApp, logs de mensagens (reusando Comunicação) | 2–3 sem |
| **6. Docker/Coolify** | Dockerfile api/worker/admin, `docker-compose.yml` (Postgres + Redis opcional + Evolution), envs, guia Coolify | 0,5–1 sem |

---

## 6. Riscos e cuidados

1. **TFM .NET 10 vs. pedido de .NET 9.** A base toda está em `net10.0`. Manter .NET 10 é o caminho de
   menor atrito; rebaixar p/ .NET 9 é possível mas dá retrabalho. **Decisão sua.**
2. **Vazamento de tenant na Comunicação** — já corrigido em código no AppIgreja (migration
   `AdicionarTenantIdComunicacaoNotificacoes`). O fork **deve incluir** essa correção. (ref. `SAAS_READINESS.md` item 1).
3. **Schedulers duplicados** (API+Worker) → rodar só no Worker.
4. **Provider real**: começar com Fake; Evolution já funciona; Cloud API oficial exige verificação Meta/templates aprovados.
5. **Uploads/backup/DR** continuam pendentes no AppIgreja — herdar como tarefas, não como bloqueio do MVP.
6. **Renome em massa** (`SistemaIgreja`→`WhatsFlow`) é mecânico mas extenso (namespaces, `using`, DbContext, 132 migrations). Fazer num passo controlado e compilando ao final.

---

## 7. O que **não** vou fazer sem confirmação

- Tocar em `/Users/aurelioromeu/repos/AppIgreja` (somente leitura).
- Remover qualquer funcionalidade sem listar antes.
- Escolher .NET 9 vs 10, layout do Worker, e estratégia de fila sem seu aval (ver §3 e §6).
