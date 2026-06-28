# WhatsFlow — Deploy (Docker / Coolify)

## Stack

| Serviço | Imagem/Build | Porta | Função |
|---|---|---|---|
| `postgres` | postgres:16-alpine | 5432 | Banco |
| `api` | `docker/Dockerfile.api` (.NET 10) | 8080 | API REST + aplica migrations + seed |
| `worker` | `docker/Dockerfile.worker` (.NET 10) | — | Processa fila/agendamentos |
| `admin` | `docker/Dockerfile.admin` (Vite→nginx) | 80 | SPA React |

Contexto de build da API/Worker = **raiz do repo** (referenciam `libs/`). Admin = `apps/admin`.

## Rodar local

```bash
cp .env.example .env        # ajuste senhas/JWT
docker compose up -d --build
```

- Admin: http://localhost:5173 · API: http://localhost:8080 · Swagger: http://localhost:8080/swagger
- A API aplica as migrations e semeia o tenant demo no boot (`Database__RunMigrations=true`, `Seed__DemoData=true`).
- **Login demo:** `admin@whatsflow.app` / `Whatsflow@2026`.
- Sem provider WhatsApp configurado, a conta demo usa o **provider Fake** (envios simulados).

## Variáveis de ambiente (principais)

| Var | Onde | Notas |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | api, worker | Host=postgres no compose |
| `Database__Provider` | api, worker | `PostgreSQL` |
| `Database__RunMigrations` | api | `true` na API (aplica no boot); `false` no worker |
| `Jwt__Key` | api | string longa/aleatória (HS256) |
| `Seed__DemoData` | api | `true` cria admin+conta Fake demo; `false` em prod limpa |
| `VITE_API_URL` | admin (build-arg) | **URL pública da API**, embutida no bundle |
| `EvolutionApi__*` | api, worker | Opcional; vazio = Fake |

> `VITE_API_URL` é build-time (Vite). Ao mudar a URL da API, **rebuild** do admin.

## Deploy no Coolify

1. **Banco**: crie um PostgreSQL gerenciado no Coolify (ou use o serviço `postgres` do compose). Anote host/porta/credenciais.
2. **Projeto via Docker Compose**: aponte o Coolify para este repositório e o `docker-compose.yml`. Alternativamente, crie 3 recursos separados (api/worker/admin) cada um com seu Dockerfile e contexto.
3. **Variáveis de ambiente** (no painel do Coolify, por serviço): defina as da tabela acima — em especial `ConnectionStrings__DefaultConnection`, `Jwt__Key` (segredo forte), e `VITE_API_URL` (domínio público da API) no build do admin.
4. **Domínios**: aponte `admin.seudominio` → serviço admin (porta 80) e `api.seudominio` → serviço api (porta 8080). Coolify provê TLS (Let's Encrypt).
5. **CORS**: adicione o domínio do admin em `Cors__AllowedOrigins__N` (env) ou na seção `Cors:AllowedOrigins` do appsettings da API.
6. **Migrations**: deixe `Database__RunMigrations=true` apenas na **API** (o worker não migra). Em produção, considere `Seed__DemoData=false` e criar o tenant/admin real via signup.
7. **Persistência**: garanta volume para o Postgres. Configure backup do banco (pendência herdada — ver `SAAS_READINESS`).

## Notas

- API e Worker compartilham a mesma connection string e o mesmo modelo (DbContext). Ao mudar o modelo, **redeploy de ambos**.
- Schedulers rodam **somente no Worker** (evita envio duplicado).
- Para WhatsApp real: configure uma `WhatsAppAccount` por tenant (provider Evolution/Cloud API) na tela "Conta WhatsApp"; o provider Fake é só para dev.
