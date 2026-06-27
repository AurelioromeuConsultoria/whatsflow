using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class ConfiguracaoCampanhaAniversarioRepository : IConfiguracaoCampanhaAniversarioRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ConfiguracaoCampanhaAniversarioRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public ConfiguracaoCampanhaAniversarioRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<ConfiguracaoCampanhaAniversario> GetAsync()
    {
        var tenantId = await ResolveTenantIdAsync();
        var configuracao = await _context.ConfiguracoesCampanhaAniversario.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (configuracao != null)
        {
            return configuracao;
        }

        configuracao = new ConfiguracaoCampanhaAniversario
        {
            TenantId = tenantId,
            Ativo = true,
            ImagemUrl = "/assets/birthday/niver-whatsapp-aniversario.png",
            HorarioEnvio = new TimeSpan(9, 0, 0),
            MensagemTemplate = CampanhaAniversarioDefaults.MensagemTemplatePadrao,
            DataAtualizacao = DateTime.Now
        };

        _context.ConfiguracoesCampanhaAniversario.Add(configuracao);
        await _context.SaveChangesAsync();
        return configuracao;
    }

    public async Task<ConfiguracaoCampanhaAniversario> UpdateAsync(ConfiguracaoCampanhaAniversario configuracao)
    {
        var tenantId = await ResolveTenantIdAsync();
        var existente = await _context.ConfiguracoesCampanhaAniversario.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (existente == null)
        {
            configuracao.TenantId = tenantId;
            configuracao.DataAtualizacao = DateTime.Now;
            _context.ConfiguracoesCampanhaAniversario.Add(configuracao);
            await _context.SaveChangesAsync();
            return configuracao;
        }

        existente.Ativo = configuracao.Ativo;
        existente.ImagemUrl = configuracao.ImagemUrl;
        existente.MensagemTemplate = configuracao.MensagemTemplate;
        existente.HorarioEnvio = configuracao.HorarioEnvio;
        existente.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();
        return existente;
    }

    private async Task<int> ResolveTenantIdAsync()
    {
        if (_tenantContext.TenantId.HasValue)
        {
            return _tenantContext.TenantId.Value;
        }

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == Tenant.InitialTenantSlug);

        return tenant?.Id ?? Tenant.InitialTenantId;
    }
}
