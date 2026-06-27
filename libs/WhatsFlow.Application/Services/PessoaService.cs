using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Pessoas;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface IPessoaService
{
    Task<IEnumerable<PessoaDto>> GetAllAsync();
    Task<PagedResultDto<PessoaDto>> GetPagedAsync(PessoaPagedQueryDto query);
    Task<PessoaDto?> GetByIdAsync(int id);
    Task<Pessoa360Dto?> Get360Async(int id);
    Task<PessoaDto> CreateAsync(CriarPessoaDto dto);
    Task<PessoaDto> UpdateAsync(int id, AtualizarPessoaDto dto);
    Task<PessoaDto> UpdateMinhaPessoaAsync(int id, AtualizarMinhaPessoaDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<AniversarianteDto>> GetProximosAniversariantesAsync(int dias, int limite);
    Task<IEnumerable<AniversarianteDto>> GetAniversariantesPorMesAsync(int mes, int limite);
}

public class PessoaService : IPessoaService
{
    private readonly IPessoaRepository _repository;
    private readonly IVisitanteService _visitanteService;
    private readonly IUsuarioService _usuarioService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PessoaService> _logger;

    public PessoaService(
        IPessoaRepository repository,
        IVisitanteService visitanteService,
        IUsuarioService usuarioService,
        ILogger<PessoaService> logger)
        : this(repository, visitanteService, usuarioService, new DefaultTenantContext(), logger)
    {
    }

    public PessoaService(
        IPessoaRepository repository,
        IVisitanteService visitanteService,
        IUsuarioService usuarioService,
        ITenantContext tenantContext,
        ILogger<PessoaService> logger)
    {
        _repository = repository;
        _visitanteService = visitanteService;
        _usuarioService = usuarioService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<PessoaDto>> GetAllAsync()
    {
        var pessoas = await _repository.GetAllAsync();
        return pessoas.Select(MapToDto).ToList();
    }

    public async Task<PagedResultDto<PessoaDto>> GetPagedAsync(PessoaPagedQueryDto queryDto)
    {
        var page = queryDto.Page <= 0 ? 1 : queryDto.Page;
        var pageSize = queryDto.PageSize <= 0 ? 20 : Math.Min(queryDto.PageSize, 200);

        TipoPessoa? tipoPessoa = null;
        if (!string.IsNullOrWhiteSpace(queryDto.TipoPessoa) &&
            Enum.TryParse<TipoPessoa>(queryDto.TipoPessoa.Trim(), ignoreCase: true, out var tipoParsed))
        {
            tipoPessoa = tipoParsed;
        }

        var query = new PessoaPagedQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = queryDto.Sort,
            Direction = queryDto.Direction,
            Nome = queryDto.Nome,
            Email = queryDto.Email,
            Telefone = queryDto.Telefone,
            WhatsApp = queryDto.WhatsApp,
            // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
            TipoPessoa = tipoPessoa,
            Ativo = queryDto.Ativo
        };

        var (items, total) = await _repository.GetPagedAsync(query);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<PessoaDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PessoaDto?> GetByIdAsync(int id)
    {
        var pessoa = await _repository.GetByIdAsync(id);
        if (pessoa == null) return null;

        return MapToDto(pessoa);
    }

    public async Task<Pessoa360Dto?> Get360Async(int id)
    {
        var pessoa = await _repository.GetByIdAsync(id);
        if (pessoa == null) return null;

        var pessoaDto = MapToDto(pessoa);

        var visitantes = await _visitanteService.GetVisitantesPorPessoaAsync(id);
        var usuario = await _usuarioService.GetByPessoaIdAsync(id);

        // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
        return new Pessoa360Dto
        {
            Pessoa = pessoaDto,
            Visitantes = visitantes.OrderByDescending(v => v.DataVisita).ToList(),
            Usuario = usuario != null ? new UsuarioResumoDto
            {
                Id = usuario.Id,
                EmailLogin = usuario.EmailLogin,
                TipoUsuarioDescricao = usuario.TipoUsuarioDescricao,
                Ativo = usuario.Ativo,
                PerfilAcessoNome = usuario.PerfilAcessoNome,
                UltimoAcesso = usuario.UltimoAcesso
            } : null
        };
    }

    public async Task<PessoaDto> CreateAsync(CriarPessoaDto dto)
    {
        // Validar email único se fornecido
        if (!string.IsNullOrEmpty(dto.Email))
        {
            var existe = await _repository.GetByEmailAsync(dto.Email);
            if (existe != null) throw new ArgumentException("Email já cadastrado");
        }

        var pessoa = new Pessoa
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            WhatsApp = dto.WhatsApp,
            DataNascimento = dto.DataNascimento,
            TipoPessoa = dto.TipoPessoa,
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(pessoa);
        _logger.LogInformation(
            "Pessoa criada. PessoaId={PessoaId} TipoPessoa={TipoPessoa} Ativo={Ativo}",
            created.Id,
            created.TipoPessoa,
            created.Ativo);
        return MapToDto(created);
    }

    public async Task<PessoaDto> UpdateAsync(int id, AtualizarPessoaDto dto)
    {
        var pessoa = await _repository.GetByIdAsync(id);
        if (pessoa == null) throw new ArgumentException("Pessoa não encontrada");

        // Validar email único se alterado
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != pessoa.Email)
        {
            var existe = await _repository.GetByEmailAsync(dto.Email);
            if (existe != null && existe.Id != id) throw new ArgumentException("Email já cadastrado para outra pessoa");
        }

        pessoa.Nome = dto.Nome;
        pessoa.Email = dto.Email;
        pessoa.Telefone = dto.Telefone;
        pessoa.WhatsApp = dto.WhatsApp;
        pessoa.DataNascimento = dto.DataNascimento;
        pessoa.TipoPessoa = dto.TipoPessoa;
        pessoa.Ativo = dto.Ativo;
        pessoa.TenantId = _tenantContext.TenantId ?? pessoa.TenantId;

        var updated = await _repository.UpdateAsync(pessoa);
        _logger.LogInformation(
            "Pessoa atualizada. PessoaId={PessoaId} TipoPessoa={TipoPessoa} Ativo={Ativo}",
            updated.Id,
            updated.TipoPessoa,
            updated.Ativo);
        return MapToDto(updated);
    }

    public async Task<PessoaDto> UpdateMinhaPessoaAsync(int id, AtualizarMinhaPessoaDto dto)
    {
        var pessoa = await _repository.GetByIdAsync(id);
        if (pessoa == null) throw new ArgumentException("Pessoa não encontrada");

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != pessoa.Email)
        {
            var existe = await _repository.GetByEmailAsync(dto.Email);
            if (existe != null && existe.Id != id) throw new ArgumentException("Email já cadastrado para outra pessoa");
        }

        pessoa.Nome = dto.Nome;
        pessoa.Email = dto.Email;
        pessoa.Telefone = dto.Telefone;
        pessoa.WhatsApp = dto.WhatsApp;
        pessoa.DataNascimento = dto.DataNascimento;
        pessoa.TipoPessoa = ResolveTipoPessoa(dto.DataNascimento, pessoa.TipoPessoa);
        pessoa.TenantId = _tenantContext.TenantId ?? pessoa.TenantId;

        var updated = await _repository.UpdateAsync(pessoa);
        _logger.LogInformation(
            "Pessoa atualizou o proprio cadastro. PessoaId={PessoaId} TipoPessoa={TipoPessoa}",
            updated.Id,
            updated.TipoPessoa);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Pessoa removida. PessoaId={PessoaId}", id);
    }

    public async Task<IEnumerable<AniversarianteDto>> GetProximosAniversariantesAsync(int dias, int limite)
    {
        if (dias <= 0) dias = 30;
        if (limite <= 0) limite = 50;

        var hoje = DateTime.Today;
        var pessoas = await _repository.GetAllAsync();

        return pessoas
            .Where(p => p.Ativo && p.DataNascimento.HasValue)
            .Select(p =>
            {
                var nasc = p.DataNascimento!.Value.Date;
                var prox = GetProximoAniversario(nasc, hoje);
                var diasRestantes = (prox - hoje).Days;
                return new AniversarianteDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    DataNascimento = nasc,
                    ProximoAniversario = prox,
                    DiasParaAniversario = diasRestantes
                };
            })
            .Where(a => a.DiasParaAniversario <= dias && a.DiasParaAniversario >= 0)
            .OrderBy(a => a.DiasParaAniversario)
            .ThenBy(a => a.Nome)
            .Take(limite)
            .ToList();
    }

    public async Task<IEnumerable<AniversarianteDto>> GetAniversariantesPorMesAsync(int mes, int limite)
    {
        if (mes is < 1 or > 12) throw new ArgumentException("Mês inválido. Use 1 a 12.");
        if (limite <= 0) limite = 500;

        var hoje = DateTime.Today;
        var pessoas = await _repository.GetAllAsync();

        return pessoas
            .Where(p => p.Ativo && p.DataNascimento.HasValue && p.DataNascimento.Value.Month == mes)
            .Select(p =>
            {
                var nasc = p.DataNascimento!.Value.Date;
                var prox = GetProximoAniversario(nasc, hoje);
                var diasRestantes = (prox - hoje).Days;
                return new AniversarianteDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    DataNascimento = nasc,
                    ProximoAniversario = prox,
                    DiasParaAniversario = diasRestantes
                };
            })
            .OrderBy(a => a.DataNascimento.Month)
            .ThenBy(a => a.DataNascimento.Day)
            .ThenBy(a => a.Nome)
            .Take(limite)
            .ToList();
    }

    // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
    private static PessoaDto MapToDto(Pessoa pessoa)
    {
        return new PessoaDto
        {
            Id = pessoa.Id,
            Nome = pessoa.Nome,
            Email = pessoa.Email,
            Telefone = pessoa.Telefone,
            WhatsApp = pessoa.WhatsApp,
            DataNascimento = pessoa.DataNascimento,
            TipoPessoa = pessoa.TipoPessoa,
            TipoPessoaDescricao = GetTipoPessoaDescricao(pessoa.TipoPessoa),
            Ativo = pessoa.Ativo,
            DataCriacao = pessoa.DataCriacao
        };
    }

    private static string GetTipoPessoaDescricao(TipoPessoa tipo)
    {
        return tipo switch
        {
            TipoPessoa.Adulto => "Adulto",
            TipoPessoa.Crianca => "Criança",
            _ => "Desconhecido"
        };
    }

    private static DateTime GetProximoAniversario(DateTime dataNascimento, DateTime hoje)
    {
        var ano = hoje.Year;
        var mes = dataNascimento.Month;
        var dia = dataNascimento.Day;

        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        if (dia > diasNoMes) dia = diasNoMes;

        var proximo = new DateTime(ano, mes, dia);
        if (proximo < hoje)
        {
            ano += 1;
            diasNoMes = DateTime.DaysInMonth(ano, mes);
            if (dia > diasNoMes) dia = diasNoMes;
            proximo = new DateTime(ano, mes, dia);
        }

        return proximo;
    }

    private static TipoPessoa ResolveTipoPessoa(DateTime? dataNascimento, TipoPessoa atual)
    {
        if (!dataNascimento.HasValue)
        {
            return atual;
        }

        var hoje = DateTime.Today;
        var idade = hoje.Year - dataNascimento.Value.Year;
        if (dataNascimento.Value.Date > hoje.AddYears(-idade))
        {
            idade--;
        }

        return idade < 18 ? TipoPessoa.Crianca : TipoPessoa.Adulto;
    }
}
