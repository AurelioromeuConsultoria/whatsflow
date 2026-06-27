using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.DTOs.MensagensAgendadas;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WhatsFlow.Application.Services;

public interface IMensagemAgendadaService
{
    Task<IEnumerable<MensagemAgendadaDto>> GetAllAsync();
    Task<PagedResultDto<MensagemAgendadaDto>> GetPagedAsync(MensagemAgendadaPagedQueryDto query);
    Task<MensagemAgendadaStatsDto> GetStatsAsync();
    Task<MensagemAgendadaDto?> GetByIdAsync(int id);
    Task<IEnumerable<MensagemAgendadaDto>> GetMensagensProntasParaEnvioAsync();
    /// <summary>Reserva transacionalmente mensagens prontas (status → EmProcessamento). Apenas as reservadas devem ser processadas.</summary>
    Task<IEnumerable<MensagemAgendadaDto>> ReservarProntasParaEnvioAsync(int limit);
    Task<IEnumerable<MensagemAgendadaDto>> GetMensagensPorContatoAsync(int contatoId);
    Task AgendarMensagensParaContatoAsync(int contatoId);
    Task<RegerarMensagensResultDto> RegerarMensagensParaContatoAsync(int contatoId);
    Task MarcarComoProntaParaEnvioAsync(int mensagemId);
    Task MarcarComoEnviadaAsync(int mensagemId);
    Task MarcarComoErroAsync(int mensagemId, string erro);
}

public class MensagemAgendadaService : IMensagemAgendadaService
{
    private readonly IMensagemAgendadaRepository _mensagemRepository;
    private readonly IContatoRepository _contatoRepository;
    private readonly IConfiguracaoMensagemRepository _configuracaoRepository;
    private readonly ILogger<MensagemAgendadaService> _logger;
    private readonly IAuditLogService _auditLogService;

    public MensagemAgendadaService(
        IMensagemAgendadaRepository mensagemRepository,
        IContatoRepository contatoRepository,
        IConfiguracaoMensagemRepository configuracaoRepository,
        ILogger<MensagemAgendadaService> logger,
        IAuditLogService auditLogService)
    {
        _mensagemRepository = mensagemRepository;
        _contatoRepository = contatoRepository;
        _configuracaoRepository = configuracaoRepository;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<IEnumerable<MensagemAgendadaDto>> GetAllAsync()
    {
        var mensagens = await _mensagemRepository.GetAllAsync();
        return mensagens.Select(MapToDto);
    }

    public async Task<PagedResultDto<MensagemAgendadaDto>> GetPagedAsync(MensagemAgendadaPagedQueryDto queryDto)
    {
        var page = queryDto.Page <= 0 ? 1 : queryDto.Page;
        var pageSize = queryDto.PageSize <= 0 ? 20 : Math.Min(queryDto.PageSize, 200);

        StatusMensagem? status = null;
        if (queryDto.Status.HasValue && Enum.IsDefined(typeof(StatusMensagem), queryDto.Status.Value))
        {
            status = (StatusMensagem)queryDto.Status.Value;
        }

        var query = new MensagemAgendadaPagedQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = queryDto.Sort,
            Direction = queryDto.Direction,
            Texto = queryDto.Texto,
            ContatoId = queryDto.ContatoId,
            Status = status,
            DataEnvioFrom = queryDto.DataEnvioFrom,
            DataEnvioTo = queryDto.DataEnvioTo
        };

        var (items, total) = await _mensagemRepository.GetPagedAsync(query);
        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<MensagemAgendadaDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<MensagemAgendadaStatsDto> GetStatsAsync()
    {
        return _mensagemRepository.GetStatsAsync();
    }

    public async Task<MensagemAgendadaDto?> GetByIdAsync(int id)
    {
        var mensagem = await _mensagemRepository.GetByIdAsync(id);
        return mensagem != null ? MapToDto(mensagem) : null;
    }

    public async Task<IEnumerable<MensagemAgendadaDto>> GetMensagensProntasParaEnvioAsync()
    {
        var mensagens = await _mensagemRepository.GetMensagensProntasParaEnvioAsync();
        return mensagens.Select(MapToDto);
    }

    public async Task<IEnumerable<MensagemAgendadaDto>> ReservarProntasParaEnvioAsync(int limit)
    {
        var mensagens = await _mensagemRepository.ReservarProntasParaEnvioAsync(limit);
        _logger.LogInformation(
            "Mensagens reservadas para envio. Quantidade={Quantidade} Limite={Limite}",
            mensagens.Count(),
            limit);
        return mensagens.Select(MapToDto);
    }

    public async Task<IEnumerable<MensagemAgendadaDto>> GetMensagensPorContatoAsync(int contatoId)
    {
        var mensagens = await _mensagemRepository.GetMensagensPorContatoAsync(contatoId);
        return mensagens.Select(MapToDto);
    }

    public async Task AgendarMensagensParaContatoAsync(int contatoId)
    {
        var contato = await _contatoRepository.GetByIdAsync(contatoId);
        if (contato == null)
            throw new ArgumentException("Contato não encontrado");

        var configuracoes = await _configuracaoRepository.GetAtivasAsync();
        var totalCriadas = 0;

        foreach (var configuracao in configuracoes)
        {
            // TODO(WhatsFlow Etapa 4C): a base de agendamento era a data da visita;
            // para Contato usamos a data de criação como aproximação.
            var dataEnvio = contato.CriadoEm.AddDays(configuracao.DiasAposVisita);
            var dataEnvioCompleta = dataEnvio.Date + configuracao.HorarioEnvio;

            var textoFinal = configuracao.TextoMensagem.Replace("{Nome}", contato.Nome ?? "");

            var mensagemAgendada = new MensagemAgendada
            {
                ContatoId = contato.Id,
                ConfiguracaoMensagemId = configuracao.Id,
                DataAgendamento = DateTime.Now,
                DataEnvio = dataEnvioCompleta,
                Status = StatusMensagem.Agendada,
                TextoFinal = textoFinal,
                DataCriacao = DateTime.Now
            };

            await _mensagemRepository.CreateAsync(mensagemAgendada);
            totalCriadas++;
        }

        _logger.LogInformation(
            "Mensagens agendadas para contato. ContatoId={ContatoId} Quantidade={Quantidade}",
            contatoId,
            totalCriadas);
    }

    public async Task<RegerarMensagensResultDto> RegerarMensagensParaContatoAsync(int contatoId)
    {
        var contato = await _contatoRepository.GetByIdAsync(contatoId);
        if (contato == null)
            throw new ArgumentException("Contato não encontrado");

        var canceladas = await _mensagemRepository.CancelarPendentesPorContatoAsync(
            contatoId,
            $"Cancelada por regeneração em {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        var configuracoes = await _configuracaoRepository.GetAtivasAsync();
        var criadas = 0;

        foreach (var configuracao in configuracoes)
        {
            var dataEnvio = contato.CriadoEm.AddDays(configuracao.DiasAposVisita);
            var dataEnvioCompleta = dataEnvio.Date + configuracao.HorarioEnvio;

            var textoFinal = configuracao.TextoMensagem.Replace("{Nome}", contato.Nome ?? "");

            var mensagemAgendada = new MensagemAgendada
            {
                ContatoId = contato.Id,
                ConfiguracaoMensagemId = configuracao.Id,
                DataAgendamento = DateTime.Now,
                DataEnvio = dataEnvioCompleta,
                Status = StatusMensagem.Agendada,
                TextoFinal = textoFinal,
                DataCriacao = DateTime.Now
            };

            await _mensagemRepository.CreateAsync(mensagemAgendada);
            criadas++;
        }

        _logger.LogInformation(
            "Mensagens regeneradas para contato. ContatoId={ContatoId} Canceladas={Canceladas} Criadas={Criadas}",
            contatoId,
            canceladas,
            criadas);
        await _auditLogService.RecordAsync(
            "MensagemAgendada",
            contatoId.ToString(),
            "Regerar",
            new { ContatoId = contatoId, Canceladas = canceladas, Criadas = criadas });

        return new RegerarMensagensResultDto
        {
            MensagensCanceladas = canceladas,
            MensagensCriadas = criadas
        };
    }

    public async Task MarcarComoProntaParaEnvioAsync(int mensagemId)
    {
        var mensagem = await _mensagemRepository.GetByIdAsync(mensagemId);
        if (mensagem == null)
            throw new ArgumentException("Mensagem não encontrada");

        mensagem.Status = StatusMensagem.ProntaParaEnvio;
        mensagem.DataProcessamento = DateTime.Now;

        await _mensagemRepository.UpdateAsync(mensagem);
        _logger.LogInformation(
            "Mensagem marcada como pronta para envio. MensagemId={MensagemId} ContatoId={ContatoId}",
            mensagem.Id,
            mensagem.ContatoId);
        await _auditLogService.RecordAsync(
            "MensagemAgendada",
            mensagem.Id.ToString(),
            "ProntaParaEnvio",
            new { mensagem.ContatoId });
    }

    public async Task MarcarComoEnviadaAsync(int mensagemId)
    {
        var mensagem = await _mensagemRepository.GetByIdAsync(mensagemId);
        if (mensagem == null)
            throw new ArgumentException("Mensagem não encontrada");

        mensagem.Status = StatusMensagem.Enviada;
        mensagem.DataProcessamento = DateTime.Now;

        await _mensagemRepository.UpdateAsync(mensagem);
        _logger.LogInformation(
            "Mensagem marcada como enviada. MensagemId={MensagemId} ContatoId={ContatoId}",
            mensagem.Id,
            mensagem.ContatoId);
        await _auditLogService.RecordAsync(
            "MensagemAgendada",
            mensagem.Id.ToString(),
            "Enviada",
            new { mensagem.ContatoId });
    }

    public async Task MarcarComoErroAsync(int mensagemId, string erro)
    {
        var mensagem = await _mensagemRepository.GetByIdAsync(mensagemId);
        if (mensagem == null)
            throw new ArgumentException("Mensagem não encontrada");

        mensagem.Status = StatusMensagem.Erro;
        mensagem.LogErro = erro;
        mensagem.DataProcessamento = DateTime.Now;

        await _mensagemRepository.UpdateAsync(mensagem);
        _logger.LogWarning(
            "Mensagem marcada como erro. MensagemId={MensagemId} ContatoId={ContatoId}",
            mensagem.Id,
            mensagem.ContatoId);
        await _auditLogService.RecordAsync(
            "MensagemAgendada",
            mensagem.Id.ToString(),
            "ErroEnvio",
            new { mensagem.ContatoId });
    }

    private static MensagemAgendadaDto MapToDto(MensagemAgendada mensagem)
    {
        var telefone = mensagem.Contato?.TelefoneWhatsApp ?? "";

        return new MensagemAgendadaDto
        {
            Id = mensagem.Id,
            ContatoId = mensagem.ContatoId,
            NomeContato = mensagem.Contato?.Nome ?? "",
            TelefoneContato = telefone,
            ConfiguracaoMensagemId = mensagem.ConfiguracaoMensagemId,
            NomeConfiguracao = mensagem.ConfiguracaoMensagem?.Nome ?? "",
            DataAgendamento = mensagem.DataAgendamento,
            DataEnvio = mensagem.DataEnvio,
            Status = mensagem.Status,
            TextoFinal = mensagem.TextoFinal,
            DataProcessamento = mensagem.DataProcessamento,
            LogErro = mensagem.LogErro,
            DataCriacao = mensagem.DataCriacao
        };
    }
}
