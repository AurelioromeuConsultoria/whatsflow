using Microsoft.EntityFrameworkCore;
using WhatsFlow.Application.DTOs.Pessoas;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Application.Services;
using WhatsFlow.Domain.Entities;
using WhatsFlow.Infrastructure.Data;

namespace WhatsFlow.Infrastructure.Repositories;

public class PessoaRepository : IPessoaRepository
{
    private readonly WhatsFlowDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PessoaRepository(WhatsFlowDbContext context)
        : this(context, new DefaultTenantContext())
    {
    }

    public PessoaRepository(WhatsFlowDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Pessoa>> GetAllAsync()
    {
        var tenantId = await ResolveTenantIdAsync();
        return await _context.Set<Pessoa>()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<Pessoa> Items, int Total)> GetPagedAsync(PessoaPagedQuery query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 200);
        var tenantId = await ResolveTenantIdAsync();

        var q = _context.Set<Pessoa>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Nome))
        {
            var nome = query.Nome.Trim().ToLower();
            q = q.Where(p => p.Nome.ToLower().Contains(nome));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var email = query.Email.Trim().ToLower();
            q = q.Where(p => p.Email != null && p.Email.ToLower().Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(query.Telefone))
        {
            var telefone = query.Telefone.Trim();
            q = q.Where(p => p.Telefone != null && p.Telefone.Contains(telefone));
        }

        if (!string.IsNullOrWhiteSpace(query.WhatsApp))
        {
            var whats = query.WhatsApp.Trim();
            q = q.Where(p => p.WhatsApp != null && p.WhatsApp.Contains(whats));
        }

        if (query.TipoPessoa.HasValue)
        {
            var tipo = query.TipoPessoa.Value;
            q = q.Where(p => p.TipoPessoa == tipo);
        }

        if (query.Ativo.HasValue)
        {
            var ativo = query.Ativo.Value;
            q = q.Where(p => p.Ativo == ativo);
        }

        if (query.Perfil.HasValue)
        {
            var perfil = query.Perfil.Value;
            q = q.Where(p => p.Perfis.Any(pp => pp.DataFim == null && pp.Perfil == perfil));
        }

        // Ordenação
        var sort = (query.Sort ?? "nome").Trim().ToLowerInvariant();
        var desc = string.Equals(query.Direction, "desc", StringComparison.OrdinalIgnoreCase);

        q = sort switch
        {
            "email" => desc ? q.OrderByDescending(p => p.Email) : q.OrderBy(p => p.Email),
            "telefone" => desc ? q.OrderByDescending(p => p.Telefone) : q.OrderBy(p => p.Telefone),
            "whatsapp" => desc ? q.OrderByDescending(p => p.WhatsApp) : q.OrderBy(p => p.WhatsApp),
            "tipopessoa" => desc ? q.OrderByDescending(p => p.TipoPessoa) : q.OrderBy(p => p.TipoPessoa),
            "ativo" => desc ? q.OrderByDescending(p => p.Ativo) : q.OrderBy(p => p.Ativo),
            "datacriacao" => desc ? q.OrderByDescending(p => p.DataCriacao) : q.OrderBy(p => p.DataCriacao),
            _ => desc ? q.OrderByDescending(p => p.Nome) : q.OrderBy(p => p.Nome),
        };

        var total = await q.CountAsync();

        var items = await q
            .Include(p => p.Perfis)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Pessoa?> GetByIdAsync(int id)
    {
        var tenantId = await ResolveTenantIdAsync();
        return await _context.Set<Pessoa>()
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
    }

    public async Task<Pessoa?> GetByEmailAsync(string email)
    {
        var tenantId = await ResolveTenantIdAsync();
        return await _context.Set<Pessoa>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Email != null && p.Email.ToLower() == email.ToLower());
    }

    public async Task<Pessoa?> GetByWhatsAppAsync(string whatsApp)
    {
        var tenantId = await ResolveTenantIdAsync();
        var whatsAppNormalizado = NormalizarTelefone(whatsApp);
        if (string.IsNullOrWhiteSpace(whatsAppNormalizado))
            return null;

        var pessoas = await _context.Set<Pessoa>()
            .Where(p => p.TenantId == tenantId && p.WhatsApp != null)
            .ToListAsync();

        return pessoas.FirstOrDefault(p => NormalizarTelefone(p.WhatsApp!) == whatsAppNormalizado);
    }

    public async Task<Pessoa?> GetByTelefoneAsync(string telefone)
    {
        var tenantId = await ResolveTenantIdAsync();
        var telefoneNormalizado = NormalizarTelefone(telefone);
        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
            return null;

        var pessoas = await _context.Set<Pessoa>()
            .Where(p => p.TenantId == tenantId && p.Telefone != null)
            .ToListAsync();

        return pessoas.FirstOrDefault(p => NormalizarTelefone(p.Telefone!) == telefoneNormalizado);
    }

    private static string NormalizarTelefone(string telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return string.Empty;
        
        // Remove tudo exceto dígitos
        return new string(telefone.Where(char.IsDigit).ToArray());
    }

    public async Task<Pessoa> CreateAsync(Pessoa pessoa)
    {
        pessoa.TenantId = await ResolveTenantIdAsync();
        _context.Set<Pessoa>().Add(pessoa);
        await _context.SaveChangesAsync();
        return pessoa;
    }

    public async Task<Pessoa> CreateWithoutSaveAsync(Pessoa pessoa)
    {
        pessoa.TenantId = await ResolveTenantIdAsync();
        _context.Set<Pessoa>().Add(pessoa);
        return pessoa;
    }

    public async Task<Pessoa> UpdateAsync(Pessoa pessoa)
    {
        _context.Set<Pessoa>().Update(pessoa);
        await _context.SaveChangesAsync();
        return pessoa;
    }

    public Task UpdateWithoutSaveAsync(Pessoa pessoa)
    {
        _context.Set<Pessoa>().Update(pessoa);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var tenantId = await ResolveTenantIdAsync();
        var entity = await _context.Set<Pessoa>().FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (entity != null)
        {
            _context.Set<Pessoa>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> ResolveTenantIdAsync(string? tenantSlug = null)
    {
        if (_tenantContext.TenantId.HasValue)
        {
            return _tenantContext.TenantId.Value;
        }

        var normalizedSlug = string.IsNullOrWhiteSpace(tenantSlug)
            ? Tenant.InitialTenantSlug
            : tenantSlug.Trim().ToLowerInvariant();

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug.ToLower() == normalizedSlug);

        return tenant?.Id ?? Tenant.InitialTenantId;
    }
}

