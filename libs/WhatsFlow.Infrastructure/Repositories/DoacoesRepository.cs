using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class DoacoesRepository : IDoacoesRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public DoacoesRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<FinalidadeDoacao>> GetFinalidadesAsync(bool publicOnly = false)
    {
        var query = _context.FinalidadesDoacao
            .Include(f => f.CategoriaReceita)
            .Include(f => f.ContaBancaria)
            .Include(f => f.CentroCusto)
            .Include(f => f.Projeto)
            .AsQueryable();

        if (publicOnly)
        {
            query = query.Where(f => f.Ativo && f.VisivelPortal);
        }

        return await query
            .OrderBy(f => f.Ordem)
            .ThenBy(f => f.Nome)
            .ToListAsync();
    }

    public async Task<FinalidadeDoacao?> GetFinalidadeByIdAsync(int id)
    {
        return await _context.FinalidadesDoacao
            .Include(f => f.CategoriaReceita)
            .Include(f => f.ContaBancaria)
            .Include(f => f.CentroCusto)
            .Include(f => f.Projeto)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<FinalidadeDoacao?> GetFinalidadeBySlugAsync(string slug)
    {
        return await _context.FinalidadesDoacao
            .FirstOrDefaultAsync(f => f.Slug == slug);
    }

    public async Task<FinalidadeDoacao> CreateFinalidadeAsync(FinalidadeDoacao finalidade)
    {
        finalidade.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.FinalidadesDoacao.Add(finalidade);
        await _context.SaveChangesAsync();
        return finalidade;
    }

    public async Task<FinalidadeDoacao> UpdateFinalidadeAsync(FinalidadeDoacao finalidade)
    {
        _context.FinalidadesDoacao.Update(finalidade);
        await _context.SaveChangesAsync();
        return finalidade;
    }

    public async Task DeleteFinalidadeAsync(int id)
    {
        var entity = await _context.FinalidadesDoacao.FindAsync(id);
        if (entity is not null)
        {
            _context.FinalidadesDoacao.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<DoacaoOnline>> GetDoacoesAsync()
    {
        return await _context.DoacoesOnline
            .Include(d => d.FinalidadeDoacao)
            .OrderByDescending(d => d.DataCriacao)
            .ToListAsync();
    }

    public async Task<DoacaoOnline?> GetDoacaoByIdAsync(int id)
    {
        return await _context.DoacoesOnline
            .Include(d => d.FinalidadeDoacao)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DoacaoOnline?> GetDoacaoByExternalPaymentIdAsync(string externalPaymentId)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            return await _context.DoacoesOnline
                .Include(d => d.FinalidadeDoacao)
                .FirstOrDefaultAsync(d => d.ExternalPaymentId == externalPaymentId);
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<DoacaoOnline?> GetDoacaoByReciboTokenAsync(string reciboToken)
    {
        return await _context.DoacoesOnline
            .Include(d => d.FinalidadeDoacao)
            .FirstOrDefaultAsync(d => d.ReciboToken == reciboToken);
    }

    public async Task<DoacaoOnline> CreateDoacaoAsync(DoacaoOnline doacao)
    {
        doacao.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
        _context.DoacoesOnline.Add(doacao);
        await _context.SaveChangesAsync();
        return doacao;
    }

    public async Task<DoacaoOnline> UpdateDoacaoAsync(DoacaoOnline doacao)
    {
        _context.DoacoesOnline.Update(doacao);
        await _context.SaveChangesAsync();
        return doacao;
    }

    public async Task<GivingProviderConfig?> GetProviderConfigAsync(GivingProvider provider)
    {
        return await _context.GivingProviderConfigs
            .FirstOrDefaultAsync(c => c.Provider == provider);
    }

    public async Task<GivingProviderConfig?> GetProviderConfigByTenantAsync(int tenantId, GivingProvider provider)
    {
        var previous = _context.IgnoreTenantFilters;
        _context.IgnoreTenantFilters = true;
        try
        {
            return await _context.GivingProviderConfigs
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Provider == provider);
        }
        finally
        {
            _context.IgnoreTenantFilters = previous;
        }
    }

    public async Task<GivingProviderConfig> SaveProviderConfigAsync(GivingProviderConfig config)
    {
        if (config.Id == 0)
        {
            config.TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId;
            _context.GivingProviderConfigs.Add(config);
        }
        else
        {
            _context.GivingProviderConfigs.Update(config);
        }

        await _context.SaveChangesAsync();
        return config;
    }

    public async Task<DoacaoOnline> EnsureReceitaForDoacaoAsync(DoacaoOnline doacao)
    {
        if (doacao.ReceitaId.HasValue)
        {
            return doacao;
        }

        var finalidade = doacao.FinalidadeDoacao;
        var receita = new Receita
        {
            TenantId = doacao.TenantId,
            Descricao = finalidade?.Nome is { Length: > 0 } nome ? $"Doação online - {nome}" : "Doação online",
            Valor = doacao.Valor,
            DataRecebimento = DateTime.Now,
            DataConfirmacao = doacao.DataConfirmacao ?? DateTime.Now,
            Status = StatusReceita.Recebida,
            Observacoes = $"Gerada automaticamente pela doação online #{doacao.Id}. Provider: {doacao.Provider}. Pagamento externo: {doacao.ExternalPaymentId}",
            CategoriaReceitaId = finalidade?.CategoriaReceitaId,
            ContaBancariaId = finalidade?.ContaBancariaId,
            CentroCustoId = finalidade?.CentroCustoId,
            ProjetoId = finalidade?.ProjetoId,
            DataCriacao = DateTime.Now,
        };

        _context.Receitas.Add(receita);
        await _context.SaveChangesAsync();

        doacao.ReceitaId = receita.Id;
        _context.DoacoesOnline.Update(doacao);
        await _context.SaveChangesAsync();
        return doacao;
    }
}
