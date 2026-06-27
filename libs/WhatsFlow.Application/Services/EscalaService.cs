using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface IEscalaService
{
    Task<EscalaDto?> GetByIdAsync(int id, int usuarioId, bool isAdmin);
    Task<EscalaDto?> GetByEventoOcorrenciaAsync(int eventoOcorrenciaId, int usuarioId, bool isAdmin);
    Task<EscalaDto?> GetByEventoOcorrenciaAndEquipeAsync(int eventoOcorrenciaId, int equipeId, int usuarioId, bool isAdmin);
    Task<IEnumerable<EscalaDto>> GetAllByEventoOcorrenciaAsync(int eventoOcorrenciaId, int usuarioId, bool isAdmin);
    Task<IEnumerable<EscalaDto>> GetMinhasEscalasAsync(int pessoaId, bool somenteFuturas = true);
    Task<IEnumerable<SugestaoEscalaVoluntarioDto>> GetSugestoesAsync(int escalaId, int equipeId, int usuarioId, bool isAdmin);
    Task<EscalaDto> CreateAsync(CriarEscalaDto dto, int usuarioId, bool isAdmin);
    Task<EscalaDto> UpdateAsync(int id, AtualizarEscalaDto dto, int usuarioId, bool isAdmin);
    Task DeleteAsync(int id, int usuarioId, bool isAdmin);

    Task<EscalaItemDto> AddItemAsync(int escalaId, CriarEscalaItemDto dto, int usuarioId, bool isAdmin);
    Task<EscalaItemDto> UpdateItemAsync(int escalaId, int escalaItemId, AtualizarEscalaItemDto dto, int usuarioId, bool isAdmin);
    Task DeleteItemAsync(int escalaId, int escalaItemId, int usuarioId, bool isAdmin);
    Task<EscalaDto> PublicarAsync(int escalaId, int usuarioId, bool isAdmin);
    Task<EscalaDto> GerarAutomaticoAsync(int eventoOcorrenciaId, int equipeId, int usuarioId, bool isAdmin);
    Task<EscalaItemDto> ConfirmarItemAsync(int escalaId, int escalaItemId, int usuarioId, bool isAdmin, int? usuarioPessoaId);
    Task<EscalaItemDto> RecusarItemAsync(int escalaId, int escalaItemId, string? motivoRecusa, int usuarioId, bool isAdmin, int? usuarioPessoaId);
    Task<EscalaItemDto> RegistrarPresencaAsync(int escalaId, int escalaItemId, bool compareceu, string? observacaoOperacional, int usuarioId, bool isAdmin);
    Task<IEnumerable<HistoricoVoluntarioDto>> GetHistoricoVoluntariosAsync(int usuarioId, bool isAdmin, int? equipeId = null, int? eventoId = null, DateTime? dataInicio = null, DateTime? dataFim = null);
    Task<PlanejamentoMensalEscalaDto> GetPlanejamentoMensalAsync(int usuarioId, bool isAdmin, int ano, int mes, int? equipeId = null, int? eventoId = null);
    Task<GerarPlanejamentoMensalResultadoDto> GerarPlanejamentoMensalAutomaticoAsync(GerarPlanejamentoMensalDto dto, int usuarioId, bool isAdmin);
    Task<EscalaItemDto> CriarAlocacaoPlanejamentoMensalAsync(CriarAlocacaoPlanejamentoMensalDto dto, int usuarioId, bool isAdmin);
    Task<int> EnviarLembretesPendentesAsync(DateTime? referencia = null);
}

public class EscalaService : IEscalaService
{
    private readonly IEscalaRepository _repository;
    private readonly IEventoOcorrenciaRepository _eventoOcorrenciaRepository;
    private readonly IVoluntarioRepository _voluntarioRepository;
    private readonly IEscalaModeloRepository _escalaModeloRepository;
    private readonly IIndisponibilidadeVoluntarioRepository _indisponibilidadeRepository;
    private readonly IEquipeRepository _equipeRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly INotificacaoUsuarioService _notificacaoUsuarioService;
    private readonly IComunicacaoAutomacaoService _comunicacaoAutomacaoService;
    private readonly ILogger<EscalaService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;

    public EscalaService(
        IEscalaRepository repository,
        IEventoOcorrenciaRepository eventoOcorrenciaRepository,
        IVoluntarioRepository voluntarioRepository,
        IEscalaModeloRepository escalaModeloRepository,
        IIndisponibilidadeVoluntarioRepository indisponibilidadeRepository,
        IEquipeRepository equipeRepository,
        IUsuarioRepository usuarioRepository,
        INotificacaoUsuarioService notificacaoUsuarioService,
        IComunicacaoAutomacaoService comunicacaoAutomacaoService,
        ILogger<EscalaService> logger,
        IAuditLogService auditLogService,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _eventoOcorrenciaRepository = eventoOcorrenciaRepository;
        _voluntarioRepository = voluntarioRepository;
        _escalaModeloRepository = escalaModeloRepository;
        _indisponibilidadeRepository = indisponibilidadeRepository;
        _equipeRepository = equipeRepository;
        _usuarioRepository = usuarioRepository;
        _notificacaoUsuarioService = notificacaoUsuarioService;
        _comunicacaoAutomacaoService = comunicacaoAutomacaoService;
        _logger = logger;
        _auditLogService = auditLogService;
        _tenantContext = tenantContext;
    }

    public EscalaService(
        IEscalaRepository repository,
        IEventoOcorrenciaRepository eventoOcorrenciaRepository,
        IVoluntarioRepository voluntarioRepository,
        IEscalaModeloRepository escalaModeloRepository,
        IIndisponibilidadeVoluntarioRepository indisponibilidadeRepository,
        IEquipeRepository equipeRepository,
        IUsuarioRepository usuarioRepository,
        INotificacaoUsuarioService notificacaoUsuarioService,
        IComunicacaoAutomacaoService comunicacaoAutomacaoService,
        ILogger<EscalaService> logger,
        IAuditLogService auditLogService)
        : this(repository, eventoOcorrenciaRepository, voluntarioRepository, escalaModeloRepository, indisponibilidadeRepository, equipeRepository, usuarioRepository, notificacaoUsuarioService, comunicacaoAutomacaoService, logger, auditLogService, new DefaultTenantContext())
    {
    }

    public async Task<EscalaDto?> GetByIdAsync(int id, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(id);
        if (escala == null) return null;
        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);
        return escala != null ? MapToDto(escala) : null;
    }

    public async Task<EscalaDto?> GetByEventoOcorrenciaAsync(int eventoOcorrenciaId, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByEventoOcorrenciaIdAsync(eventoOcorrenciaId);
        if (escala == null) return null;
        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);
        return escala != null ? MapToDto(escala) : null;
    }

    public async Task<EscalaDto?> GetByEventoOcorrenciaAndEquipeAsync(int eventoOcorrenciaId, int equipeId, int usuarioId, bool isAdmin)
    {
        await ValidarPermissaoGestaoEquipeAsync(equipeId, usuarioId, isAdmin);
        var escala = await _repository.GetByEventoOcorrenciaAndEquipeAsync(eventoOcorrenciaId, equipeId);
        return escala != null ? MapToDto(escala) : null;
    }

    public async Task<IEnumerable<EscalaDto>> GetAllByEventoOcorrenciaAsync(int eventoOcorrenciaId, int usuarioId, bool isAdmin)
    {
        var escalas = await _repository.GetAllByEventoOcorrenciaAsync(eventoOcorrenciaId);
        if (isAdmin)
        {
            return escalas.Select(MapToDto);
        }

        var equipesGeridas = (await _equipeRepository.GetAllAsync())
            .Where(e => e.LiderUsuarioId == usuarioId)
            .Select(e => e.Id)
            .ToHashSet();

        return escalas
            .Where(e => equipesGeridas.Contains(e.EquipeId))
            .Select(MapToDto);
    }

    public async Task<IEnumerable<EscalaDto>> GetMinhasEscalasAsync(int pessoaId, bool somenteFuturas = true)
    {
        var escalas = await _repository.GetByPessoaIdAsync(pessoaId, somenteFuturas);
        return escalas.Select(e =>
        {
            var dto = MapToDto(e);
            dto.Itens = dto.Itens
                .Where(i => i.VoluntarioPessoaId == pessoaId)
                .OrderBy(i => i.Ordem)
                .ThenBy(i => i.Id)
                .ToList();
            return dto;
        });
    }

    public async Task<IEnumerable<SugestaoEscalaVoluntarioDto>> GetSugestoesAsync(int escalaId, int equipeId, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(escalaId);
        if (escala == null) throw new ArgumentException("Escala não encontrada");
        if (escala.EquipeId != equipeId) throw new ArgumentException("Equipe inválida para esta escala");
        await ValidarPermissaoGestaoEquipeAsync(equipeId, usuarioId, isAdmin);

        var voluntarios = (await _voluntarioRepository.GetByEquipeAsync(equipeId)).ToList();
        var pessoaIdsJaEscaladas = await _repository.GetPessoaIdsJaEscaladasAsync(escalaId);
        var cargaRecente = await _repository.GetCargaRecentePorVoluntarioAsync(equipeId, DateTime.Now.AddDays(-60));

        var sugestoes = voluntarios
            .Select(v =>
            {
                var indisponivel = pessoaIdsJaEscaladas.Contains(v.PessoaId);
                var carga = cargaRecente.TryGetValue(v.Id, out var qtd) ? qtd : 0;
                return new SugestaoEscalaVoluntarioDto
                {
                    VoluntarioId = v.Id,
                    PessoaId = v.PessoaId,
                    VoluntarioNome = v.Pessoa?.Nome ?? string.Empty,
                    EquipeId = v.EquipeId,
                    EquipeNome = v.Equipe?.Nome ?? string.Empty,
                    CargoId = v.CargoId,
                    CargoNome = v.Cargo?.Nome ?? string.Empty,
                    Disponivel = !indisponivel,
                    CargaRecente = carga,
                    MotivoBloqueio = indisponivel ? "Já escalado neste evento" : null
                };
            })
            .OrderByDescending(s => s.Disponivel)
            .ThenBy(s => s.CargaRecente)
            .ThenBy(s => s.VoluntarioNome)
            .ToList();

        return sugestoes;
    }

    public async Task<EscalaDto> CreateAsync(CriarEscalaDto dto, int usuarioId, bool isAdmin)
    {
        await ValidarPermissaoGestaoEquipeAsync(dto.EquipeId, usuarioId, isAdmin);

        var ocorrencia = await _eventoOcorrenciaRepository.GetByIdAsync(dto.EventoOcorrenciaId);
        if (ocorrencia == null) throw new ArgumentException("Ocorrência não encontrada");

        var existente = await _repository.GetByEventoOcorrenciaAndEquipeAsync(dto.EventoOcorrenciaId, dto.EquipeId);
        if (existente != null) throw new ArgumentException("Já existe escala para esta ocorrência e equipe");

        var escala = new Escala
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EventoOcorrenciaId = dto.EventoOcorrenciaId,
            EquipeId = dto.EquipeId,
            Status = StatusEscala.Rascunho,
            Observacoes = dto.Observacoes,
            CriadoPorUsuarioId = usuarioId > 0 ? usuarioId : null,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(escala);
        _logger.LogInformation(
            "Escala criada. EscalaId={EscalaId} EventoOcorrenciaId={EventoOcorrenciaId} EquipeId={EquipeId} UsuarioId={UsuarioId}",
            created.Id,
            created.EventoOcorrenciaId,
            created.EquipeId,
            usuarioId);
        var createdFull = await _repository.GetByIdAsync(created.Id);
        return MapToDto(createdFull!);
    }

    public async Task<EscalaDto> UpdateAsync(int id, AtualizarEscalaDto dto, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(id);
        if (escala == null) throw new ArgumentException("Escala não encontrada");
        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);

        escala.Status = dto.Status;
        escala.Observacoes = dto.Observacoes;

        var updated = await _repository.UpdateAsync(escala);
        _logger.LogInformation(
            "Escala atualizada. EscalaId={EscalaId} EquipeId={EquipeId} Status={Status} UsuarioId={UsuarioId}",
            updated.Id,
            updated.EquipeId,
            updated.Status,
            usuarioId);
        var updatedFull = await _repository.GetByIdAsync(updated.Id);
        if (updatedFull != null)
        {
            await NotificarPublicacaoEscalaAsync(updatedFull, usuarioId);
        }
        return MapToDto(updatedFull!);
    }

    public async Task DeleteAsync(int id, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(id);
        if (escala == null) return;

        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);
        await _repository.DeleteAsync(id);
        _logger.LogInformation(
            "Escala removida. EscalaId={EscalaId} EquipeId={EquipeId} UsuarioId={UsuarioId}",
            id,
            escala.EquipeId,
            usuarioId);
    }

    public async Task<EscalaItemDto> AddItemAsync(int escalaId, CriarEscalaItemDto dto, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(escalaId);
        if (escala == null) throw new ArgumentException("Escala não encontrada");
        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);
        if (escala.Status == StatusEscala.Fechada) throw new ArgumentException("Escala fechada não pode ser alterada");
        if (dto.EquipeId != escala.EquipeId) throw new ArgumentException("O item deve ser da mesma equipe da escala");

        var voluntario = await _voluntarioRepository.GetByIdAsync(dto.VoluntarioId);
        if (voluntario == null) throw new ArgumentException("Voluntário não encontrado");

        await ValidarConflitoPessoaAsync(escalaId, dto.VoluntarioId, dto.ForcarConflito, dto.MotivoExcecao, usuarioId, isAdmin, null);

        var proximaOrdem = escala.Itens.Any()
            ? escala.Itens.Max(i => i.Ordem) + 1
            : 0;

        var item = new EscalaItem
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EscalaId = escalaId,
            EquipeId = escala.EquipeId,
            CargoId = dto.CargoId,
            PessoaId = voluntario.PessoaId,
            VoluntarioId = dto.VoluntarioId,
            Ordem = proximaOrdem,
            ConflitoAprovado = dto.ForcarConflito,
            MotivoExcecao = dto.ForcarConflito ? dto.MotivoExcecao?.Trim() : null,
            AprovadoPorUsuarioId = dto.ForcarConflito ? usuarioId : null,
            AprovadoEm = dto.ForcarConflito ? DateTime.Now : null,
            Status = escala.Status == StatusEscala.Publicada ? StatusEscalaItem.Pendente : StatusEscalaItem.Pendente,
            DataConvite = escala.Status == StatusEscala.Publicada ? DateTime.Now : null,
            DataCriacao = DateTime.Now
        };

        var created = await _repository.AddItemAsync(item);
        _logger.LogInformation(
            "Item de escala criado. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioId={VoluntarioId} CargoId={CargoId} UsuarioId={UsuarioId} ConflitoAprovado={ConflitoAprovado}",
            escalaId,
            created.Id,
            escala.EquipeId,
            created.VoluntarioId,
            created.CargoId,
            usuarioId,
            created.ConflitoAprovado);
        var createdFull = await _repository.GetItemByIdAsync(created.Id);
        return MapItemToDto(createdFull!);
    }

    public async Task<EscalaItemDto> UpdateItemAsync(int escalaId, int escalaItemId, AtualizarEscalaItemDto dto, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(escalaId);
        if (escala == null) throw new ArgumentException("Escala não encontrada");
        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);
        if (escala.Status == StatusEscala.Fechada) throw new ArgumentException("Escala fechada não pode ser alterada");

        var item = await _repository.GetItemByIdAsync(escalaItemId);
        if (item == null || item.EscalaId != escalaId) throw new ArgumentException("Item da escala não encontrado");
        if (dto.EquipeId != escala.EquipeId) throw new ArgumentException("O item deve ser da mesma equipe da escala");

        var voluntario = await _voluntarioRepository.GetByIdAsync(dto.VoluntarioId);
        if (voluntario == null) throw new ArgumentException("Voluntário não encontrado");

        await ValidarConflitoPessoaAsync(escalaId, dto.VoluntarioId, dto.ForcarConflito, dto.MotivoExcecao, usuarioId, isAdmin, escalaItemId);

        item.EquipeId = escala.EquipeId;
        item.CargoId = dto.CargoId;
        item.PessoaId = voluntario.PessoaId;
        item.VoluntarioId = dto.VoluntarioId;
        item.Ordem = dto.Ordem;
        item.ConflitoAprovado = dto.ForcarConflito;
        item.MotivoExcecao = dto.ForcarConflito ? dto.MotivoExcecao?.Trim() : null;
        item.AprovadoPorUsuarioId = dto.ForcarConflito ? usuarioId : null;
        item.AprovadoEm = dto.ForcarConflito ? DateTime.Now : null;

        var updated = await _repository.UpdateItemAsync(item);
        _logger.LogInformation(
            "Item de escala atualizado. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioId={VoluntarioId} CargoId={CargoId} UsuarioId={UsuarioId} ConflitoAprovado={ConflitoAprovado}",
            escalaId,
            updated.Id,
            escala.EquipeId,
            updated.VoluntarioId,
            updated.CargoId,
            usuarioId,
            updated.ConflitoAprovado);
        var updatedFull = await _repository.GetItemByIdAsync(updated.Id);
        return MapItemToDto(updatedFull!);
    }

    public async Task DeleteItemAsync(int escalaId, int escalaItemId, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(escalaId);
        if (escala == null) throw new ArgumentException("Escala não encontrada");
        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);
        if (escala.Status == StatusEscala.Fechada) throw new ArgumentException("Escala fechada não pode ser alterada");

        var item = await _repository.GetItemByIdAsync(escalaItemId);
        if (item == null || item.EscalaId != escalaId) throw new ArgumentException("Item da escala não encontrado");

        await _repository.DeleteItemAsync(escalaItemId);
        _logger.LogInformation(
            "Item de escala removido. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} UsuarioId={UsuarioId}",
            escalaId,
            escalaItemId,
            escala.EquipeId,
            usuarioId);
    }

    public async Task<EscalaDto> PublicarAsync(int escalaId, int usuarioId, bool isAdmin)
    {
        var escala = await _repository.GetByIdAsync(escalaId);
        if (escala == null) throw new ArgumentException("Escala não encontrada");
        await ValidarPermissaoGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);

        if (!escala.Itens.Any())
        {
            throw new ArgumentException("Não é possível publicar escala sem itens");
        }

        escala.Status = StatusEscala.Publicada;
        escala.DataPublicacao = DateTime.Now;

        foreach (var item in escala.Itens)
        {
            if (item.Status == StatusEscalaItem.Confirmado || item.Status == StatusEscalaItem.Serviu)
            {
                continue;
            }

            item.Status = StatusEscalaItem.Pendente;
            item.DataConvite ??= DateTime.Now;
            item.DataConfirmacao = null;
            item.DataRecusa = null;
            item.MotivoRecusa = null;
            item.RespondidoPorUsuarioId = null;
        }

        var updated = await _repository.UpdateAsync(escala);
        _logger.LogInformation(
            "Escala publicada. EscalaId={EscalaId} EquipeId={EquipeId} Itens={Itens} UsuarioId={UsuarioId}",
            updated.Id,
            updated.EquipeId,
            updated.Itens.Count,
            usuarioId);
        await _auditLogService.RecordAsync(
            "Escala",
            updated.Id.ToString(),
            "Publicar",
            new { updated.EquipeId, Itens = updated.Itens.Count, UsuarioId = usuarioId });
        var updatedFull = await _repository.GetByIdAsync(updated.Id);
        return MapToDto(updatedFull!);
    }

    public async Task<EscalaDto> GerarAutomaticoAsync(int eventoOcorrenciaId, int equipeId, int usuarioId, bool isAdmin)
    {
        await ValidarPermissaoGestaoEquipeAsync(equipeId, usuarioId, isAdmin);

        var ocorrencia = await _eventoOcorrenciaRepository.GetByIdAsync(eventoOcorrenciaId);
        if (ocorrencia == null) throw new ArgumentException("Ocorrência não encontrada.");
        var eventoId = ocorrencia.EventoId;
        var dataOcorrencia = ocorrencia.DataHoraInicio;

        var modelo = await _escalaModeloRepository.GetByEventoAndEquipeAsync(eventoId, equipeId);
        if (modelo == null || !modelo.Itens.Any())
            throw new ArgumentException("Não há modelo de escala ativo para este evento e equipe. Cadastre um em Voluntariado → Modelos de Escala.");

        var escala = await _repository.GetByEventoOcorrenciaAndEquipeAsync(eventoOcorrenciaId, equipeId);
        if (escala == null)
        {
            escala = new Escala
            {
                TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                EventoOcorrenciaId = eventoOcorrenciaId,
                EquipeId = equipeId,
                Status = StatusEscala.Rascunho,
                CriadoPorUsuarioId = usuarioId > 0 ? usuarioId : null,
                DataCriacao = DateTime.Now
            };
            escala = await _repository.CreateAsync(escala);
            _logger.LogInformation(
                "Escala rascunho criada para geração automática. EscalaId={EscalaId} EventoOcorrenciaId={EventoOcorrenciaId} EquipeId={EquipeId} UsuarioId={UsuarioId}",
                escala.Id,
                escala.EventoOcorrenciaId,
                escala.EquipeId,
                usuarioId);
        }
        else if (escala.Status != StatusEscala.Rascunho)
            throw new ArgumentException("Só é possível gerar automaticamente em escala em rascunho.");

        foreach (var item in escala.Itens.ToList())
            await _repository.DeleteItemAsync(item.Id);

        var voluntarios = (await _voluntarioRepository.GetByEquipeAsync(equipeId)).ToList();
        var pessoaIdsJaEscaladas = await _repository.GetPessoaIdsJaEscaladasAsync(escala.Id);
        var voluntarioIds = voluntarios.Select(v => v.Id).ToList();
        var indisponiveis = await _indisponibilidadeRepository.GetVoluntarioIdsIndisponiveisNaDataAsync(voluntarioIds, dataOcorrencia);
        var ano = dataOcorrencia.Year;
        var mes = dataOcorrencia.Month;
        var escalasNoMes = await _repository.GetQuantidadeEscalasNoMesPorVoluntarioAsync(equipeId, ano, mes);
        var diasFolga = modelo.DiasFolgaAposEscala ?? 0;
        var dataInicioFolga = dataOcorrencia.Date.AddDays(-diasFolga);
        var escalasPeriodoFolga = diasFolga > 0
            ? await _repository.GetQuantidadeEscalasEmPeriodoPorVoluntarioAsync(equipeId, dataInicioFolga, dataOcorrencia)
            : new Dictionary<int, int>();
        var cargaRecente = await _repository.GetCargaRecentePorVoluntarioAsync(equipeId, dataOcorrencia.AddDays(-60));

        var ordemGlobal = 0;
        foreach (var itemModelo in modelo.Itens.OrderBy(i => i.Ordem).ThenBy(i => i.Id))
        {
            var candidatos = voluntarios
                .Where(v =>
                    (itemModelo.CargoId == null || v.CargoId == itemModelo.CargoId) &&
                    !pessoaIdsJaEscaladas.Contains(v.PessoaId) &&
                    !indisponiveis.Contains(v.Id) &&
                    (!v.MaxEscalasPorMes.HasValue || !escalasNoMes.TryGetValue(v.Id, out var noMes) || noMes < v.MaxEscalasPorMes.Value) &&
                    (diasFolga == 0 || !escalasPeriodoFolga.TryGetValue(v.Id, out var noPeriodo) || noPeriodo == 0))
                .OrderBy(v => cargaRecente.GetValueOrDefault(v.Id, 0))
                .ThenBy(v => v.Pessoa?.Nome ?? string.Empty)
                .Take(itemModelo.Quantidade)
                .ToList();

            foreach (var vol in candidatos)
            {
                var escalaItem = new EscalaItem
                {
                    TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
                    EscalaId = escala.Id,
                    EquipeId = equipeId,
                    CargoId = vol.CargoId,
                    PessoaId = vol.PessoaId,
                    VoluntarioId = vol.Id,
                    Ordem = ordemGlobal++,
                    ConflitoAprovado = false,
                    Status = escala.Status == StatusEscala.Publicada ? StatusEscalaItem.Pendente : StatusEscalaItem.Pendente,
                    DataConvite = escala.Status == StatusEscala.Publicada ? DateTime.Now : null,
                    DataCriacao = DateTime.Now
                };
                await _repository.AddItemAsync(escalaItem);
                pessoaIdsJaEscaladas.Add(vol.PessoaId);
            }
        }

        var updatedFull = await _repository.GetByIdAsync(escala.Id);
        _logger.LogInformation(
            "Escala gerada automaticamente. EscalaId={EscalaId} EventoOcorrenciaId={EventoOcorrenciaId} EquipeId={EquipeId} ItensGerados={ItensGerados} UsuarioId={UsuarioId}",
            escala.Id,
            eventoOcorrenciaId,
            equipeId,
            updatedFull?.Itens.Count ?? 0,
            usuarioId);
        await _auditLogService.RecordAsync(
            "Escala",
            escala.Id.ToString(),
            "GerarAutomatico",
            new { EventoOcorrenciaId = eventoOcorrenciaId, EquipeId = equipeId, ItensGerados = updatedFull?.Itens.Count ?? 0, UsuarioId = usuarioId });
        return MapToDto(updatedFull!);
    }

    public async Task<EscalaItemDto> ConfirmarItemAsync(int escalaId, int escalaItemId, int usuarioId, bool isAdmin, int? usuarioPessoaId)
    {
        var item = await ObterItemComPermissaoAsync(escalaId, escalaItemId, usuarioId, isAdmin, usuarioPessoaId);

        item.Status = StatusEscalaItem.Confirmado;
        item.DataConfirmacao = DateTime.Now;
        item.DataRecusa = null;
        item.MotivoRecusa = null;
        item.RespondidoPorUsuarioId = usuarioId > 0 ? usuarioId : null;
        item.DataConvite ??= DateTime.Now;

        var updated = await _repository.UpdateItemAsync(item);
        var updatedFull = await _repository.GetItemByIdAsync(updated.Id);
        _logger.LogInformation(
            "Escala confirmada. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioId={VoluntarioId} UsuarioId={UsuarioId}",
            escalaId,
            updated.Id,
            updated.EquipeId,
            updated.VoluntarioId,
            usuarioId);
        await _auditLogService.RecordAsync(
            "EscalaItem",
            updated.Id.ToString(),
            "Confirmar",
            new { EscalaId = escalaId, updated.EquipeId, updated.VoluntarioId, UsuarioId = usuarioId });
        if (updatedFull != null)
        {
            await NotificarRespostaEscalaAsync(updatedFull, true, usuarioId);
        }
        return MapItemToDto(updatedFull!);
    }

    public async Task<EscalaItemDto> RecusarItemAsync(int escalaId, int escalaItemId, string? motivoRecusa, int usuarioId, bool isAdmin, int? usuarioPessoaId)
    {
        var item = await ObterItemComPermissaoAsync(escalaId, escalaItemId, usuarioId, isAdmin, usuarioPessoaId);

        item.Status = StatusEscalaItem.Recusado;
        item.DataRecusa = DateTime.Now;
        item.DataConfirmacao = null;
        item.MotivoRecusa = motivoRecusa?.Trim();
        item.RespondidoPorUsuarioId = usuarioId > 0 ? usuarioId : null;
        item.DataConvite ??= DateTime.Now;

        var updated = await _repository.UpdateItemAsync(item);
        var updatedFull = await _repository.GetItemByIdAsync(updated.Id);
        _logger.LogInformation(
            "Escala recusada. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioId={VoluntarioId} UsuarioId={UsuarioId} MotivoInformado={MotivoInformado}",
            escalaId,
            updated.Id,
            updated.EquipeId,
            updated.VoluntarioId,
            usuarioId,
            !string.IsNullOrWhiteSpace(updated.MotivoRecusa));
        await _auditLogService.RecordAsync(
            "EscalaItem",
            updated.Id.ToString(),
            "Recusar",
            new { EscalaId = escalaId, updated.EquipeId, updated.VoluntarioId, UsuarioId = usuarioId, MotivoInformado = !string.IsNullOrWhiteSpace(updated.MotivoRecusa) });
        if (updatedFull != null)
        {
            await NotificarRespostaEscalaAsync(updatedFull, false, usuarioId);
        }
        return MapItemToDto(updatedFull!);
    }

    public async Task<EscalaItemDto> RegistrarPresencaAsync(int escalaId, int escalaItemId, bool compareceu, string? observacaoOperacional, int usuarioId, bool isAdmin)
    {
        var item = await _repository.GetItemByIdAsync(escalaItemId);
        if (item == null || item.EscalaId != escalaId)
        {
            throw new ArgumentException("Item da escala não encontrado");
        }

        await ValidarPermissaoGestaoEquipeAsync(item.EquipeId, usuarioId, isAdmin);

        item.Status = compareceu ? StatusEscalaItem.Serviu : StatusEscalaItem.Faltou;
        item.ObservacaoOperacional = observacaoOperacional?.Trim();
        item.RespondidoPorUsuarioId = usuarioId > 0 ? usuarioId : null;
        item.DataConfirmacao ??= compareceu ? DateTime.Now : item.DataConfirmacao;
        item.DataRecusa = compareceu ? null : item.DataRecusa;
        item.MotivoRecusa = compareceu ? null : item.MotivoRecusa;

        var updated = await _repository.UpdateItemAsync(item);
        _logger.LogInformation(
            "Presença registrada em escala. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioId={VoluntarioId} Compareceu={Compareceu} UsuarioId={UsuarioId}",
            escalaId,
            updated.Id,
            updated.EquipeId,
            updated.VoluntarioId,
            compareceu,
            usuarioId);
        await _auditLogService.RecordAsync(
            "EscalaItem",
            updated.Id.ToString(),
            "RegistrarPresenca",
            new { EscalaId = escalaId, updated.EquipeId, updated.VoluntarioId, Compareceu = compareceu, UsuarioId = usuarioId });
        var updatedFull = await _repository.GetItemByIdAsync(updated.Id);
        return MapItemToDto(updatedFull!);
    }

    public async Task<IEnumerable<HistoricoVoluntarioDto>> GetHistoricoVoluntariosAsync(int usuarioId, bool isAdmin, int? equipeId = null, int? eventoId = null, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var inicio = dataInicio ?? DateTime.Today.AddMonths(-6);
        var fim = dataFim ?? DateTime.Today.AddMonths(1);
        var itens = (await _repository.GetItensComOcorrenciaNoPeriodoAsync(inicio, fim, equipeId, eventoId)).ToList();

        if (!isAdmin)
        {
            var equipesGeridas = (await _equipeRepository.GetAllAsync())
                .Where(e => e.LiderUsuarioId == usuarioId)
                .Select(e => e.Id)
                .ToHashSet();

            itens = itens.Where(i => equipesGeridas.Contains(i.EquipeId)).ToList();
        }

        var inicioMesAtual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var fimMesAtual = inicioMesAtual.AddMonths(1).AddTicks(-1);

        return itens
            .Where(i => i.Pessoa != null)
            .GroupBy(i => i.PessoaId)
            .Select(g => new HistoricoVoluntarioDto
            {
                PessoaId = g.Key,
                VoluntarioNome = g.Select(x => x.Pessoa.Nome).FirstOrDefault() ?? string.Empty,
                Equipes = g.Select(x => x.Equipe?.Nome ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList(),
                TotalEscalas = g.Count(),
                Confirmados = g.Count(x => x.Status == StatusEscalaItem.Confirmado),
                Recusados = g.Count(x => x.Status == StatusEscalaItem.Recusado),
                Substituidos = g.Count(x => x.Status == StatusEscalaItem.Substituido),
                Presencas = g.Count(x => x.Status == StatusEscalaItem.Serviu),
                Faltas = g.Count(x => x.Status == StatusEscalaItem.Faltou),
                Pendentes = g.Count(x => x.Status == StatusEscalaItem.Pendente),
                CargaMesAtual = g.Count(x => x.Escala?.EventoOcorrencia?.DataHoraInicio >= inicioMesAtual && x.Escala?.EventoOcorrencia?.DataHoraInicio <= fimMesAtual),
                UltimaEscalaEm = g
                    .Where(x => x.Escala?.EventoOcorrencia?.DataHoraInicio <= DateTime.Now)
                    .Select(x => (DateTime?)x.Escala!.EventoOcorrencia!.DataHoraInicio)
                    .DefaultIfEmpty()
                    .Max(),
                ProximaEscalaEm = g
                    .Where(x => x.Escala?.EventoOcorrencia?.DataHoraInicio >= DateTime.Now)
                    .Select(x => (DateTime?)x.Escala!.EventoOcorrencia!.DataHoraInicio)
                    .DefaultIfEmpty()
                    .Min(),
            })
            .OrderByDescending(x => x.Faltas)
            .ThenByDescending(x => x.Pendentes)
            .ThenBy(x => x.VoluntarioNome)
            .ToList();
    }

    public async Task<PlanejamentoMensalEscalaDto> GetPlanejamentoMensalAsync(int usuarioId, bool isAdmin, int ano, int mes, int? equipeId = null, int? eventoId = null)
    {
        if (ano < 2000 || ano > 2100) throw new ArgumentException("Ano inválido");
        if (mes < 1 || mes > 12) throw new ArgumentException("Mês inválido");

        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddTicks(-1);
        var equipeIdsPermitidas = await GetEquipeIdsPermitidasAsync(usuarioId, isAdmin, equipeId);

        var ocorrencias = (await _eventoOcorrenciaRepository.GetByPeriodoAsync(inicio, fim, eventoId))
            .OrderBy(o => o.DataHoraInicio)
            .ToList();

        var itens = (await _repository.GetItensComOcorrenciaNoPeriodoAsync(inicio, fim, equipeId, eventoId))
            .Where(i => equipeIdsPermitidas == null || equipeIdsPermitidas.Contains(i.EquipeId))
            .ToList();

        var voluntariosBase = await GetVoluntariosBasePlanejamentoAsync(equipeId, equipeIdsPermitidas);
        var pessoasBase = voluntariosBase
            .Where(v => v.Pessoa != null)
            .GroupBy(v => v.PessoaId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    PessoaId = g.Key,
                    Nome = g.Select(v => v.Pessoa!.Nome).FirstOrDefault() ?? string.Empty,
                    WhatsApp = g.Select(v => v.Pessoa!.WhatsApp).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                    Equipes = g.Select(v => v.Equipe?.Nome ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList(),
                    Cargos = g.Select(v => v.Cargo?.Nome ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList()
                });

        var rowsByPessoa = itens
            .Where(i => i.Pessoa != null)
            .GroupBy(i => i.PessoaId)
            .ToDictionary(g => g.Key, g =>
            {
                pessoasBase.TryGetValue(g.Key, out var pessoaBase);
                var datas = g
                    .Select(i => i.Escala?.EventoOcorrencia?.DataHoraInicio.Date)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                return new PlanejamentoMensalVoluntarioDto
                {
                    PessoaId = g.Key,
                    Nome = pessoaBase?.Nome ?? g.Select(i => i.Pessoa.Nome).FirstOrDefault() ?? string.Empty,
                    WhatsApp = pessoaBase?.WhatsApp ?? g.Select(i => i.Pessoa.WhatsApp).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                    Equipes = pessoaBase?.Equipes ?? g.Select(i => i.Equipe?.Nome ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList(),
                    Cargos = pessoaBase?.Cargos ?? g.Select(i => i.Cargo?.Nome ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList(),
                    TotalEscalas = g.Count(),
                    Confirmados = g.Count(i => i.Status == StatusEscalaItem.Confirmado || i.Status == StatusEscalaItem.Serviu),
                    Pendentes = g.Count(i => i.Status == StatusEscalaItem.Pendente),
                    Recusados = g.Count(i => i.Status == StatusEscalaItem.Recusado || i.Status == StatusEscalaItem.Substituido),
                    Faltas = g.Count(i => i.Status == StatusEscalaItem.Faltou),
                    TemDomingosConsecutivos = TemDatasConsecutivas(datas),
                    Alocacoes = g
                        .OrderBy(i => i.Escala?.EventoOcorrencia?.DataHoraInicio)
                        .ThenBy(i => i.Equipe?.Nome)
                        .Select(i => new PlanejamentoMensalAlocacaoDto
                        {
                            EscalaId = i.EscalaId,
                            EscalaItemId = i.Id,
                            OcorrenciaId = i.Escala?.EventoOcorrenciaId ?? 0,
                            EquipeId = i.EquipeId,
                            EquipeNome = i.Equipe?.Nome ?? string.Empty,
                            CargoId = i.CargoId,
                            CargoNome = i.Cargo?.Nome,
                            DataHoraInicio = i.Escala?.EventoOcorrencia?.DataHoraInicio ?? DateTime.MinValue,
                            Status = i.Status
                        })
                        .ToList()
                };
            });

        foreach (var pessoa in pessoasBase.Values)
        {
            if (rowsByPessoa.ContainsKey(pessoa.PessoaId)) continue;
            rowsByPessoa[pessoa.PessoaId] = new PlanejamentoMensalVoluntarioDto
            {
                PessoaId = pessoa.PessoaId,
                Nome = pessoa.Nome,
                WhatsApp = pessoa.WhatsApp,
                Equipes = pessoa.Equipes,
                Cargos = pessoa.Cargos
            };
        }

        var rows = rowsByPessoa.Values
            .OrderBy(v => v.Nome)
            .ThenBy(v => v.PessoaId)
            .ToList();

        return new PlanejamentoMensalEscalaDto
        {
            DataInicio = inicio,
            DataFim = fim,
            EventoId = eventoId,
            EquipeId = equipeId,
            Ocorrencias = ocorrencias.Select(o => new PlanejamentoMensalOcorrenciaDto
            {
                OcorrenciaId = o.Id,
                EventoId = o.EventoId,
                EventoTitulo = o.Evento?.Titulo ?? string.Empty,
                DataHoraInicio = o.DataHoraInicio,
                TotalEscalados = itens.Count(i => i.Escala?.EventoOcorrenciaId == o.Id)
            }).ToList(),
            Voluntarios = rows,
            Resumo = new PlanejamentoMensalResumoDto
            {
                TotalVoluntarios = rows.Count,
                TotalEscalas = itens.Count,
                VoluntariosSemEscala = rows.Count(v => v.TotalEscalas == 0),
                VoluntariosComMaisDeDuasEscalas = rows.Count(v => v.TotalEscalas > 2),
                VoluntariosComDomingosConsecutivos = rows.Count(v => v.TemDomingosConsecutivos)
            }
        };
    }

    public async Task<GerarPlanejamentoMensalResultadoDto> GerarPlanejamentoMensalAutomaticoAsync(GerarPlanejamentoMensalDto dto, int usuarioId, bool isAdmin)
    {
        if (dto.EquipeId <= 0) throw new ArgumentException("Equipe é obrigatória para gerar o mês automaticamente");
        if (dto.Ano < 2000 || dto.Ano > 2100) throw new ArgumentException("Ano inválido");
        if (dto.Mes < 1 || dto.Mes > 12) throw new ArgumentException("Mês inválido");

        await ValidarPermissaoGestaoEquipeAsync(dto.EquipeId, usuarioId, isAdmin);

        var inicio = new DateTime(dto.Ano, dto.Mes, 1);
        var fim = inicio.AddMonths(1).AddTicks(-1);
        var ocorrencias = (await _eventoOcorrenciaRepository.GetByPeriodoAsync(inicio, fim, dto.EventoId))
            .OrderBy(o => o.DataHoraInicio)
            .ToList();

        var resultado = new GerarPlanejamentoMensalResultadoDto
        {
            OcorrenciasProcessadas = ocorrencias.Count
        };

        foreach (var ocorrencia in ocorrencias)
        {
            try
            {
                var escala = await GerarAutomaticoAsync(ocorrencia.Id, dto.EquipeId, usuarioId, isAdmin);
                resultado.EscalasGeradas += 1;

                if (!escala.Itens.Any())
                {
                    resultado.Avisos.Add($"{ocorrencia.DataHoraInicio:dd/MM}: nenhuma pessoa foi selecionada automaticamente.");
                }
            }
            catch (ArgumentException ex)
            {
                resultado.Avisos.Add($"{ocorrencia.DataHoraInicio:dd/MM}: {ex.Message}");
            }
        }

        return resultado;
    }

    public async Task<EscalaItemDto> CriarAlocacaoPlanejamentoMensalAsync(CriarAlocacaoPlanejamentoMensalDto dto, int usuarioId, bool isAdmin)
    {
        if (dto.EventoOcorrenciaId <= 0) throw new ArgumentException("Ocorrência é obrigatória");
        if (dto.EquipeId <= 0) throw new ArgumentException("Equipe é obrigatória");
        if (dto.VoluntarioId <= 0) throw new ArgumentException("Voluntário é obrigatório");

        await ValidarPermissaoGestaoEquipeAsync(dto.EquipeId, usuarioId, isAdmin);

        var escala = await _repository.GetByEventoOcorrenciaAndEquipeAsync(dto.EventoOcorrenciaId, dto.EquipeId);
        if (escala == null)
        {
            var created = await CreateAsync(new CriarEscalaDto
            {
                EventoOcorrenciaId = dto.EventoOcorrenciaId,
                EquipeId = dto.EquipeId
            }, usuarioId, isAdmin);
            escala = await _repository.GetByIdAsync(created.Id);
        }

        if (escala == null) throw new ArgumentException("Não foi possível criar a escala");

        return await AddItemAsync(escala.Id, new CriarEscalaItemDto
        {
            EquipeId = dto.EquipeId,
            VoluntarioId = dto.VoluntarioId,
            CargoId = dto.CargoId,
            ForcarConflito = dto.ForcarConflito,
            MotivoExcecao = dto.MotivoExcecao
        }, usuarioId, isAdmin);
    }

    public async Task<int> EnviarLembretesPendentesAsync(DateTime? referencia = null)
    {
        var agora = referencia ?? DateTime.Now;
        var atualizados = new List<EscalaItem>();
        var lembretes = new List<ComunicacaoLembreteOperacionalRequest>();

        var janela24Inicio = agora.AddHours(23);
        var janela24Fim = agora.AddHours(25);
        var janela7Inicio = agora.AddDays(6).AddHours(20);
        var janela7Fim = agora.AddDays(7).AddHours(4);

        var itens = (await _repository.GetItensComOcorrenciaNoPeriodoAsync(agora, janela7Fim)).ToList();

        foreach (var item in itens)
        {
            if (item.Status is StatusEscalaItem.Recusado or StatusEscalaItem.Substituido or StatusEscalaItem.Faltou)
            {
                continue;
            }

            var pessoaId = item.Voluntario?.PessoaId;
            if (!pessoaId.HasValue || pessoaId.Value <= 0)
            {
                continue;
            }

            var usuario = await _usuarioRepository.GetByPessoaIdAsync(pessoaId.Value);
            if (usuario == null)
            {
                continue;
            }

            var dataEvento = item.Escala?.EventoOcorrencia?.DataHoraInicio;
            if (!dataEvento.HasValue)
            {
                continue;
            }

            var lembreteTitulo = string.Empty;
            var lembreteMensagem = string.Empty;

            if (!item.DataLembrete24HorasEnviado.HasValue && dataEvento.Value >= janela24Inicio && dataEvento.Value <= janela24Fim)
            {
                item.DataLembrete24HorasEnviado = agora;
                lembreteTitulo = "Lembrete: escala em 24 horas";
                lembreteMensagem = $"Sua escala para {item.Escala?.EventoOcorrencia?.Evento?.Titulo ?? "o evento"} acontece em breve, no dia {dataEvento.Value:dd/MM/yyyy HH:mm}.";
            }
            else if (!item.DataLembrete7DiasEnviado.HasValue && dataEvento.Value >= janela7Inicio && dataEvento.Value <= janela7Fim)
            {
                item.DataLembrete7DiasEnviado = agora;
                lembreteTitulo = "Lembrete: escala nesta semana";
                lembreteMensagem = $"Sua escala para {item.Escala?.EventoOcorrencia?.Evento?.Titulo ?? "o evento"} está chegando: {dataEvento.Value:dd/MM/yyyy HH:mm}.";
            }

            if (string.IsNullOrWhiteSpace(lembreteTitulo))
            {
                continue;
            }

            atualizados.Add(item);
            lembretes.Add(new ComunicacaoLembreteOperacionalRequest
            {
                ChaveEvento = $"escala:{item.Id}:{(item.DataLembrete24HorasEnviado.HasValue ? "24h" : "7d")}",
                PessoaId = pessoaId.Value,
                Titulo = lembreteTitulo,
                Mensagem = lembreteMensagem,
                Objetivo = "lembrete-escala"
            });
        }

        foreach (var item in atualizados)
        {
            await _repository.UpdateItemAsync(item);
        }

        var totalEnviado = await _comunicacaoAutomacaoService.ExecutarLembretesOperacionaisAsync(lembretes);
        _logger.LogInformation(
            "Lembretes de escala processados. Referencia={Referencia} Quantidade={Quantidade}",
            agora,
            totalEnviado);
        return totalEnviado;
    }

    private async Task ValidarConflitoPessoaAsync(
        int escalaId,
        int voluntarioId,
        bool forcarConflito,
        string? motivoExcecao,
        int usuarioId,
        bool isAdmin,
        int? ignorarEscalaItemId)
    {
        var conflito = await _repository.GetConflitoPessoaNaEscalaAsync(escalaId, voluntarioId, ignorarEscalaItemId);
        if (conflito == null) return;

        if (!forcarConflito)
        {
            var nome = conflito.Voluntario?.Pessoa?.Nome ?? "Voluntário";
            var equipe = conflito.Equipe?.Nome ?? "Equipe";
            throw new ArgumentException($"{nome} já está escalado neste evento pela equipe '{equipe}'.");
        }

        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Apenas administradores podem aprovar exceção de conflito.");
        }

        if (string.IsNullOrWhiteSpace(motivoExcecao))
        {
            throw new ArgumentException("Motivo da exceção é obrigatório ao forçar conflito.");
        }

        if (usuarioId <= 0)
        {
            throw new ArgumentException("Usuário aprovador inválido.");
        }
    }

    private async Task ValidarPermissaoGestaoEquipeAsync(int equipeId, int usuarioId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        var ehLider = await _equipeRepository.IsLiderUsuarioDaEquipeAsync(usuarioId, equipeId);
        if (!ehLider)
        {
            _logger.LogWarning(
                "Acesso negado para gestão de escala. EquipeId={EquipeId} UsuarioId={UsuarioId}",
                equipeId,
                usuarioId);
            throw new UnauthorizedAccessException("Você não tem permissão para gerenciar escalas desta equipe.");
        }
    }

    private async Task<HashSet<int>?> GetEquipeIdsPermitidasAsync(int usuarioId, bool isAdmin, int? equipeId)
    {
        if (isAdmin)
        {
            if (equipeId.HasValue)
            {
                return new HashSet<int> { equipeId.Value };
            }

            return null;
        }

        var equipesGeridas = (await _equipeRepository.GetAllAsync())
            .Where(e => e.LiderUsuarioId == usuarioId)
            .Select(e => e.Id)
            .ToHashSet();

        if (equipeId.HasValue && !equipesGeridas.Contains(equipeId.Value))
        {
            throw new UnauthorizedAccessException("Você não tem permissão para gerenciar escalas desta equipe.");
        }

        return equipeId.HasValue ? new HashSet<int> { equipeId.Value } : equipesGeridas;
    }

    private async Task<List<Voluntario>> GetVoluntariosBasePlanejamentoAsync(int? equipeId, HashSet<int>? equipeIdsPermitidas)
    {
        var voluntarios = equipeId.HasValue
            ? (await _voluntarioRepository.GetByEquipeAsync(equipeId.Value)).ToList()
            : (await _voluntarioRepository.GetAllAsync()).ToList();

        if (equipeIdsPermitidas != null)
        {
            voluntarios = voluntarios.Where(v => equipeIdsPermitidas.Contains(v.EquipeId)).ToList();
        }

        return voluntarios;
    }

    private static bool TemDatasConsecutivas(IReadOnlyList<DateTime> datas)
    {
        for (var i = 1; i < datas.Count; i++)
        {
            var diferenca = (datas[i] - datas[i - 1]).TotalDays;
            if (diferenca > 0 && diferenca <= 8)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<EscalaItem> ObterItemComPermissaoAsync(
        int escalaId,
        int escalaItemId,
        int usuarioId,
        bool isAdmin,
        int? usuarioPessoaId)
    {
        var item = await _repository.GetItemByIdAsync(escalaItemId);
        if (item == null || item.EscalaId != escalaId)
        {
            throw new ArgumentException("Item da escala não encontrado");
        }

        var podeGerenciarEquipe = isAdmin || await _equipeRepository.IsLiderUsuarioDaEquipeAsync(usuarioId, item.EquipeId);
        var ehProprioVoluntario = usuarioPessoaId.HasValue && item.Voluntario?.PessoaId == usuarioPessoaId.Value;

        if (!podeGerenciarEquipe && !ehProprioVoluntario)
        {
            _logger.LogWarning(
                "Resposta de escala negada por permissão. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} UsuarioId={UsuarioId} UsuarioPessoaId={UsuarioPessoaId}",
                escalaId,
                escalaItemId,
                item.EquipeId,
                usuarioId,
                usuarioPessoaId);
            throw new UnauthorizedAccessException("Você não tem permissão para responder esta escala.");
        }

        return item;
    }

    private async Task NotificarPublicacaoEscalaAsync(Escala escala, int usuarioId)
    {
        var notificacoes = new List<CriarNotificacaoUsuarioDto>();
        var dataEvento = escala.EventoOcorrencia?.DataHoraInicio.ToString("dd/MM/yyyy HH:mm") ?? "data a confirmar";

        foreach (var item in escala.Itens)
        {
            var pessoaId = item.Voluntario?.PessoaId;
            if (!pessoaId.HasValue || pessoaId.Value <= 0) continue;

            var usuario = await _usuarioRepository.GetByPessoaIdAsync(pessoaId.Value);
            if (usuario == null || usuario.Id == usuarioId) continue;

            notificacoes.Add(new CriarNotificacaoUsuarioDto
            {
                UsuarioId = usuario.Id,
                Tipo = TipoNotificacaoUsuario.Escala,
                Titulo = "Nova escala publicada",
                Mensagem = $"Voce foi escalado para {escala.Equipe?.Nome ?? "uma equipe"} em {escala.EventoOcorrencia?.Evento?.Titulo ?? "um evento"} no dia {dataEvento}.",
                Link = $"/minhas-escalas"
            });
        }

        await _notificacaoUsuarioService.CriarParaUsuariosAsync(notificacoes);
    }

    private async Task NotificarRespostaEscalaAsync(EscalaItem item, bool confirmado, int usuarioId)
    {
        var liderUsuarioId = item.Equipe?.LiderUsuarioId;
        if (!liderUsuarioId.HasValue || liderUsuarioId.Value == usuarioId) return;

        var dataEvento = item.Escala?.EventoOcorrencia?.DataHoraInicio.ToString("dd/MM/yyyy HH:mm") ?? "data a confirmar";
        var acao = confirmado ? "confirmou" : "recusou";
        var complemento = confirmado
            ? $"confirmou a escala de {item.Equipe?.Nome ?? "equipe"}."
            : $"recusou a escala de {item.Equipe?.Nome ?? "equipe"}" + (string.IsNullOrWhiteSpace(item.MotivoRecusa) ? "." : $". Motivo: {item.MotivoRecusa}");

        await _notificacaoUsuarioService.CriarAsync(new CriarNotificacaoUsuarioDto
        {
            UsuarioId = liderUsuarioId.Value,
            Tipo = TipoNotificacaoUsuario.Escala,
            Titulo = confirmado ? "Escala confirmada" : "Escala recusada",
            Mensagem = $"{item.Voluntario?.Pessoa?.Nome ?? "Um voluntario"} {acao} para {item.Escala?.EventoOcorrencia?.Evento?.Titulo ?? "o evento"} em {dataEvento} e {complemento}",
            Link = $"/voluntariado/escalas/ocorrencia/{item.Escala?.EventoOcorrenciaId}/equipe/{item.EquipeId}"
        });
    }

    private static EscalaDto MapToDto(Escala e)
    {
        return new EscalaDto
        {
            Id = e.Id,
            EventoOcorrenciaId = e.EventoOcorrenciaId,
            EquipeId = e.EquipeId,
            EquipeNome = e.Equipe?.Nome ?? string.Empty,
            EventoDataHoraInicio = e.EventoOcorrencia?.DataHoraInicio ?? DateTime.MinValue,
            EventoTitulo = e.EventoOcorrencia?.Evento?.Titulo ?? string.Empty,
            Status = e.Status,
            Observacoes = e.Observacoes,
            CriadoPorUsuarioId = e.CriadoPorUsuarioId,
            CriadoPorUsuarioNome = e.CriadoPorUsuario?.Pessoa?.Nome,
            DataCriacao = e.DataCriacao,
            DataPublicacao = e.DataPublicacao,
            Itens = e.Itens
                .OrderBy(i => i.Ordem)
                .ThenBy(i => i.Id)
                .Select(MapItemToDto)
                .ToList()
        };
    }

    private static EscalaItemDto MapItemToDto(EscalaItem i)
    {
        return new EscalaItemDto
        {
            Id = i.Id,
            EscalaId = i.EscalaId,
            EquipeId = i.EquipeId,
            EquipeNome = i.Equipe?.Nome ?? string.Empty,
            CargoId = i.CargoId,
            CargoNome = i.Cargo?.Nome,
            VoluntarioId = i.VoluntarioId,
            VoluntarioPessoaId = i.PessoaId,
            VoluntarioNome = i.Pessoa?.Nome ?? string.Empty,
            Ordem = i.Ordem,
            ConflitoAprovado = i.ConflitoAprovado,
            MotivoExcecao = i.MotivoExcecao,
            AprovadoPorUsuarioId = i.AprovadoPorUsuarioId,
            AprovadoPorUsuarioNome = i.AprovadoPorUsuario?.Pessoa?.Nome,
            AprovadoEm = i.AprovadoEm,
            Status = i.Status,
            DataConvite = i.DataConvite,
            DataConfirmacao = i.DataConfirmacao,
            DataRecusa = i.DataRecusa,
            DataLembrete7DiasEnviado = i.DataLembrete7DiasEnviado,
            DataLembrete24HorasEnviado = i.DataLembrete24HorasEnviado,
            MotivoRecusa = i.MotivoRecusa,
            RespondidoPorUsuarioId = i.RespondidoPorUsuarioId,
            RespondidoPorUsuarioNome = i.RespondidoPorUsuario?.Pessoa?.Nome,
            ObservacaoOperacional = i.ObservacaoOperacional,
            DataCriacao = i.DataCriacao
        };
    }
}
