using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface ISolicitacaoTrocaEscalaService
{
    Task<IEnumerable<SolicitacaoTrocaEscalaDto>> GetGerenciaveisAsync(int usuarioId, bool isAdmin, int? equipeId, StatusSolicitacaoTrocaEscala? status);
    Task<IEnumerable<SolicitacaoTrocaEscalaDto>> GetByEscalaAsync(int escalaId, int usuarioId, bool isAdmin);
    Task<IEnumerable<SolicitacaoTrocaEscalaDto>> GetMinhasAsync(int pessoaId);
    Task<SolicitacaoTrocaEscalaDto> CreateAsync(int escalaId, int escalaItemId, CriarSolicitacaoTrocaEscalaDto dto, int usuarioId, bool isAdmin, int? usuarioPessoaId);
    Task<SolicitacaoTrocaEscalaDto> AprovarAsync(int id, AprovarSolicitacaoTrocaEscalaDto dto, int usuarioId, bool isAdmin);
    Task<SolicitacaoTrocaEscalaDto> RejeitarAsync(int id, RejeitarSolicitacaoTrocaEscalaDto dto, int usuarioId, bool isAdmin);
}

public class SolicitacaoTrocaEscalaService : ISolicitacaoTrocaEscalaService
{
    private readonly ISolicitacaoTrocaEscalaRepository _repository;
    private readonly IEscalaRepository _escalaRepository;
    private readonly IEquipeRepository _equipeRepository;
    private readonly IVoluntarioRepository _voluntarioRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly INotificacaoUsuarioService _notificacaoUsuarioService;
    private readonly ILogger<SolicitacaoTrocaEscalaService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;

    public SolicitacaoTrocaEscalaService(
        ISolicitacaoTrocaEscalaRepository repository,
        IEscalaRepository escalaRepository,
        IEquipeRepository equipeRepository,
        IVoluntarioRepository voluntarioRepository,
        IUsuarioRepository usuarioRepository,
        INotificacaoUsuarioService notificacaoUsuarioService,
        ILogger<SolicitacaoTrocaEscalaService> logger,
        IAuditLogService auditLogService,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _escalaRepository = escalaRepository;
        _equipeRepository = equipeRepository;
        _voluntarioRepository = voluntarioRepository;
        _usuarioRepository = usuarioRepository;
        _notificacaoUsuarioService = notificacaoUsuarioService;
        _logger = logger;
        _auditLogService = auditLogService;
        _tenantContext = tenantContext;
    }

    public SolicitacaoTrocaEscalaService(
        ISolicitacaoTrocaEscalaRepository repository,
        IEscalaRepository escalaRepository,
        IEquipeRepository equipeRepository,
        IVoluntarioRepository voluntarioRepository,
        IUsuarioRepository usuarioRepository,
        INotificacaoUsuarioService notificacaoUsuarioService,
        ILogger<SolicitacaoTrocaEscalaService> logger,
        IAuditLogService auditLogService)
        : this(repository, escalaRepository, equipeRepository, voluntarioRepository, usuarioRepository, notificacaoUsuarioService, logger, auditLogService, new DefaultTenantContext())
    {
    }

    public async Task<IEnumerable<SolicitacaoTrocaEscalaDto>> GetGerenciaveisAsync(int usuarioId, bool isAdmin, int? equipeId, StatusSolicitacaoTrocaEscala? status)
    {
        var list = await _repository.GetGerenciaveisAsync(usuarioId, isAdmin, equipeId, status);
        return list.Select(MapToDto);
    }

    public async Task<IEnumerable<SolicitacaoTrocaEscalaDto>> GetByEscalaAsync(int escalaId, int usuarioId, bool isAdmin)
    {
        var escala = await _escalaRepository.GetByIdAsync(escalaId);
        if (escala == null) throw new ArgumentException("Escala não encontrada");

        await ValidarGestaoEquipeAsync(escala.EquipeId, usuarioId, isAdmin);

        var list = await _repository.GetByEscalaAsync(escalaId);
        return list.Select(MapToDto);
    }

    public async Task<IEnumerable<SolicitacaoTrocaEscalaDto>> GetMinhasAsync(int pessoaId)
    {
        var list = await _repository.GetByPessoaAsync(pessoaId);
        return list.Select(MapToDto);
    }

    public async Task<SolicitacaoTrocaEscalaDto> CreateAsync(int escalaId, int escalaItemId, CriarSolicitacaoTrocaEscalaDto dto, int usuarioId, bool isAdmin, int? usuarioPessoaId)
    {
        var item = await _escalaRepository.GetItemByIdAsync(escalaItemId);
        if (item == null || item.EscalaId != escalaId) throw new ArgumentException("Item da escala não encontrado");

        var podeGerenciarEquipe = isAdmin || await _equipeRepository.IsLiderUsuarioDaEquipeAsync(usuarioId, item.EquipeId);
        var ehProprioVoluntario = usuarioPessoaId.HasValue && item.Voluntario?.PessoaId == usuarioPessoaId.Value;
        if (!podeGerenciarEquipe && !ehProprioVoluntario)
        {
            _logger.LogWarning(
                "Solicitação de troca negada por permissão. EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} UsuarioId={UsuarioId} UsuarioPessoaId={UsuarioPessoaId}",
                escalaId,
                escalaItemId,
                item.EquipeId,
                usuarioId,
                usuarioPessoaId);
            throw new UnauthorizedAccessException("Você não pode solicitar troca para esta escala.");
        }

        if (item.Status == StatusEscalaItem.Substituido || item.Status == StatusEscalaItem.Faltou)
        {
            throw new ArgumentException("Este item não permite solicitação de troca.");
        }

        var existente = await _repository.GetPendenteByEscalaItemAsync(escalaItemId);
        if (existente != null)
        {
            throw new ArgumentException("Já existe uma solicitação de troca pendente para este item.");
        }

        var solicitacao = new SolicitacaoTrocaEscala
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EscalaItemId = escalaItemId,
            VoluntarioSolicitanteId = item.VoluntarioId ?? throw new InvalidOperationException("Este item de escala não possui mais um voluntário vinculado e não pode ser usado para solicitar troca."),
            Motivo = dto.Motivo?.Trim(),
            Status = StatusSolicitacaoTrocaEscala.Pendente,
            DataSolicitacao = DateTime.Now
        };

        var created = await _repository.CreateAsync(solicitacao);
        _logger.LogInformation(
            "Solicitação de troca criada. SolicitacaoId={SolicitacaoId} EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioSolicitanteId={VoluntarioSolicitanteId} UsuarioId={UsuarioId}",
            created.Id,
            escalaId,
            escalaItemId,
            item.EquipeId,
            created.VoluntarioSolicitanteId,
            usuarioId);
        var full = await _repository.GetByIdAsync(created.Id);
        if (full != null)
        {
            await NotificarSolicitacaoCriadaAsync(full, usuarioId);
        }
        return MapToDto(full!);
    }

    public async Task<SolicitacaoTrocaEscalaDto> AprovarAsync(int id, AprovarSolicitacaoTrocaEscalaDto dto, int usuarioId, bool isAdmin)
    {
        var solicitacao = await _repository.GetByIdAsync(id);
        if (solicitacao == null) throw new ArgumentException("Solicitação não encontrada");
        if (solicitacao.Status != StatusSolicitacaoTrocaEscala.Pendente) throw new ArgumentException("Solicitação já foi respondida");

        var item = await _escalaRepository.GetItemByIdAsync(solicitacao.EscalaItemId);
        if (item == null) throw new ArgumentException("Item da escala não encontrado");

        await ValidarGestaoEquipeAsync(item.EquipeId, usuarioId, isAdmin);

        var substituto = await _voluntarioRepository.GetByIdAsync(dto.VoluntarioSubstitutoId);
        if (substituto == null) throw new ArgumentException("Voluntário substituto não encontrado");
        if (substituto.EquipeId != item.EquipeId) throw new ArgumentException("O substituto deve ser da mesma equipe");
        if (substituto.PessoaId == item.Voluntario?.PessoaId) throw new ArgumentException("O substituto deve ser diferente do solicitante");

        var conflito = await _escalaRepository.GetConflitoPessoaNaEscalaAsync(item.EscalaId, substituto.Id);
        if (conflito != null)
        {
            throw new ArgumentException("O voluntário substituto já está escalado neste evento.");
        }

        item.Status = StatusEscalaItem.Substituido;
        item.DataRecusa ??= DateTime.Now;
        item.MotivoRecusa = solicitacao.Motivo;
        item.RespondidoPorUsuarioId = usuarioId;
        await _escalaRepository.UpdateItemAsync(item);

        var novoItem = new EscalaItem
        {
            TenantId = _tenantContext.TenantId ?? Tenant.InitialTenantId,
            EscalaId = item.EscalaId,
            EquipeId = item.EquipeId,
            CargoId = item.CargoId,
            PessoaId = substituto.PessoaId,
            VoluntarioId = substituto.Id,
            Ordem = item.Ordem,
            Status = StatusEscalaItem.Pendente,
            DataConvite = DateTime.Now,
            ObservacaoOperacional = $"Substituição da solicitação #{solicitacao.Id}",
            DataCriacao = DateTime.Now
        };
        await _escalaRepository.AddItemAsync(novoItem);

        solicitacao.VoluntarioSubstitutoId = substituto.Id;
        solicitacao.Status = StatusSolicitacaoTrocaEscala.Aprovada;
        solicitacao.ObservacaoResposta = dto.ObservacaoResposta?.Trim();
        solicitacao.RespondidoPorUsuarioId = usuarioId;
        solicitacao.DataResposta = DateTime.Now;

        var updated = await _repository.UpdateAsync(solicitacao);
        var full = await _repository.GetByIdAsync(updated.Id);
        _logger.LogInformation(
            "Solicitação de troca aprovada. SolicitacaoId={SolicitacaoId} EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioSolicitanteId={VoluntarioSolicitanteId} VoluntarioSubstitutoId={VoluntarioSubstitutoId} UsuarioId={UsuarioId}",
            updated.Id,
            item.EscalaId,
            item.Id,
            item.EquipeId,
            updated.VoluntarioSolicitanteId,
            updated.VoluntarioSubstitutoId,
            usuarioId);
        await _auditLogService.RecordAsync(
            "SolicitacaoTrocaEscala",
            updated.Id.ToString(),
            "Aprovar",
            new { EscalaId = item.EscalaId, EscalaItemId = item.Id, item.EquipeId, updated.VoluntarioSolicitanteId, updated.VoluntarioSubstitutoId, UsuarioId = usuarioId });
        if (full != null)
        {
            await NotificarSolicitacaoAprovadaAsync(full, substituto, usuarioId);
        }
        return MapToDto(full!);
    }

    public async Task<SolicitacaoTrocaEscalaDto> RejeitarAsync(int id, RejeitarSolicitacaoTrocaEscalaDto dto, int usuarioId, bool isAdmin)
    {
        var solicitacao = await _repository.GetByIdAsync(id);
        if (solicitacao == null) throw new ArgumentException("Solicitação não encontrada");
        if (solicitacao.Status != StatusSolicitacaoTrocaEscala.Pendente) throw new ArgumentException("Solicitação já foi respondida");

        var item = await _escalaRepository.GetItemByIdAsync(solicitacao.EscalaItemId);
        if (item == null) throw new ArgumentException("Item da escala não encontrado");

        await ValidarGestaoEquipeAsync(item.EquipeId, usuarioId, isAdmin);

        solicitacao.Status = StatusSolicitacaoTrocaEscala.Rejeitada;
        solicitacao.ObservacaoResposta = dto.ObservacaoResposta?.Trim();
        solicitacao.RespondidoPorUsuarioId = usuarioId;
        solicitacao.DataResposta = DateTime.Now;

        var updated = await _repository.UpdateAsync(solicitacao);
        var full = await _repository.GetByIdAsync(updated.Id);
        _logger.LogInformation(
            "Solicitação de troca rejeitada. SolicitacaoId={SolicitacaoId} EscalaId={EscalaId} EscalaItemId={EscalaItemId} EquipeId={EquipeId} VoluntarioSolicitanteId={VoluntarioSolicitanteId} UsuarioId={UsuarioId}",
            updated.Id,
            item.EscalaId,
            item.Id,
            item.EquipeId,
            updated.VoluntarioSolicitanteId,
            usuarioId);
        await _auditLogService.RecordAsync(
            "SolicitacaoTrocaEscala",
            updated.Id.ToString(),
            "Rejeitar",
            new { EscalaId = item.EscalaId, EscalaItemId = item.Id, item.EquipeId, updated.VoluntarioSolicitanteId, UsuarioId = usuarioId });
        if (full != null)
        {
            await NotificarSolicitacaoRejeitadaAsync(full, usuarioId);
        }
        return MapToDto(full!);
    }

    private async Task ValidarGestaoEquipeAsync(int equipeId, int usuarioId, bool isAdmin)
    {
        if (isAdmin) return;
        var ehLider = await _equipeRepository.IsLiderUsuarioDaEquipeAsync(usuarioId, equipeId);
        if (!ehLider) throw new UnauthorizedAccessException("Você não tem permissão para gerenciar solicitações desta equipe.");
    }

    private async Task NotificarSolicitacaoCriadaAsync(SolicitacaoTrocaEscala solicitacao, int usuarioId)
    {
        var liderUsuarioId = solicitacao.EscalaItem?.Equipe?.LiderUsuarioId;
        if (!liderUsuarioId.HasValue || liderUsuarioId.Value == usuarioId) return;

        var dataEvento = solicitacao.EscalaItem?.Escala?.EventoOcorrencia?.DataHoraInicio.ToString("dd/MM/yyyy HH:mm") ?? "data a confirmar";
        await _notificacaoUsuarioService.CriarAsync(new CriarNotificacaoUsuarioDto
        {
            UsuarioId = liderUsuarioId.Value,
            Tipo = TipoNotificacaoUsuario.TrocaEscala,
            Titulo = "Nova solicitacao de troca",
            Mensagem = $"{solicitacao.VoluntarioSolicitante?.Pessoa?.Nome ?? "Um voluntario"} pediu troca para {solicitacao.EscalaItem?.Escala?.EventoOcorrencia?.Evento?.Titulo ?? "o evento"} em {dataEvento}.",
            Link = $"/voluntariado/escalas/ocorrencia/{solicitacao.EscalaItem?.Escala?.EventoOcorrenciaId}/equipe/{solicitacao.EscalaItem?.EquipeId}"
        });
    }

    private async Task NotificarSolicitacaoAprovadaAsync(SolicitacaoTrocaEscala solicitacao, Voluntario substituto, int usuarioId)
    {
        var notificacoes = new List<CriarNotificacaoUsuarioDto>();
        var dataEvento = solicitacao.EscalaItem?.Escala?.EventoOcorrencia?.DataHoraInicio.ToString("dd/MM/yyyy HH:mm") ?? "data a confirmar";
        var linkEscala = $"/minhas-escalas";

        var usuarioSolicitante = solicitacao.VoluntarioSolicitante?.PessoaId > 0
            ? await _usuarioRepository.GetByPessoaIdAsync(solicitacao.VoluntarioSolicitante.PessoaId)
            : null;
        if (usuarioSolicitante != null && usuarioSolicitante.Id != usuarioId)
        {
            notificacoes.Add(new CriarNotificacaoUsuarioDto
            {
                UsuarioId = usuarioSolicitante.Id,
                Tipo = TipoNotificacaoUsuario.TrocaEscala,
                Titulo = "Troca aprovada",
                Mensagem = $"Sua solicitacao de troca para {solicitacao.EscalaItem?.Escala?.EventoOcorrencia?.Evento?.Titulo ?? "o evento"} em {dataEvento} foi aprovada.",
                Link = linkEscala
            });
        }

        var usuarioSubstituto = substituto.PessoaId > 0 ? await _usuarioRepository.GetByPessoaIdAsync(substituto.PessoaId) : null;
        if (usuarioSubstituto != null && usuarioSubstituto.Id != usuarioId)
        {
            notificacoes.Add(new CriarNotificacaoUsuarioDto
            {
                UsuarioId = usuarioSubstituto.Id,
                Tipo = TipoNotificacaoUsuario.TrocaEscala,
                Titulo = "Nova substituicao na escala",
                Mensagem = $"Voce foi definido como substituto em {solicitacao.EscalaItem?.Equipe?.Nome ?? "uma equipe"} para {solicitacao.EscalaItem?.Escala?.EventoOcorrencia?.Evento?.Titulo ?? "o evento"} em {dataEvento}.",
                Link = linkEscala
            });
        }

        await _notificacaoUsuarioService.CriarParaUsuariosAsync(notificacoes);
    }

    private async Task NotificarSolicitacaoRejeitadaAsync(SolicitacaoTrocaEscala solicitacao, int usuarioId)
    {
        var usuarioSolicitante = solicitacao.VoluntarioSolicitante?.PessoaId > 0
            ? await _usuarioRepository.GetByPessoaIdAsync(solicitacao.VoluntarioSolicitante.PessoaId)
            : null;
        if (usuarioSolicitante == null || usuarioSolicitante.Id == usuarioId) return;

        var dataEvento = solicitacao.EscalaItem?.Escala?.EventoOcorrencia?.DataHoraInicio.ToString("dd/MM/yyyy HH:mm") ?? "data a confirmar";
        await _notificacaoUsuarioService.CriarAsync(new CriarNotificacaoUsuarioDto
        {
            UsuarioId = usuarioSolicitante.Id,
            Tipo = TipoNotificacaoUsuario.TrocaEscala,
            Titulo = "Troca rejeitada",
            Mensagem = $"Sua solicitacao de troca para {solicitacao.EscalaItem?.Escala?.EventoOcorrencia?.Evento?.Titulo ?? "o evento"} em {dataEvento} foi rejeitada.",
            Link = "/minhas-escalas"
        });
    }

    private static SolicitacaoTrocaEscalaDto MapToDto(SolicitacaoTrocaEscala x)
    {
        return new SolicitacaoTrocaEscalaDto
        {
            Id = x.Id,
            EscalaItemId = x.EscalaItemId,
            EscalaId = x.EscalaItem?.EscalaId ?? 0,
            EventoOcorrenciaId = x.EscalaItem?.Escala?.EventoOcorrenciaId ?? 0,
            EventoTitulo = x.EscalaItem?.Escala?.EventoOcorrencia?.Evento?.Titulo ?? string.Empty,
            EventoDataHoraInicio = x.EscalaItem?.Escala?.EventoOcorrencia?.DataHoraInicio,
            EquipeId = x.EscalaItem?.EquipeId ?? 0,
            EquipeNome = x.EscalaItem?.Equipe?.Nome ?? string.Empty,
            VoluntarioSolicitanteId = x.VoluntarioSolicitanteId,
            VoluntarioSolicitanteNome = x.VoluntarioSolicitante?.Pessoa?.Nome ?? string.Empty,
            VoluntarioSubstitutoId = x.VoluntarioSubstitutoId,
            VoluntarioSubstitutoNome = x.VoluntarioSubstituto?.Pessoa?.Nome,
            Status = x.Status,
            Motivo = x.Motivo,
            ObservacaoResposta = x.ObservacaoResposta,
            RespondidoPorUsuarioId = x.RespondidoPorUsuarioId,
            RespondidoPorUsuarioNome = x.RespondidoPorUsuario?.Pessoa?.Nome,
            DataSolicitacao = x.DataSolicitacao,
            DataResposta = x.DataResposta
        };
    }
}
