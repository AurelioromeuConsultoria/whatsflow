using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsFlow.Application.Configuration;
using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Services;

public class BillingCycleService : IBillingCycleService
{
    private readonly WhatsFlowDbContext _context;
    private readonly BillingSettings _billing;
    private readonly IEmailService _emailService;
    private readonly ILogger<BillingCycleService> _logger;

    public BillingCycleService(
        WhatsFlowDbContext context,
        IOptions<BillingSettings> billing,
        IEmailService emailService,
        ILogger<BillingCycleService> logger)
    {
        _context = context;
        _billing = billing.Value;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CicloBillingResultado> ExecutarTransicoesAutomaticasAsync(CancellationToken cancellationToken = default)
    {
        var resultado = new CicloBillingResultado();
        var agora = DateTime.UtcNow;
        var limiteCarencia = agora.AddDays(-_billing.CarenciaDias);

        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            // 0) Aviso de "trial acabando" (uma vez por assinatura).
            var limiteAviso = agora.AddDays(_billing.TrialAvisoDiasAntes);
            var trialsAcabando = await _context.Assinaturas
                .Where(a => a.Status == StatusAssinatura.Trial
                    && a.TrialAvisoEnviadoEm == null
                    && a.TrialFim != null && a.TrialFim > agora && a.TrialFim <= limiteAviso)
                .ToListAsync(cancellationToken);
            foreach (var assinatura in trialsAcabando)
            {
                assinatura.TrialAvisoEnviadoEm = agora;
                resultado.AvisosTrialEnviados++;
                await NotificarTrialAcabandoAsync(assinatura.TenantId, assinatura.TrialFim!.Value, cancellationToken);
            }

            // 1) Trial expirado → Inadimplente (inicia a carência).
            var trialsVencidos = await _context.Assinaturas
                .Where(a => a.Status == StatusAssinatura.Trial && a.TrialFim != null && a.TrialFim <= agora)
                .ToListAsync(cancellationToken);
            foreach (var assinatura in trialsVencidos)
            {
                assinatura.Status = StatusAssinatura.Inadimplente;
                assinatura.InadimplenteDesde = assinatura.TrialFim ?? agora;
                assinatura.DataAtualizacao = agora;
                resultado.TrialsExpirados++;
            }

            // 2) Inadimplente além da carência → Suspensa.
            var inadimplentes = await _context.Assinaturas
                .Where(a => a.Status == StatusAssinatura.Inadimplente
                    && a.InadimplenteDesde != null && a.InadimplenteDesde <= limiteCarencia)
                .ToListAsync(cancellationToken);
            foreach (var assinatura in inadimplentes)
            {
                assinatura.Status = StatusAssinatura.Suspensa;
                assinatura.SuspensaEm = agora;
                assinatura.DataAtualizacao = agora;
                resultado.Suspensos++;
                await NotificarSuspensaoAsync(assinatura.TenantId, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }

        if (resultado.TrialsExpirados > 0 || resultado.Suspensos > 0)
        {
            _logger.LogInformation("Ciclo de billing: {Trials} trials expirados, {Suspensos} suspensos.",
                resultado.TrialsExpirados, resultado.Suspensos);
        }

        return resultado;
    }

    private async Task NotificarTrialAcabandoAsync(int tenantId, DateTime trialFim, CancellationToken cancellationToken)
    {
        await EnviarParaAdminAsync(tenantId,
            "Seu período de teste está acabando — Verbo+",
            nome => EmailTemplates.TrialAcabando(nome, trialFim),
            cancellationToken);
    }

    private async Task NotificarSuspensaoAsync(int tenantId, CancellationToken cancellationToken)
    {
        await EnviarParaAdminAsync(tenantId,
            "Sua assinatura foi suspensa — Verbo+",
            EmailTemplates.AssinaturaSuspensa,
            cancellationToken);
    }

    private async Task EnviarParaAdminAsync(int tenantId, string assunto, Func<string, string> htmlBuilder, CancellationToken cancellationToken)
    {
        try
        {
            var dados = await _context.Usuarios
                .Where(u => u.TenantId == tenantId && u.Ativo && u.TipoUsuario == TipoUsuario.Admin)
                .Select(u => new { u.EmailLogin, TenantNome = u.Tenant.Nome })
                .FirstOrDefaultAsync(cancellationToken);

            if (dados == null || string.IsNullOrWhiteSpace(dados.EmailLogin))
                return;

            await _emailService.SendAsync(new EmailMessage
            {
                To = dados.EmailLogin,
                Subject = assunto,
                HtmlBody = htmlBuilder(dados.TenantNome ?? "sua organização")
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            // Notificação é best-effort: nunca derruba o ciclo.
            _logger.LogWarning(ex, "Falha ao notificar tenant {TenantId} ('{Assunto}').", tenantId, assunto);
        }
    }
}
