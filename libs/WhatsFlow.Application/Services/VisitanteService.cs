using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.Visitantes;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface IVisitanteService
{
    Task<IEnumerable<VisitanteDto>> GetAllAsync();
    Task<PagedResultDto<VisitanteDto>> GetPagedAsync(VisitantePagedQueryDto query);
    Task<VisitanteDto?> GetByIdAsync(int id);
    Task<VisitanteResponse> CreateVisitanteAsync(CreateVisitanteRequest request);
    Task<VisitanteDto> CreateAsync(CriarVisitanteDto dto); // Método legado mantido
    Task<VisitanteDto> UpdateAsync(int id, AtualizarVisitanteDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<VisitanteDto>> GetVisitantesPorPessoaAsync(int pessoaId);
}

public class VisitanteService : IVisitanteService
{
    private readonly IVisitanteRepository _visitanteRepository;
    private readonly IComunicacaoAutomacaoService _comunicacaoAutomacaoService;
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<VisitanteService> _logger;

    public VisitanteService(
        IVisitanteRepository visitanteRepository,
        IComunicacaoAutomacaoService comunicacaoAutomacaoService,
        IPessoaRepository pessoaRepository,
        IUnitOfWork unitOfWork,
        ILogger<VisitanteService> logger)
        : this(visitanteRepository, comunicacaoAutomacaoService, pessoaRepository, unitOfWork, new DefaultTenantContext(), logger)
    {
    }

    public VisitanteService(
        IVisitanteRepository visitanteRepository,
        IComunicacaoAutomacaoService comunicacaoAutomacaoService,
        IPessoaRepository pessoaRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<VisitanteService> logger)
    {
        _visitanteRepository = visitanteRepository;
        _comunicacaoAutomacaoService = comunicacaoAutomacaoService;
        _pessoaRepository = pessoaRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<VisitanteDto>> GetAllAsync()
    {
        var visitantes = await _visitanteRepository.GetAllAsync();
        return visitantes.Select(MapToDto);
    }

    public async Task<PagedResultDto<VisitanteDto>> GetPagedAsync(VisitantePagedQueryDto queryDto)
    {
        var page = queryDto.Page <= 0 ? 1 : queryDto.Page;
        var pageSize = queryDto.PageSize <= 0 ? 20 : Math.Min(queryDto.PageSize, 200);

        var query = new VisitantePagedQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = queryDto.Sort,
            Direction = queryDto.Direction,
            Nome = queryDto.Nome,
            Email = queryDto.Email,
            Telefone = queryDto.Telefone,
            WhatsApp = queryDto.WhatsApp,
            DataVisitaFrom = queryDto.DataVisitaFrom,
            DataVisitaTo = queryDto.DataVisitaTo
        };

        var (items, total) = await _visitanteRepository.GetPagedAsync(query);
        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<VisitanteDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<VisitanteDto?> GetByIdAsync(int id)
    {
        var visitante = await _visitanteRepository.GetByIdAsync(id);
        if (visitante == null) return null;

        return MapToDto(visitante);
    }

    public async Task<IEnumerable<VisitanteDto>> GetVisitantesPorPessoaAsync(int pessoaId)
    {
        var visitantes = await _visitanteRepository.GetVisitantesPorPessoaAsync(pessoaId);
        return visitantes.Select(MapToDto);
    }

    /// <summary>
    /// Cria um visitante seguindo o fluxo completo de deduplicação de Pessoa
    /// </summary>
    public async Task<VisitanteResponse> CreateVisitanteAsync(CreateVisitanteRequest request)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new ArgumentException("Nome é obrigatório");

        if (!string.IsNullOrEmpty(request.Email) && !IsValidEmail(request.Email))
            throw new ArgumentException("Email inválido");

        var visitanteId = 0;
        var pessoaCriada = false;
        var pessoaAtualizada = false;
        var perfilVisitanteCriado = false;
        var pessoaId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1. Deduplicação de Pessoa
            Pessoa? pessoa = await BuscarPessoaExistenteAsync(request);

            // 2. Criar ou atualizar Pessoa
            if (pessoa == null)
            {
                pessoa = new Pessoa
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    Nome = request.Nome,
                    Email = NormalizarEmail(request.Email),
                    Telefone = NormalizarTelefone(request.Telefone),
                    WhatsApp = NormalizarTelefone(request.WhatsApp),
                    DataNascimento = NormalizarDataOpcional(request.DataNascimento),
                    TipoPessoa = TipoPessoa.Adulto,
                    Ativo = true,
                    DataCriacao = AgoraSemFuso()
                };
                pessoa = await _pessoaRepository.CreateWithoutSaveAsync(pessoa);
                await _unitOfWork.SaveChangesAsync();
                pessoaCriada = true;
            }
            else
            {
                // Atualizar apenas campos vazios (sem sobrescrever dados existentes)
                bool atualizado = false;
                if (string.IsNullOrWhiteSpace(pessoa.Nome) && !string.IsNullOrWhiteSpace(request.Nome))
                {
                    pessoa.Nome = request.Nome;
                    atualizado = true;
                }
                if (string.IsNullOrWhiteSpace(pessoa.Email) && !string.IsNullOrWhiteSpace(request.Email))
                {
                    pessoa.Email = NormalizarEmail(request.Email);
                    atualizado = true;
                }
                if (string.IsNullOrWhiteSpace(pessoa.Telefone) && !string.IsNullOrWhiteSpace(request.Telefone))
                {
                    pessoa.Telefone = NormalizarTelefone(request.Telefone);
                    atualizado = true;
                }
                if (string.IsNullOrWhiteSpace(pessoa.WhatsApp) && !string.IsNullOrWhiteSpace(request.WhatsApp))
                {
                    pessoa.WhatsApp = NormalizarTelefone(request.WhatsApp);
                    atualizado = true;
                }
                if (pessoa.DataNascimento == null && request.DataNascimento.HasValue)
                {
                    pessoa.DataNascimento = NormalizarDataOpcional(request.DataNascimento);
                    atualizado = true;
                }

                if (atualizado)
                {
                    await _pessoaRepository.UpdateWithoutSaveAsync(pessoa);
                    pessoaAtualizada = true;
                }
            }

            pessoaId = pessoa.Id;

            // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)

            // 4. Criar registro de Visitante (histórico de visita)
            var visitante = new Visitante
            {
                TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                PessoaId = pessoa.Id,
                DataVisita = NormalizarDataObrigatoria(request.DataVisita) ?? AgoraSemFuso(),
                Observacoes = request.Observacoes,
                DataCadastro = AgoraSemFuso()
            };

            var visitanteCriado = await _visitanteRepository.CreateWithoutSaveAsync(visitante);

            // Salvar todas as mudanças dentro da transação
            await _unitOfWork.SaveChangesAsync();
            visitanteId = visitanteCriado.Id;
        });

        if (visitanteId <= 0)
        {
            throw new InvalidOperationException("Falha ao criar visitante (ID não gerado)");
        }

        // 5. Gerar comunicação automatica centralizada (fora da transação)
        try
        {
            await _comunicacaoAutomacaoService.ExecutarNovoVisitanteAsync(visitanteId);
            _logger.LogInformation(
                "Visitante criado e automacao central de comunicacao executada. VisitanteId={VisitanteId} PessoaId={PessoaId} PessoaCriada={PessoaCriada} PessoaAtualizada={PessoaAtualizada} PerfilVisitanteCriado={PerfilVisitanteCriado}",
                visitanteId,
                pessoaId,
                pessoaCriada,
                pessoaAtualizada,
                perfilVisitanteCriado);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Visitante criado, mas houve falha ao executar automacao de comunicacao. VisitanteId={VisitanteId} PessoaId={PessoaId}",
                visitanteId,
                pessoaId);
        }

        // 6. Retornar resposta consolidada
        var visitanteParaResposta = await _visitanteRepository.GetByIdAsync(visitanteId)
            ?? throw new InvalidOperationException("Visitante não encontrado após criação");
        return await MapToResponseAsync(visitanteParaResposta);
    }

    /// <summary>
    /// Busca Pessoa existente seguindo ordem: Email -> WhatsApp -> Telefone
    /// </summary>
    private async Task<Pessoa?> BuscarPessoaExistenteAsync(CreateVisitanteRequest request)
    {
        // 1. Buscar por Email (se fornecido)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var pessoa = await _pessoaRepository.GetByEmailAsync(request.Email);
            if (pessoa != null) return pessoa;
        }

        // 2. Buscar por WhatsApp normalizado (se fornecido)
        if (!string.IsNullOrWhiteSpace(request.WhatsApp))
        {
            var whatsAppNormalizado = NormalizarTelefone(request.WhatsApp);
            if (!string.IsNullOrWhiteSpace(whatsAppNormalizado))
            {
                var pessoa = await _pessoaRepository.GetByWhatsAppAsync(whatsAppNormalizado);
                if (pessoa != null) return pessoa;
            }
        }

        // 3. Buscar por Telefone normalizado (se fornecido)
        if (!string.IsNullOrWhiteSpace(request.Telefone))
        {
            var telefoneNormalizado = NormalizarTelefone(request.Telefone);
            if (!string.IsNullOrWhiteSpace(telefoneNormalizado))
            {
                var pessoa = await _pessoaRepository.GetByTelefoneAsync(telefoneNormalizado);
                if (pessoa != null) return pessoa;
            }
        }

        return null;
    }

    private static string NormalizarTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return string.Empty;

        // Remove tudo exceto dígitos
        return new string(telefone.Where(char.IsDigit).ToArray());
    }

    private static string? NormalizarEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return email.Trim().ToLowerInvariant();
    }

    private static DateTime AgoraSemFuso()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }

    private static DateTime? NormalizarDataOpcional(DateTime? data)
    {
        if (!data.HasValue)
            return null;

        return DateTime.SpecifyKind(data.Value.Date, DateTimeKind.Unspecified);
    }

    private static DateTime? NormalizarDataObrigatoria(DateTime? data)
    {
        if (!data.HasValue)
            return null;

        return DateTime.SpecifyKind(data.Value, DateTimeKind.Unspecified);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<VisitanteResponse> MapToResponseAsync(Visitante visitante)
    {
        // Recarregar com relacionamentos
        var visitanteCompleto = await _visitanteRepository.GetByIdAsync(visitante.Id);
        if (visitanteCompleto == null)
            throw new InvalidOperationException("Visitante não encontrado após criação");

        // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
        return new VisitanteResponse
        {
            VisitanteId = visitanteCompleto.Id,
            PessoaId = visitanteCompleto.PessoaId,
            Nome = visitanteCompleto.Pessoa?.Nome ?? string.Empty,
            Email = visitanteCompleto.Pessoa?.Email,
            Telefone = visitanteCompleto.Pessoa?.Telefone,
            WhatsApp = visitanteCompleto.Pessoa?.WhatsApp,
            DataVisita = visitanteCompleto.DataVisita,
            Observacoes = visitanteCompleto.Observacoes
        };
    }

    // Método legado mantido para compatibilidade
    public async Task<VisitanteDto> CreateAsync(CriarVisitanteDto dto)
    {
        var request = new CreateVisitanteRequest
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            WhatsApp = dto.WhatsApp,
            DataNascimento = dto.DataNascimento,
            DataVisita = dto.DataVisita,
            Observacoes = dto.Observacoes
        };

        var response = await CreateVisitanteAsync(request);
        return new VisitanteDto
        {
            Id = response.VisitanteId,
            PessoaId = response.PessoaId,
            Nome = response.Nome,
            Email = response.Email,
            Telefone = response.Telefone,
            WhatsApp = response.WhatsApp,
            DataVisita = response.DataVisita,
            Observacoes = response.Observacoes,
            DataCadastro = DateTime.UtcNow
        };
    }

    public async Task<VisitanteDto> UpdateAsync(int id, AtualizarVisitanteDto dto)
    {
        var visitante = await _visitanteRepository.GetByIdAsync(id);
        if (visitante == null)
            throw new ArgumentException("Visitante não encontrado");

        // Atualizar apenas dados específicos do visitante (não altera Pessoa)
        visitante.DataVisita = dto.DataVisita;
        visitante.Observacoes = dto.Observacoes;

        var visitanteAtualizado = await _visitanteRepository.UpdateAsync(visitante);
        _logger.LogInformation(
            "Visitante atualizado. VisitanteId={VisitanteId} PessoaId={PessoaId} DataVisita={DataVisita}",
            visitanteAtualizado.Id,
            visitanteAtualizado.PessoaId,
            visitanteAtualizado.DataVisita);
        return MapToDto(visitanteAtualizado);
    }

    public async Task DeleteAsync(int id)
    {
        await _visitanteRepository.DeleteAsync(id);
        _logger.LogInformation("Visitante removido. VisitanteId={VisitanteId}", id);
    }

    private static VisitanteDto MapToDto(Visitante visitante)
    {
        return new VisitanteDto
        {
            Id = visitante.Id,
            PessoaId = visitante.PessoaId,
            Nome = visitante.Pessoa?.Nome ?? string.Empty,
            Telefone = visitante.Pessoa?.Telefone,
            WhatsApp = visitante.Pessoa?.WhatsApp,
            Email = visitante.Pessoa?.Email,
            DataVisita = visitante.DataVisita,
            Observacoes = visitante.Observacoes,
            DataCadastro = visitante.DataCadastro
            // TODO(WhatsFlow Etapa 4): rever público-alvo (Tag/Segmento + Contato)
        };
    }
}
