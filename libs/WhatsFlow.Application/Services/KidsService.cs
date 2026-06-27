using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface IKidsService
{
    Task<IEnumerable<CriancaDto>> GetCriancasAsync();
    Task<CriancaDto?> GetCriancaByIdAsync(int criancaPessoaId);
    Task<IEnumerable<MinhaCriancaResumoDto>> GetMinhasCriancasAsync();
    Task<MinhaCriancaDetalheDto?> GetMinhaCriancaByIdAsync(int criancaPessoaId);
    Task<IEnumerable<MeuCheckinResumoDto>> GetMeusCheckinsAsync();
    Task<MeuHistoricoPagedDto> GetMeuHistoricoAsync(int? criancaPessoaId, int page, int pageSize);
    Task<CriancaDto> CreateCriancaAsync(CreateCriancaRequest request, string? ipOrigem = null);
    Task<CriancaDto> UpdateCriancaAsync(int criancaPessoaId, UpdateCriancaRequest request);
    Task DeleteCriancaAsync(int criancaPessoaId);
    
    Task<ResponsavelCriancaDto> VincularResponsavelAsync(int criancaPessoaId, CreateResponsavelRequest request);
    Task<ResponsavelCriancaDto> UpdateResponsavelAsync(int responsavelId, UpdateResponsavelRequest request);
    Task DesvincularResponsavelAsync(int responsavelId);
    
    Task<CheckinResponse> CheckinAsync(CheckinRequest request);
    Task CheckoutAsync(CheckoutRequest request);
    Task<IEnumerable<KidsCheckinDto>> GetHistoricoCheckinsAsync(int? criancaPessoaId = null);
}

public class KidsService : IKidsService
{
    private readonly IPessoaRepository _pessoaRepository;
    private readonly ICriancaDetalheRepository _criancaDetalheRepository;
    private readonly IKidsEstruturaRepository _kidsEstruturaRepository;
    private readonly IResponsavelCriancaRepository _responsavelRepository;
    private readonly IKidsCheckinRepository _checkinRepository;
    private readonly IKidsNotificacaoRepository _notificacaoRepository;
    private readonly IPessoaPerfilRepository _perfilRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IKidsAuthorizationService _authorizationService;
    private readonly IComunicacaoAutomacaoService _comunicacaoAutomacaoService;
    private readonly ITenantContext _tenantContext;
    private readonly IKidsPushNotificationService? _pushService;
    private readonly IConsentimentoRegistroRepository? _consentimentoRepository;
    private readonly ILogger<KidsService> _logger;

    public KidsService(
        IPessoaRepository pessoaRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IKidsEstruturaRepository kidsEstruturaRepository,
        IResponsavelCriancaRepository responsavelRepository,
        IKidsCheckinRepository checkinRepository,
        IKidsNotificacaoRepository notificacaoRepository,
        IPessoaPerfilRepository perfilRepository,
        IUnitOfWork unitOfWork,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService,
        IComunicacaoAutomacaoService comunicacaoAutomacaoService,
        ITenantContext tenantContext,
        ILogger<KidsService> logger,
        IKidsPushNotificationService? pushService = null,
        IConsentimentoRegistroRepository? consentimentoRepository = null)
    {
        _pessoaRepository = pessoaRepository;
        _criancaDetalheRepository = criancaDetalheRepository;
        _kidsEstruturaRepository = kidsEstruturaRepository;
        _responsavelRepository = responsavelRepository;
        _checkinRepository = checkinRepository;
        _notificacaoRepository = notificacaoRepository;
        _perfilRepository = perfilRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _authorizationService = authorizationService;
        _comunicacaoAutomacaoService = comunicacaoAutomacaoService;
        _tenantContext = tenantContext;
        _pushService = pushService;
        _consentimentoRepository = consentimentoRepository;
        _logger = logger;
    }

    public KidsService(
        IPessoaRepository pessoaRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IKidsEstruturaRepository kidsEstruturaRepository,
        IResponsavelCriancaRepository responsavelRepository,
        IKidsCheckinRepository checkinRepository,
        IKidsNotificacaoRepository notificacaoRepository,
        IPessoaPerfilRepository perfilRepository,
        IUnitOfWork unitOfWork,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUserContext,
        IKidsAuthorizationService authorizationService,
        IComunicacaoAutomacaoService comunicacaoAutomacaoService,
        ILogger<KidsService> logger,
        IKidsPushNotificationService? pushService = null)
        : this(
            pessoaRepository,
            criancaDetalheRepository,
            kidsEstruturaRepository,
            responsavelRepository,
            checkinRepository,
            notificacaoRepository,
            perfilRepository,
            unitOfWork,
            usuarioRepository,
            currentUserContext,
            authorizationService,
            comunicacaoAutomacaoService,
            new DefaultTenantContext(),
            logger,
            pushService)
    {
    }

    public async Task<IEnumerable<CriancaDto>> GetCriancasAsync()
    {
        await _authorizationService.EnsureOperadorAsync();
        var pessoas = await _pessoaRepository.GetAllAsync();
        var criancas = pessoas.Where(p => p.TipoPessoa == TipoPessoa.Crianca && p.Ativo).ToList();
        
        var resultado = new List<CriancaDto>();
        
        foreach (var pessoa in criancas)
        {
            var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(pessoa.Id);
            var responsaveis = await _responsavelRepository.GetByCriancaIdAsync(pessoa.Id);
            var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(pessoa.Id);
            
            resultado.Add(MapToCriancaDto(pessoa, detalhe, responsaveis, checkinAtivo));
        }
        
        return resultado;
    }

    public async Task<CriancaDto?> GetCriancaByIdAsync(int criancaPessoaId)
    {
        await _authorizationService.EnsureOperadorAsync();
        var pessoa = await _pessoaRepository.GetByIdAsync(criancaPessoaId);
        if (pessoa == null || pessoa.TipoPessoa != TipoPessoa.Crianca) return null;
        
        var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(criancaPessoaId);
        var responsaveis = await _responsavelRepository.GetByCriancaIdAsync(criancaPessoaId);
        var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(criancaPessoaId);
        var historico = await _checkinRepository.GetHistoricoPorCriancaAsync(criancaPessoaId, 10);
        
        var dto = MapToCriancaDto(pessoa, detalhe, responsaveis, checkinAtivo);
        return dto;
    }

    public async Task<IEnumerable<MinhaCriancaResumoDto>> GetMinhasCriancasAsync()
    {
        var responsavelPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        var criancaIds = (await _responsavelRepository.GetCriancaIdsAtivosByResponsavelIdAsync(responsavelPessoaId)).ToHashSet();
        if (criancaIds.Count == 0)
            return [];

        var pessoas = await _pessoaRepository.GetAllAsync();
        var criancas = pessoas
            .Where(p => criancaIds.Contains(p.Id) && p.TipoPessoa == TipoPessoa.Crianca && p.Ativo)
            .OrderBy(p => p.Nome)
            .ToList();

        var resultado = new List<MinhaCriancaResumoDto>();
        foreach (var crianca in criancas)
        {
            var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(crianca.Id);
            var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(crianca.Id);
            resultado.Add(MapToMinhaCriancaResumoDto(crianca, detalhe, checkinAtivo));
        }

        _logger.LogInformation(
            "Consulta Kids me/criancas para responsavel {ResponsavelPessoaId}. TotalCriancas={TotalCriancas}",
            responsavelPessoaId,
            resultado.Count);

        return resultado;
    }

    public async Task<MinhaCriancaDetalheDto?> GetMinhaCriancaByIdAsync(int criancaPessoaId)
    {
        var responsavelPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        await EnsureCriancaPertenceAoResponsavelAsync(criancaPessoaId, responsavelPessoaId);

        var pessoa = await _pessoaRepository.GetByIdAsync(criancaPessoaId);
        if (pessoa == null || pessoa.TipoPessoa != TipoPessoa.Crianca || !pessoa.Ativo)
            return null;

        var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(criancaPessoaId);
        var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(criancaPessoaId);
        var historico = await _checkinRepository.GetHistoricoPorCriancaAsync(criancaPessoaId, 10);

        _logger.LogInformation(
            "Consulta Kids me/criancas/{CriancaPessoaId} para responsavel {ResponsavelPessoaId}",
            criancaPessoaId,
            responsavelPessoaId);

        return MapToMinhaCriancaDetalheDto(pessoa, detalhe, checkinAtivo, historico);
    }

    public async Task<IEnumerable<MeuCheckinResumoDto>> GetMeusCheckinsAsync()
    {
        var responsavelPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        var criancaIds = (await _responsavelRepository.GetCriancaIdsAtivosByResponsavelIdAsync(responsavelPessoaId)).ToHashSet();
        if (criancaIds.Count == 0)
            return [];

        var resultado = new List<MeuCheckinResumoDto>();
        foreach (var criancaId in criancaIds)
        {
            var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(criancaId);
            var historico = await _checkinRepository.GetHistoricoPorCriancaAsync(criancaId, 10);
            resultado.AddRange(historico.Select(c => MapToMeuCheckinResumoDto(c, detalhe?.SalaId)));
        }

        _logger.LogInformation(
            "Consulta Kids me/checkins para responsavel {ResponsavelPessoaId}. TotalCheckins={TotalCheckins}",
            responsavelPessoaId,
            resultado.Count);

        return resultado.OrderByDescending(c => c.CheckinTime).ToList();
    }

    public async Task<MeuHistoricoPagedDto> GetMeuHistoricoAsync(int? criancaPessoaId, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var responsavelPessoaId = await GetRequiredCurrentUserPessoaIdAsync();
        var todasCriancaIds = (await _responsavelRepository.GetCriancaIdsAtivosByResponsavelIdAsync(responsavelPessoaId)).ToHashSet();

        if (criancaPessoaId.HasValue)
        {
            if (!todasCriancaIds.Contains(criancaPessoaId.Value))
                throw new UnauthorizedAccessException("Criança não vinculada a este responsável.");
            todasCriancaIds = [criancaPessoaId.Value];
        }

        if (todasCriancaIds.Count == 0)
            return new MeuHistoricoPagedDto { Page = page, PageSize = pageSize };

        var (items, total) = await _checkinRepository.GetHistoricoPagedAsync(todasCriancaIds, page, pageSize);

        var salaLookup = new Dictionary<int, string?>();
        foreach (var cid in todasCriancaIds)
        {
            var d = await _criancaDetalheRepository.GetByPessoaIdAsync(cid);
            salaLookup[cid] = d?.SalaId;
        }

        return new MeuHistoricoPagedDto
        {
            Items = items.Select(c => MapToMeuCheckinResumoDto(c, salaLookup.GetValueOrDefault(c.CriancaPessoaId))).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CriancaDto> CreateCriancaAsync(CreateCriancaRequest request, string? ipOrigem = null)
    {
        await _authorizationService.EnsureLiderAsync();
        await ValidateSalaTurmaAsync(request.SalaId, request.TurmaId);

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Criar Pessoa
                var pessoa = new Pessoa
                {
                    Nome = request.Nome,
                    DataNascimento = request.DataNascimento,
                    Email = request.Email,
                    Telefone = request.Telefone,
                    WhatsApp = request.WhatsApp,
                    TipoPessoa = TipoPessoa.Crianca,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                // Adicionar Pessoa ao contexto sem salvar
                var pessoaCriada = await _pessoaRepository.CreateWithoutSaveAsync(pessoa);

                // Salvar apenas a Pessoa para gerar o ID (dentro da transação)
                await _unitOfWork.SaveChangesAsync();

                // Recarregar a pessoa para garantir que o ID está disponível
                pessoaCriada = await _pessoaRepository.GetByIdAsync(pessoaCriada.Id) ?? pessoaCriada;

                // Agora que o ID foi gerado e confirmado, criar CriancaDetalhe
                var detalhe = new CriancaDetalhe
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    PessoaId = pessoaCriada.Id,
                    Alergias = request.Alergias,
                    RestricoesAlimentares = request.RestricoesAlimentares,
                    Observacoes = request.Observacoes,
                    SalaId = request.SalaId,
                    TurmaId = request.TurmaId,
                    DataCadastro = DateTime.UtcNow
                };

                await _criancaDetalheRepository.CreateWithoutSaveAsync(detalhe);

                // Criar perfil Kids
                var perfil = new PessoaPerfil
                {
                    PessoaId = pessoaCriada.Id,
                    Perfil = PerfilPessoa.Kids,
                    DataInicio = DateTime.UtcNow,
                    DataFim = null
                };

                await _perfilRepository.CreateWithoutSaveAsync(perfil);

                // Responsável que concederá o consentimento parental (primeiro da lista).
                int? consentidoPorId = null;

                // Processar responsáveis se fornecidos
                if (request.Responsaveis != null && request.Responsaveis.Any())
                {
                    foreach (var respRequest in request.Responsaveis)
                    {
                        Pessoa? responsavelPessoa;

                        if (respRequest.ResponsavelPessoaId.HasValue)
                        {
                            responsavelPessoa = await _pessoaRepository.GetByIdAsync(respRequest.ResponsavelPessoaId.Value);
                            if (responsavelPessoa == null)
                                throw new ArgumentException($"Responsável com ID {respRequest.ResponsavelPessoaId} não encontrado");
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(respRequest.Nome))
                                throw new ArgumentException("Nome do responsável é obrigatório quando não fornecido ID");

                            var novaPessoa = new Pessoa
                            {
                                Nome = respRequest.Nome,
                                Telefone = respRequest.Telefone,
                                WhatsApp = respRequest.WhatsApp,
                                Email = respRequest.Email,
                                TipoPessoa = TipoPessoa.Adulto,
                                Ativo = true,
                                DataCriacao = DateTime.UtcNow
                            };

                            responsavelPessoa = await _pessoaRepository.CreateWithoutSaveAsync(novaPessoa);
                            await _unitOfWork.SaveChangesAsync();
                        }

                        consentidoPorId ??= responsavelPessoa!.Id;

                        var responsavelCrianca = new ResponsavelCrianca
                        {
                            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                            CriancaPessoaId = pessoaCriada.Id,
                            ResponsavelPessoaId = responsavelPessoa!.Id,
                            PodeRetirar = respRequest.PodeRetirar,
                            Parentesco = respRequest.Parentesco,
                            Ativo = true,
                            DataCadastro = DateTime.UtcNow
                        };

                        await _responsavelRepository.CreateWithoutSaveAsync(responsavelCrianca);
                    }
                }

                // Trilha de consentimento parental LGPD (dado sensível de menor).
                if (_consentimentoRepository != null && !string.IsNullOrWhiteSpace(request.ConsentimentoParentalVersao))
                {
                    await _consentimentoRepository.CreateWithoutSaveAsync(new ConsentimentoRegistro
                    {
                        TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                        PessoaId = pessoaCriada.Id,
                        Tipo = TipoConsentimento.ConsentimentoParental,
                        VersaoDocumento = request.ConsentimentoParentalVersao.Trim(),
                        AceitoEm = DateTime.UtcNow,
                        IpOrigem = ipOrigem,
                        Origem = "kids_cadastro",
                        ConcedidoPorPessoaId = consentidoPorId
                    });
                }

                await _unitOfWork.SaveChangesAsync();

                // Retornar crianca criada
                var responsaveis = await _responsavelRepository.GetByCriancaIdAsync(pessoaCriada.Id);
                KidsCheckin? checkinAtivo = null;
                return MapToCriancaDto(pessoaCriada, detalhe, responsaveis, checkinAtivo);
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao criar criança: {ex.Message}", ex);
        }
    }

    public async Task<CriancaDto> UpdateCriancaAsync(int criancaPessoaId, UpdateCriancaRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        await ValidateSalaTurmaAsync(request.SalaId, request.TurmaId);

        var pessoa = await _pessoaRepository.GetByIdAsync(criancaPessoaId);
        if (pessoa == null || pessoa.TipoPessoa != TipoPessoa.Crianca)
            throw new ArgumentException("Criança não encontrada");
        
        // Atualizar Pessoa
        pessoa.Nome = request.Nome;
        pessoa.DataNascimento = request.DataNascimento;
        pessoa.Email = request.Email;
        pessoa.Telefone = request.Telefone;
        pessoa.WhatsApp = request.WhatsApp;
        await _pessoaRepository.UpdateAsync(pessoa);
        
        // Atualizar ou criar CriancaDetalhe
        var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(criancaPessoaId);
        if (detalhe == null)
        {
            detalhe = new CriancaDetalhe
            {
                TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                PessoaId = criancaPessoaId,
                Alergias = request.Alergias,
                RestricoesAlimentares = request.RestricoesAlimentares,
                Observacoes = request.Observacoes,
                SalaId = request.SalaId,
                TurmaId = request.TurmaId,
                DataCadastro = DateTime.UtcNow
            };
            await _criancaDetalheRepository.CreateAsync(detalhe);
        }
        else
        {
            detalhe.Alergias = request.Alergias;
            detalhe.RestricoesAlimentares = request.RestricoesAlimentares;
            detalhe.Observacoes = request.Observacoes;
            detalhe.SalaId = request.SalaId;
            detalhe.TurmaId = request.TurmaId;
            await _criancaDetalheRepository.UpdateAsync(detalhe);
        }
        
        var responsaveis = await _responsavelRepository.GetByCriancaIdAsync(criancaPessoaId);
        var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(criancaPessoaId);
        
        return MapToCriancaDto(pessoa, detalhe, responsaveis, checkinAtivo);
    }

    public async Task DeleteCriancaAsync(int criancaPessoaId)
    {
        await _authorizationService.EnsureLiderAsync();
        var pessoa = await _pessoaRepository.GetByIdAsync(criancaPessoaId);
        if (pessoa == null || pessoa.TipoPessoa != TipoPessoa.Crianca)
            throw new ArgumentException("Criança não encontrada");
        
        // Soft delete: desativar pessoa
        pessoa.Ativo = false;
        await _pessoaRepository.UpdateAsync(pessoa);
    }

    public async Task<ResponsavelCriancaDto> VincularResponsavelAsync(int criancaPessoaId, CreateResponsavelRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var crianca = await _pessoaRepository.GetByIdAsync(criancaPessoaId);
        if (crianca == null || crianca.TipoPessoa != TipoPessoa.Crianca)
            throw new ArgumentException("Criança não encontrada");
        
        var responsavel = await _pessoaRepository.GetByIdAsync(request.ResponsavelPessoaId);
        if (responsavel == null)
            throw new ArgumentException("Responsável não encontrado");
        
        // Verificar se já existe vínculo
        var vinculoExistente = await _responsavelRepository.GetByCriancaAndResponsavelAsync(criancaPessoaId, request.ResponsavelPessoaId);
        if (vinculoExistente != null)
            throw new InvalidOperationException("Responsável já está vinculado a esta criança");
        
        var responsavelCrianca = new ResponsavelCrianca
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            CriancaPessoaId = criancaPessoaId,
            ResponsavelPessoaId = request.ResponsavelPessoaId,
            PodeRetirar = request.PodeRetirar,
            Parentesco = request.Parentesco,
            Ativo = true,
            DataCadastro = DateTime.UtcNow
        };
        
        var criado = await _responsavelRepository.CreateAsync(responsavelCrianca);
        return MapToResponsavelDto(criado);
    }

    public async Task<ResponsavelCriancaDto> UpdateResponsavelAsync(int responsavelId, UpdateResponsavelRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var responsavel = await _responsavelRepository.GetByIdAsync(responsavelId);
        if (responsavel == null)
            throw new ArgumentException("Vínculo de responsável não encontrado");
        
        if (request.PodeRetirar.HasValue)
            responsavel.PodeRetirar = request.PodeRetirar.Value;
        
        if (request.Parentesco != null)
            responsavel.Parentesco = request.Parentesco;
        
        if (request.Ativo.HasValue)
            responsavel.Ativo = request.Ativo.Value;
        
        var atualizado = await _responsavelRepository.UpdateAsync(responsavel);
        return MapToResponsavelDto(atualizado);
    }

    public async Task DesvincularResponsavelAsync(int responsavelId)
    {
        await _authorizationService.EnsureLiderAsync();
        var responsavel = await _responsavelRepository.GetByIdAsync(responsavelId);
        if (responsavel == null)
            throw new ArgumentException("Vínculo de responsável não encontrado");
        
        await _responsavelRepository.DeleteAsync(responsavelId);
    }

    public async Task<CheckinResponse> CheckinAsync(CheckinRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();

        _logger.LogInformation(
            "Iniciando check-in Kids para crianca {CriancaPessoaId}. Metodo={Metodo}. CheckinByPessoaId={CheckinByPessoaId}. UsuarioAtual={UsuarioAtualId}",
            request.CriancaPessoaId,
            request.Metodo,
            request.CheckinByPessoaId,
            _currentUserContext.UserId);

        try
        {
            var (response, responsavelIds, msg, tipo) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var crianca = await _pessoaRepository.GetByIdAsync(request.CriancaPessoaId);
                if (crianca == null || crianca.TipoPessoa != TipoPessoa.Crianca || !crianca.Ativo)
                    throw new ArgumentException("Criança não encontrada ou inativa");

                var checkinAtivo = await _checkinRepository.GetCheckinAtivoPorCriancaAsync(request.CriancaPessoaId);
                if (checkinAtivo != null)
                    throw new InvalidOperationException("Criança já possui um check-in ativo");

                var codigoSessao = Guid.NewGuid().ToString("N")[..12].ToUpper();
                var tokenRetirada = Guid.NewGuid().ToString("N")[..24].ToUpper();
                var pinRetirada = Random.Shared.Next(100000, 999999).ToString();
                var tokenRetiradaExpiraEm = DateTime.UtcNow.AddHours(8);

                var checkin = new KidsCheckin
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    CriancaPessoaId = request.CriancaPessoaId,
                    CheckinTime = DateTime.UtcNow,
                    CheckinByPessoaId = request.CheckinByPessoaId,
                    Metodo = request.Metodo,
                    CodigoSessao = codigoSessao,
                    TokenRetirada = tokenRetirada,
                    PinRetirada = pinRetirada,
                    TokenRetiradaExpiraEm = tokenRetiradaExpiraEm,
                    Status = "CheckedIn",
                    Observacoes = request.Observacoes
                };

                await _checkinRepository.CreateWithoutSaveAsync(checkin);

                var responsaveis = await _responsavelRepository.GetByCriancaIdAsync(request.CriancaPessoaId);
                var notificacoes = new List<NotificacaoCriadaDto>();

                foreach (var responsavel in responsaveis)
                {
                    var mensagem = $"Check-in realizado para {crianca.Nome} às {DateTime.UtcNow:HH:mm}";

                    var notificacao = new KidsNotificacao
                    {
                        TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                        CriancaPessoaId = request.CriancaPessoaId,
                        ResponsavelPessoaId = responsavel.ResponsavelPessoaId,
                        Titulo = "Check-in realizado",
                        Tipo = "CHECKIN",
                        Origem = "AUTOMATICA",
                        Mensagem = mensagem,
                        Status = "Enviado",
                        EnviadoEm = DateTime.UtcNow,
                        DataCriacao = DateTime.UtcNow,
                        CriadoByPessoaId = request.CheckinByPessoaId
                    };

                    await _notificacaoRepository.CreateWithoutSaveAsync(notificacao);

                    notificacoes.Add(new NotificacaoCriadaDto
                    {
                        ResponsavelPessoaId = responsavel.ResponsavelPessoaId,
                        ResponsavelNome = responsavel.Responsavel?.Nome ?? "N/A",
                        Status = "Pendente"
                    });
                }

                await _unitOfWork.SaveChangesAsync();

                var responsavelIds = notificacoes.Select(n => n.ResponsavelPessoaId).Distinct().ToList();
                var msg = $"Check-in realizado para {crianca.Nome} às {DateTime.UtcNow:HH:mm}";
                return (new CheckinResponse
                {
                    CheckinId = checkin.Id,
                    CodigoSessao = codigoSessao,
                    TokenRetirada = tokenRetirada,
                    PinRetirada = pinRetirada,
                    TokenRetiradaExpiraEm = tokenRetiradaExpiraEm,
                    CheckinTime = checkin.CheckinTime,
                    Notificacoes = notificacoes
                }, responsavelIds, msg, "CHECKIN");
            });

            if (_pushService != null && responsavelIds.Count > 0)
                await _comunicacaoAutomacaoService.ExecutarAvisoContextualKidsAsync(new ComunicacaoAvisoContextualKidsRequest
                {
                    ChaveEvento = $"kids:checkin:{request.CriancaPessoaId}:{response.CheckinId}",
                    CriancaPessoaId = request.CriancaPessoaId,
                    ResponsavelPessoaIds = responsavelIds,
                    Titulo = "App Kids - Check-in",
                    Mensagem = msg,
                    Tipo = tipo
                });

            _logger.LogInformation(
                "Check-in Kids concluido para crianca {CriancaPessoaId}. CheckinId={CheckinId}. CodigoSessao={CodigoSessao}. TotalResponsaveisNotificados={TotalResponsaveis}",
                request.CriancaPessoaId,
                response.CheckinId,
                response.CodigoSessao,
                response.Notificacoes.Count);

            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Check-in Kids bloqueado para crianca {CriancaPessoaId}. Metodo={Metodo}",
                request.CriancaPessoaId,
                request.Metodo);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Check-in Kids invalido para crianca {CriancaPessoaId}. Metodo={Metodo}",
                request.CriancaPessoaId,
                request.Metodo);
            throw;
        }
    }

    public async Task CheckoutAsync(CheckoutRequest request)
    {
        await _authorizationService.EnsureOperadorAsync();

        _logger.LogInformation(
            "Iniciando check-out Kids para crianca {CriancaPessoaId}. CodigoSessao={CodigoSessao}. CheckoutByPessoaId={CheckoutByPessoaId}. Metodo={Metodo}. UsuarioAtual={UsuarioAtualId}",
            request.CriancaPessoaId,
            request.CodigoSessao,
            request.CheckoutByPessoaId,
            request.Metodo,
            _currentUserContext.UserId);

        List<int>? responsavelIdsForPush = null;
        string? msgForPush = null;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var checkin = await _checkinRepository.GetByCodigoSessaoAsync(request.CodigoSessao);
                if (checkin == null)
                    throw new ArgumentException("Código de sessão inválido");

                if (checkin.CriancaPessoaId != request.CriancaPessoaId)
                    throw new ArgumentException("Código de sessão não corresponde à criança");

                if (checkin.Status != "CheckedIn")
                    throw new InvalidOperationException("Check-in já foi finalizado");

                var podeRetirar = await _responsavelRepository.PodeRetirarAsync(request.CriancaPessoaId, request.CheckoutByPessoaId);
                if (!podeRetirar)
                {
                    var pessoa = await _pessoaRepository.GetByIdAsync(request.CheckoutByPessoaId);
                    if (pessoa == null || !pessoa.Ativo)
                        throw new UnauthorizedAccessException("Você não tem autorização para retirar esta criança");
                }

                checkin.CheckoutTime = DateTime.UtcNow;
                checkin.CheckoutByPessoaId = request.CheckoutByPessoaId;
                checkin.Status = "CheckedOut";
                if (!string.IsNullOrEmpty(request.Metodo))
                    checkin.Metodo = request.Metodo;

                await _checkinRepository.UpdateWithoutSaveAsync(checkin);

                var responsaveis = await _responsavelRepository.GetByCriancaIdAsync(request.CriancaPessoaId);
                var crianca = await _pessoaRepository.GetByIdAsync(request.CriancaPessoaId);
                msgForPush = $"Check-out realizado para {crianca?.Nome ?? "criança"} às {DateTime.UtcNow:HH:mm}";
                responsavelIdsForPush = responsaveis.Select(r => r.ResponsavelPessoaId).Distinct().ToList();

                foreach (var responsavel in responsaveis)
                {
                    var notificacao = new KidsNotificacao
                    {
                        TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                        CriancaPessoaId = request.CriancaPessoaId,
                        ResponsavelPessoaId = responsavel.ResponsavelPessoaId,
                        Titulo = "Check-out realizado",
                        Tipo = "CHECKOUT",
                        Origem = "AUTOMATICA",
                        Mensagem = msgForPush,
                        Status = "Enviado",
                        EnviadoEm = DateTime.UtcNow,
                        DataCriacao = DateTime.UtcNow,
                        CriadoByPessoaId = request.CheckoutByPessoaId
                    };

                    await _notificacaoRepository.CreateWithoutSaveAsync(notificacao);
                }

                await _unitOfWork.SaveChangesAsync();
            });

            if (_pushService != null && responsavelIdsForPush != null && responsavelIdsForPush.Count > 0 && msgForPush != null)
                await _comunicacaoAutomacaoService.ExecutarAvisoContextualKidsAsync(new ComunicacaoAvisoContextualKidsRequest
                {
                    ChaveEvento = $"kids:checkout:{request.CriancaPessoaId}:{request.CodigoSessao}",
                    CriancaPessoaId = request.CriancaPessoaId,
                    ResponsavelPessoaIds = responsavelIdsForPush,
                    Titulo = "App Kids - Check-out",
                    Mensagem = msgForPush,
                    Tipo = "CHECKOUT"
                });

            _logger.LogInformation(
                "Check-out Kids concluido para crianca {CriancaPessoaId}. CodigoSessao={CodigoSessao}. CheckoutByPessoaId={CheckoutByPessoaId}",
                request.CriancaPessoaId,
                request.CodigoSessao,
                request.CheckoutByPessoaId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Check-out Kids negado para crianca {CriancaPessoaId}. CodigoSessao={CodigoSessao}. CheckoutByPessoaId={CheckoutByPessoaId}",
                request.CriancaPessoaId,
                request.CodigoSessao,
                request.CheckoutByPessoaId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Check-out Kids bloqueado para crianca {CriancaPessoaId}. CodigoSessao={CodigoSessao}",
                request.CriancaPessoaId,
                request.CodigoSessao);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Check-out Kids invalido para crianca {CriancaPessoaId}. CodigoSessao={CodigoSessao}",
                request.CriancaPessoaId,
                request.CodigoSessao);
            throw;
        }
    }

    public async Task<IEnumerable<KidsCheckinDto>> GetHistoricoCheckinsAsync(int? criancaPessoaId = null)
    {
        await _authorizationService.EnsureOperadorAsync();
        IEnumerable<KidsCheckin> checkins;
        
        if (criancaPessoaId.HasValue)
        {
            checkins = await _checkinRepository.GetHistoricoPorCriancaAsync(criancaPessoaId.Value);
        }
        else
        {
            checkins = await _checkinRepository.GetCheckinsAtivosAsync();
        }
        
        return checkins.Select(MapToCheckinDto);
    }

    private async Task<int?> GetCurrentUserPessoaIdAsync()
    {
        if (!_currentUserContext.UserId.HasValue)
            return null;

        var usuario = await _usuarioRepository.GetByIdAsync(_currentUserContext.UserId.Value);
        return usuario?.PessoaId;
    }

    private async Task<int> GetRequiredCurrentUserPessoaIdAsync()
    {
        var pessoaId = await GetCurrentUserPessoaIdAsync();
        if (!pessoaId.HasValue)
            throw new UnauthorizedAccessException("Usuário atual sem vínculo de pessoa válido.");

        return pessoaId.Value;
    }

    private async Task EnsureCriancaPertenceAoResponsavelAsync(int criancaPessoaId, int? responsavelPessoaId = null)
    {
        var pessoaId = responsavelPessoaId ?? await GetCurrentUserPessoaIdAsync();
        if (!pessoaId.HasValue)
            throw new UnauthorizedAccessException("Usuário atual sem vínculo de pessoa válido.");

        var possuiVinculo = await _responsavelRepository.ExisteVinculoAtivoAsync(criancaPessoaId, pessoaId.Value);
        if (!possuiVinculo)
            throw new UnauthorizedAccessException("Você não tem acesso a esta criança.");
    }

    private async Task ValidateSalaTurmaAsync(string? salaId, string? turmaId)
    {
        if (string.IsNullOrWhiteSpace(salaId) && string.IsNullOrWhiteSpace(turmaId))
            return;

        var salaNormalizada = string.IsNullOrWhiteSpace(salaId) ? null : salaId.Trim().ToUpperInvariant();
        var turmaNormalizada = string.IsNullOrWhiteSpace(turmaId) ? null : turmaId.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(salaNormalizada))
        {
            var sala = await _kidsEstruturaRepository.GetSalaByIdAsync(salaNormalizada);
            if (sala == null || !sala.Ativo)
                throw new ArgumentException("Sala informada não encontrada ou inativa.");
        }

        if (!string.IsNullOrWhiteSpace(turmaNormalizada))
        {
            var turma = await _kidsEstruturaRepository.GetTurmaByIdAsync(turmaNormalizada);
            if (turma == null || !turma.Ativo)
                throw new ArgumentException("Turma informada não encontrada ou inativa.");

            if (string.IsNullOrWhiteSpace(salaNormalizada))
                throw new ArgumentException("Sala é obrigatória quando uma turma é informada.");

            if (!string.Equals(turma.SalaId, salaNormalizada, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Turma informada não pertence à sala selecionada.");
        }
    }

    private static CriancaDto MapToCriancaDto(
        Pessoa pessoa,
        CriancaDetalhe? detalhe,
        IEnumerable<ResponsavelCrianca> responsaveis,
        KidsCheckin? checkinAtivo)
    {
        return new CriancaDto
        {
            PessoaId = pessoa.Id,
            Nome = pessoa.Nome,
            DataNascimento = pessoa.DataNascimento,
            Email = pessoa.Email,
            Telefone = pessoa.Telefone,
            WhatsApp = pessoa.WhatsApp,
            Ativo = pessoa.Ativo,
            DataCriacao = pessoa.DataCriacao,
            Alergias = detalhe?.Alergias,
            RestricoesAlimentares = detalhe?.RestricoesAlimentares,
            Observacoes = detalhe?.Observacoes,
            SalaId = detalhe?.SalaId,
            TurmaId = detalhe?.TurmaId,
            DataCadastro = detalhe?.DataCadastro ?? pessoa.DataCriacao,
            Responsaveis = responsaveis.Select(MapToResponsavelDto).ToList(),
            EstaCheckedIn = checkinAtivo != null,
            CheckinAtual = checkinAtivo != null ? MapToCheckinDto(checkinAtivo) : null
        };
    }

    private static ResponsavelCriancaDto MapToResponsavelDto(ResponsavelCrianca responsavel)
    {
        return new ResponsavelCriancaDto
        {
            Id = responsavel.Id,
            CriancaPessoaId = responsavel.CriancaPessoaId,
            CriancaNome = responsavel.Crianca?.Nome ?? string.Empty,
            ResponsavelPessoaId = responsavel.ResponsavelPessoaId,
            ResponsavelNome = responsavel.Responsavel?.Nome ?? string.Empty,
            ResponsavelTelefone = responsavel.Responsavel?.Telefone,
            ResponsavelWhatsApp = responsavel.Responsavel?.WhatsApp,
            ResponsavelEmail = responsavel.Responsavel?.Email,
            PodeRetirar = responsavel.PodeRetirar,
            Parentesco = responsavel.Parentesco,
            Ativo = responsavel.Ativo,
            DataCadastro = responsavel.DataCadastro
        };
    }

    private static KidsCheckinDto MapToCheckinDto(KidsCheckin checkin)
    {
        return new KidsCheckinDto
        {
            Id = checkin.Id,
            CriancaPessoaId = checkin.CriancaPessoaId,
            CriancaNome = checkin.Crianca?.Nome ?? string.Empty,
            CheckinTime = checkin.CheckinTime,
            CheckoutTime = checkin.CheckoutTime,
            CheckinByPessoaId = checkin.CheckinByPessoaId,
            CheckinByNome = checkin.CheckinBy?.Nome,
            CheckoutByPessoaId = checkin.CheckoutByPessoaId,
            CheckoutByNome = checkin.CheckoutBy?.Nome,
            Metodo = checkin.Metodo,
            CodigoSessao = checkin.CodigoSessao,
            TokenRetirada = checkin.TokenRetirada,
            PinRetirada = checkin.PinRetirada,
            TokenRetiradaExpiraEm = checkin.TokenRetiradaExpiraEm,
            Status = checkin.Status,
            RetiradaConfirmadaPorPessoaId = checkin.RetiradaConfirmadaPorPessoaId,
            RetiradaMetodo = checkin.RetiradaMetodo,
            RetiradaEmModoExcecao = checkin.RetiradaEmModoExcecao,
            RetiradaMotivoExcecao = checkin.RetiradaMotivoExcecao,
            RetiradaPessoaNome = checkin.RetiradaPessoaNome,
            Observacoes = checkin.Observacoes
        };
    }

    private static MinhaCriancaResumoDto MapToMinhaCriancaResumoDto(
        Pessoa pessoa,
        CriancaDetalhe? detalhe,
        KidsCheckin? checkinAtivo)
    {
        return new MinhaCriancaResumoDto
        {
            PessoaId = pessoa.Id,
            Nome = pessoa.Nome,
            DataNascimento = pessoa.DataNascimento,
            SalaId = detalhe?.SalaId,
            TurmaId = detalhe?.TurmaId,
            EstaCheckedIn = checkinAtivo != null,
            CheckinAtual = checkinAtivo != null ? MapToMeuCheckinResumoDto(checkinAtivo, detalhe?.SalaId) : null,
            TemAlertaCritico = !string.IsNullOrWhiteSpace(detalhe?.Alergias) ||
                               !string.IsNullOrWhiteSpace(detalhe?.RestricoesAlimentares),
            FotoUrl = pessoa.FotoUrl
        };
    }

    private static MinhaCriancaDetalheDto MapToMinhaCriancaDetalheDto(
        Pessoa pessoa,
        CriancaDetalhe? detalhe,
        KidsCheckin? checkinAtivo,
        IEnumerable<KidsCheckin> historico)
    {
        return new MinhaCriancaDetalheDto
        {
            PessoaId = pessoa.Id,
            Nome = pessoa.Nome,
            DataNascimento = pessoa.DataNascimento,
            SalaId = detalhe?.SalaId,
            TurmaId = detalhe?.TurmaId,
            Alergias = detalhe?.Alergias,
            RestricoesAlimentares = detalhe?.RestricoesAlimentares,
            ObservacoesVisiveisAoResponsavel = null,
            EstaCheckedIn = checkinAtivo != null,
            CheckinAtual = checkinAtivo != null ? MapToMeuCheckinResumoDto(checkinAtivo, detalhe?.SalaId) : null,
            HistoricoRecente = historico.Select(c => MapToMeuCheckinResumoDto(c, detalhe?.SalaId)).ToList(),
            FotoUrl = pessoa.FotoUrl
        };
    }

    private static MeuCheckinResumoDto MapToMeuCheckinResumoDto(KidsCheckin checkin, string? salaId = null)
    {
        return new MeuCheckinResumoDto
        {
            Id = checkin.Id,
            CriancaPessoaId = checkin.CriancaPessoaId,
            CriancaNome = checkin.Crianca?.Nome ?? string.Empty,
            CheckinTime = checkin.CheckinTime,
            CheckoutTime = checkin.CheckoutTime,
            Status = checkin.Status,
            SalaId = salaId,
            TokenRetirada = checkin.TokenRetirada,
            PinRetirada = checkin.PinRetirada,
            TokenRetiradaExpiraEm = checkin.TokenRetiradaExpiraEm
        };
    }
}
