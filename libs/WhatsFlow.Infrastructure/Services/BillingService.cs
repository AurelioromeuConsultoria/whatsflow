using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly WhatsFlowDbContext _context;
    private readonly IAsaasBillingClient _asaas;
    private readonly BillingSettings _billing;
    private readonly AsaasBillingSettings _asaasSettings;
    private readonly ITenantContext _tenantContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        WhatsFlowDbContext context,
        IAsaasBillingClient asaas,
        IOptions<BillingSettings> billing,
        IOptions<AsaasBillingSettings> asaasSettings,
        ITenantContext tenantContext,
        IEmailService emailService,
        ILogger<BillingService> logger)
    {
        _context = context;
        _asaas = asaas;
        _billing = billing.Value;
        _asaasSettings = asaasSettings.Value;
        _tenantContext = tenantContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<PlanoDto>> ListarPlanosAsync()
    {
        return await _context.Planos
            .Where(p => p.Ativo)
            .OrderBy(p => p.Ordem)
            .Select(p => new PlanoDto
            {
                Id = p.Id,
                Nome = p.Nome,
                Slug = p.Slug,
                Descricao = p.Descricao,
                PrecoMensal = p.PrecoMensal,
                PrecoAnual = p.PrecoAnual,
                Ordem = p.Ordem
            })
            .ToListAsync();
    }

    public async Task<AssinaturaDto> AssinarAsync(AssinarTenantDto dto)
    {
        var tenantId = dto.TenantId ?? _tenantContext.TenantId ?? Tenant.InitialTenantId;
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var existente = await _context.Assinaturas
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Status != StatusAssinatura.Cancelada);
            if (existente != null)
            {
                throw new InvalidOperationException("Este tenant já possui uma assinatura ativa.");
            }

            var plano = await _context.Planos.FirstOrDefaultAsync(p => p.Id == dto.PlanoId && p.Ativo)
                ?? throw new ArgumentException("Plano inválido ou inativo.");

            var valor = dto.Ciclo == CicloCobranca.Anual
                ? (plano.PrecoAnual ?? plano.PrecoMensal * 12)
                : plano.PrecoMensal;

            var agora = DateTime.UtcNow;
            var trialFim = agora.AddDays(_billing.TrialDias);

            string? customerId = null;
            string? subscriptionId = null;

            if (_asaas.Configurado)
            {
                var cust = await _asaas.CreateCustomerAsync(new AsaasCustomerRequest
                {
                    Nome = dto.NomeCliente,
                    Email = dto.Email,
                    CpfCnpj = dto.CpfCnpj,
                    Telefone = dto.Telefone
                });
                if (!cust.Success)
                {
                    throw new InvalidOperationException($"Falha ao criar cliente no gateway: {cust.ErrorMessage}");
                }
                customerId = cust.CustomerId;

                // billingType UNDEFINED: o pagador escolhe (PIX/boleto/cartão) na 1ª fatura
                // após o trial — não exige cartão no início (decisão do projeto).
                var sub = await _asaas.CreateSubscriptionAsync(new AsaasSubscriptionRequest
                {
                    CustomerId = customerId!,
                    Valor = valor,
                    Ciclo = dto.Ciclo,
                    PrimeiroVencimento = trialFim,
                    Descricao = $"VerboPlus - Plano {plano.Nome}",
                    BillingType = "UNDEFINED"
                });
                if (!sub.Success)
                {
                    throw new InvalidOperationException($"Falha ao criar assinatura no gateway: {sub.ErrorMessage}");
                }
                subscriptionId = sub.SubscriptionId;
            }
            else
            {
                _logger.LogWarning("Asaas billing não configurado — criando assinatura trial local sem gateway (tenant {TenantId}).", tenantId);
            }

            var assinatura = new Assinatura
            {
                TenantId = tenantId,
                PlanoId = plano.Id,
                Status = StatusAssinatura.Trial,
                Ciclo = dto.Ciclo,
                Valor = valor,
                MetodoPagamento = dto.MetodoPagamento,
                TrialFim = trialFim,
                ProximaCobranca = trialFim,
                GatewayCustomerId = customerId,
                GatewaySubscriptionId = subscriptionId,
                DataCriacao = agora
            };

            _context.Assinaturas.Add(assinatura);
            await _context.SaveChangesAsync();

            return Map(assinatura, plano.Nome);
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<AssinaturaDto?> ObterPorTenantAsync(int tenantId)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var item = await _context.Assinaturas
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.DataCriacao)
                .Select(a => new { Assinatura = a, PlanoNome = a.Plano.Nome })
                .FirstOrDefaultAsync();

            return item == null ? null : Map(item.Assinatura, item.PlanoNome);
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<IEnumerable<AssinaturaDto>> ListarTodasAsync()
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var itens = await _context.Assinaturas
                .OrderByDescending(a => a.DataCriacao)
                .Select(a => new { Assinatura = a, PlanoNome = a.Plano.Nome, TenantNome = a.Tenant.Nome })
                .ToListAsync();

            return itens.Select(i =>
            {
                var dto = Map(i.Assinatura, i.PlanoNome);
                dto.TenantNome = i.TenantNome;
                return dto;
            }).ToList();
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<AssinaturaDto?> SuspenderAsync(int tenantId)
    {
        return await AlterarStatusAdminAsync(tenantId, StatusAssinatura.Suspensa);
    }

    public async Task<AssinaturaDto?> ReativarAsync(int tenantId)
    {
        return await AlterarStatusAdminAsync(tenantId, StatusAssinatura.Ativa);
    }

    private async Task<AssinaturaDto?> AlterarStatusAdminAsync(int tenantId, StatusAssinatura novoStatus)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var assinatura = await _context.Assinaturas
                .Where(a => a.TenantId == tenantId && a.Status != StatusAssinatura.Cancelada)
                .OrderByDescending(a => a.DataCriacao)
                .FirstOrDefaultAsync();
            if (assinatura == null)
            {
                return null;
            }

            var agora = DateTime.UtcNow;
            assinatura.Status = novoStatus;
            assinatura.DataAtualizacao = agora;
            if (novoStatus == StatusAssinatura.Suspensa)
            {
                assinatura.SuspensaEm = agora;
            }
            else if (novoStatus == StatusAssinatura.Ativa)
            {
                assinatura.SuspensaEm = null;
                assinatura.InadimplenteDesde = null;
            }

            await _context.SaveChangesAsync();
            var plano = await _context.Planos.FirstOrDefaultAsync(p => p.Id == assinatura.PlanoId);
            return Map(assinatura, plano?.Nome ?? string.Empty);
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<IEnumerable<FaturaDto>> ListarFaturasAsync(int tenantId)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            return await _context.Faturas
                .Where(f => f.TenantId == tenantId)
                .OrderByDescending(f => f.Vencimento)
                .Select(f => new FaturaDto
                {
                    Id = f.Id,
                    Valor = f.Valor,
                    Status = f.Status.ToString(),
                    Vencimento = f.Vencimento,
                    PagaEm = f.PagaEm,
                    LinkPagamento = f.LinkPagamento,
                    PixCopiaECola = f.PixCopiaECola
                })
                .ToListAsync();
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<AssinaturaDto?> CancelarAsync(int tenantId)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var assinatura = await _context.Assinaturas
                .Where(a => a.TenantId == tenantId && a.Status != StatusAssinatura.Cancelada)
                .OrderByDescending(a => a.DataCriacao)
                .FirstOrDefaultAsync();
            if (assinatura == null)
            {
                return null;
            }

            if (_asaas.Configurado && !string.IsNullOrWhiteSpace(assinatura.GatewaySubscriptionId))
            {
                await _asaas.CancelSubscriptionAsync(assinatura.GatewaySubscriptionId!);
            }

            assinatura.Status = StatusAssinatura.Cancelada;
            assinatura.CanceladaEm = DateTime.UtcNow;
            assinatura.DataAtualizacao = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var plano = await _context.Planos.FirstOrDefaultAsync(p => p.Id == assinatura.PlanoId);
            return Map(assinatura, plano?.Nome ?? string.Empty);
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<bool> ProcessarWebhookAsync(JsonElement payload, string? accessToken)
    {
        // Validação do webhook (quando o token está configurado).
        if (!string.IsNullOrWhiteSpace(_asaasSettings.WebhookToken)
            && !string.Equals(accessToken, _asaasSettings.WebhookToken, StringComparison.Ordinal))
        {
            _logger.LogWarning("Webhook de billing rejeitado: token inválido.");
            return false;
        }

        var evento = ReadString(payload, "event");
        if (string.IsNullOrWhiteSpace(evento))
        {
            return false;
        }

        JsonElement pagamento = default;
        var temPagamento = payload.TryGetProperty("payment", out pagamento);
        var paymentId = temPagamento ? ReadString(pagamento, "id") : null;
        var subscriptionId = temPagamento ? ReadString(pagamento, "subscription") : null;

        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            // Idempotência: se este (pagamento, evento) já foi processado, ignora.
            if (!string.IsNullOrWhiteSpace(paymentId))
            {
                var jaProcessado = await _context.EventosWebhookBilling
                    .AnyAsync(e => e.GatewayPaymentId == paymentId && e.Evento == evento && e.Processado);
                if (jaProcessado)
                {
                    return true;
                }
            }

            var registro = new EventoWebhookBilling
            {
                Evento = evento,
                GatewayPaymentId = paymentId,
                GatewaySubscriptionId = subscriptionId,
                RecebidoEm = DateTime.UtcNow,
                Processado = false,
                PayloadJson = payload.GetRawText()
            };
            _context.EventosWebhookBilling.Add(registro);

            var assinatura = string.IsNullOrWhiteSpace(subscriptionId)
                ? null
                : await _context.Assinaturas.FirstOrDefaultAsync(a => a.GatewaySubscriptionId == subscriptionId);

            if (assinatura == null)
            {
                registro.Processado = true;
                registro.Observacao = "Assinatura não encontrada para o evento.";
                await _context.SaveChangesAsync();
                return true;
            }

            var agora = DateTime.UtcNow;
            switch (evento)
            {
                case "PAYMENT_CONFIRMED":
                case "PAYMENT_RECEIVED":
                    await MarcarFaturaPagaAsync(assinatura, pagamento, paymentId, agora);
                    assinatura.Status = StatusAssinatura.Ativa;
                    assinatura.VigenciaInicio ??= agora;
                    assinatura.InadimplenteDesde = null;
                    assinatura.ProximaCobranca = ProximaData(agora, assinatura.Ciclo);
                    break;

                case "PAYMENT_OVERDUE":
                    var jaInadimplente = assinatura.Status == StatusAssinatura.Inadimplente;
                    assinatura.Status = StatusAssinatura.Inadimplente;
                    assinatura.InadimplenteDesde ??= agora;
                    await AtualizarFaturaStatusAsync(paymentId, StatusFatura.Vencida);
                    if (!jaInadimplente)
                    {
                        await NotificarPagamentoFalhouAsync(assinatura.TenantId);
                    }
                    break;

                case "PAYMENT_REFUNDED":
                case "PAYMENT_CHARGEBACK_REQUESTED":
                    assinatura.Status = StatusAssinatura.Inadimplente;
                    assinatura.InadimplenteDesde ??= agora;
                    break;
            }

            assinatura.DataAtualizacao = agora;
            registro.Processado = true;
            registro.TenantId = assinatura.TenantId;
            await _context.SaveChangesAsync();
            return true;
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<bool> TenantBloqueadoAsync(int tenantId)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            var assinatura = await _context.Assinaturas
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.DataCriacao)
                .Select(a => new { a.Status, a.ProximaCobranca })
                .FirstOrDefaultAsync();

            if (assinatura == null)
            {
                return false; // fail-open: sem assinatura, não bloqueia
            }

            if (assinatura.Status == StatusAssinatura.Suspensa)
            {
                return true;
            }

            if (assinatura.Status == StatusAssinatura.Cancelada
                && (assinatura.ProximaCobranca == null || assinatura.ProximaCobranca <= DateTime.UtcNow))
            {
                return true; // cancelada e fora do período pago
            }

            return false;
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    private async Task MarcarFaturaPagaAsync(Assinatura assinatura, JsonElement pagamento, string? paymentId, DateTime agora)
    {
        var fatura = string.IsNullOrWhiteSpace(paymentId)
            ? null
            : await _context.Faturas.FirstOrDefaultAsync(f => f.GatewayPaymentId == paymentId);

        if (fatura == null)
        {
            fatura = new Fatura
            {
                TenantId = assinatura.TenantId,
                AssinaturaId = assinatura.Id,
                Valor = ReadDecimal(pagamento, "value") ?? assinatura.Valor,
                Vencimento = ReadDate(pagamento, "dueDate") ?? agora,
                GatewayPaymentId = paymentId,
                DataCriacao = agora
            };
            _context.Faturas.Add(fatura);
        }

        fatura.Status = StatusFatura.Paga;
        fatura.PagaEm = agora;
    }

    private async Task AtualizarFaturaStatusAsync(string? paymentId, StatusFatura status)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return;
        }
        var fatura = await _context.Faturas.FirstOrDefaultAsync(f => f.GatewayPaymentId == paymentId);
        if (fatura != null)
        {
            fatura.Status = status;
        }
    }

    private async Task NotificarPagamentoFalhouAsync(int tenantId)
    {
        try
        {
            var email = await _context.Usuarios
                .Where(u => u.TenantId == tenantId && u.Ativo && u.TipoUsuario == TipoUsuario.Admin)
                .Select(u => u.EmailLogin)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            var nomeOrg = await _context.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.Nome)
                .FirstOrDefaultAsync() ?? "sua organização";

            await _emailService.SendAsync(new EmailMessage
            {
                To = email,
                Subject = "Pagamento pendente na sua assinatura — Verbo+",
                HtmlBody = EmailTemplates.PagamentoPendente(nomeOrg)
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao notificar pagamento pendente do tenant {TenantId}.", tenantId);
        }
    }

    private static DateTime ProximaData(DateTime from, CicloCobranca ciclo)
        => ciclo == CicloCobranca.Anual ? from.AddYears(1) : from.AddMonths(1);

    private static AssinaturaDto Map(Assinatura a, string planoNome)
    {
        var agora = DateTime.UtcNow;
        var emTrial = a.Status == StatusAssinatura.Trial;
        int? diasTrial = a.TrialFim.HasValue ? (int)Math.Ceiling((a.TrialFim.Value - agora).TotalDays) : null;
        return new AssinaturaDto
        {
            Id = a.Id,
            TenantId = a.TenantId,
            PlanoId = a.PlanoId,
            PlanoNome = planoNome,
            Status = a.Status.ToString(),
            Ciclo = a.Ciclo.ToString(),
            Valor = a.Valor,
            MetodoPagamento = a.MetodoPagamento?.ToString(),
            TrialFim = a.TrialFim,
            VigenciaInicio = a.VigenciaInicio,
            ProximaCobranca = a.ProximaCobranca,
            CanceladaEm = a.CanceladaEm,
            GatewaySubscriptionId = a.GatewaySubscriptionId,
            DataCriacao = a.DataCriacao,
            EmTrial = emTrial,
            DiasTrialRestantes = emTrial ? diasTrial : null
        };
    }

    private static string? ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal? ReadDecimal(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;

    private static DateTime? ReadDate(JsonElement element, string property)
    {
        var raw = ReadString(element, property);
        return DateTime.TryParse(raw, out var d) ? d : null;
    }
}
