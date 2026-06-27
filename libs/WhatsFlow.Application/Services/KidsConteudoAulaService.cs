using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IKidsConteudoAulaService
{
    Task<IEnumerable<KidsConteudoAulaAdminDto>> GetAsync(string? status = null, string? salaId = null, string? turmaId = null, DateTime? dataReferencia = null, int? limit = null);
    Task<KidsConteudoAulaAdminDto?> GetByIdAsync(int id);
    Task<IEnumerable<MeuConteudoAulaDto>> GetMeuConteudoPorCriancaAsync(int criancaPessoaId, int? limit = null);
    Task<KidsConteudoAulaAdminDto> CreateAsync(CreateKidsConteudoAulaRequest request);
    Task<KidsConteudoAulaAdminDto> UpdateAsync(int id, UpdateKidsConteudoAulaRequest request);
    Task<KidsConteudoAulaAdminDto> PublicarAsync(int id);
    Task<KidsConteudoAulaAdminDto> ArquivarAsync(int id);
}

public class KidsConteudoAulaService : IKidsConteudoAulaService
{
    private static readonly HashSet<string> TiposAnexoPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pdf",
        "Imagem",
        "Link"
    };

    private readonly IKidsConteudoAulaRepository _conteudoRepository;
    private readonly IKidsConteudoAulaAnexoRepository _anexoRepository;
    private readonly IKidsEstruturaRepository _estruturaRepository;
    private readonly IEventoOcorrenciaRepository _eventoOcorrenciaRepository;
    private readonly IKidsAuthorizationService _authorizationService;
    private readonly IResponsavelCriancaRepository _responsavelCriancaRepository;
    private readonly ICriancaDetalheRepository _criancaDetalheRepository;
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public KidsConteudoAulaService(
        IKidsConteudoAulaRepository conteudoRepository,
        IKidsConteudoAulaAnexoRepository anexoRepository,
        IKidsEstruturaRepository estruturaRepository,
        IEventoOcorrenciaRepository eventoOcorrenciaRepository,
        IKidsAuthorizationService authorizationService,
        IResponsavelCriancaRepository responsavelCriancaRepository,
        ICriancaDetalheRepository criancaDetalheRepository,
        IPessoaRepository pessoaRepository,
        IUnitOfWork unitOfWork)
    {
        _conteudoRepository = conteudoRepository;
        _anexoRepository = anexoRepository;
        _estruturaRepository = estruturaRepository;
        _eventoOcorrenciaRepository = eventoOcorrenciaRepository;
        _authorizationService = authorizationService;
        _responsavelCriancaRepository = responsavelCriancaRepository;
        _criancaDetalheRepository = criancaDetalheRepository;
        _pessoaRepository = pessoaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<KidsConteudoAulaAdminDto>> GetAsync(string? status = null, string? salaId = null, string? turmaId = null, DateTime? dataReferencia = null, int? limit = null)
    {
        await _authorizationService.EnsureOperadorAsync();
        var items = await _conteudoRepository.GetAllAsync(NormalizeStatusOrNull(status), NormalizeIdOrNull(salaId), NormalizeIdOrNull(turmaId), dataReferencia, limit);
        return items.Select(MapAdminDto).ToList();
    }

    public async Task<KidsConteudoAulaAdminDto?> GetByIdAsync(int id)
    {
        await _authorizationService.EnsureOperadorAsync();
        var item = await _conteudoRepository.GetByIdAsync(id);
        return item == null ? null : MapAdminDto(item);
    }

    public async Task<IEnumerable<MeuConteudoAulaDto>> GetMeuConteudoPorCriancaAsync(int criancaPessoaId, int? limit = null)
    {
        var context = await _authorizationService.GetCurrentContextAsync();
        var vinculoAtivo = await _responsavelCriancaRepository.ExisteVinculoAtivoAsync(criancaPessoaId, context.PessoaId);
        if (!vinculoAtivo)
        {
            throw new UnauthorizedAccessException("A criança informada não está vinculada ao responsável atual.");
        }

        var detalhe = await _criancaDetalheRepository.GetByPessoaIdAsync(criancaPessoaId)
            ?? throw new ArgumentException("Detalhe da criança não encontrado.");
        var crianca = await _pessoaRepository.GetByIdAsync(criancaPessoaId)
            ?? throw new ArgumentException("Criança não encontrada.");

        var published = await _conteudoRepository.GetAllAsync("Published", limit: limit ?? 20);
        var filtrados = published
            .Where(item => MatchesEstrutura(item, detalhe))
            .OrderByDescending(item => item.DataReferencia)
            .ThenByDescending(item => item.PublicadoEm ?? item.CriadoEm)
            .Take(limit ?? 20)
            .Select(item => MapMeuConteudoDto(item, crianca))
            .ToList();

        return filtrados;
    }

    public async Task<KidsConteudoAulaAdminDto> CreateAsync(CreateKidsConteudoAulaRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var context = await _authorizationService.GetCurrentContextAsync();
        var normalizedSalaId = NormalizeIdOrNull(request.SalaId);
        var normalizedTurmaId = NormalizeIdOrNull(request.TurmaId);

        await ValidateEstruturaAsync(normalizedSalaId, normalizedTurmaId);
        await ValidateEventoAsync(request.EventoOcorrenciaId);
        ValidateAnexos(request.Anexos);

        var created = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteudo = new KidsConteudoAula
            {
                Titulo = request.Titulo.Trim(),
                Tema = CleanText(request.Tema),
                Versiculo = CleanText(request.Versiculo),
                Resumo = request.Resumo.Trim(),
                AtividadeEmCasa = CleanText(request.AtividadeEmCasa),
                ObservacaoResponsavel = CleanText(request.ObservacaoResponsavel),
                Status = "Draft",
                DataReferencia = request.DataReferencia,
                EventoOcorrenciaId = request.EventoOcorrenciaId,
                SalaId = normalizedSalaId,
                TurmaId = normalizedTurmaId,
                CriadoEm = DateTime.UtcNow
            };

            await _conteudoRepository.CreateWithoutSaveAsync(conteudo);
            await _unitOfWork.SaveChangesAsync();

            var anexos = BuildAnexos(conteudo.Id, request.Anexos);
            if (anexos.Count > 0)
            {
                await _anexoRepository.CreateRangeWithoutSaveAsync(anexos);
                await _unitOfWork.SaveChangesAsync();
            }

            var persisted = await _conteudoRepository.GetByIdAsync(conteudo.Id)
                ?? throw new InvalidOperationException("Falha ao recarregar o conteúdo da aula criado.");

            return persisted;
        });

        return MapAdminDto(created);
    }

    public async Task<KidsConteudoAulaAdminDto> UpdateAsync(int id, UpdateKidsConteudoAulaRequest request)
    {
        await _authorizationService.EnsureLiderAsync();
        var normalizedSalaId = NormalizeIdOrNull(request.SalaId);
        var normalizedTurmaId = NormalizeIdOrNull(request.TurmaId);

        await ValidateEstruturaAsync(normalizedSalaId, normalizedTurmaId);
        await ValidateEventoAsync(request.EventoOcorrenciaId);
        ValidateAnexos(request.Anexos);

        var updated = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteudo = await _conteudoRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("Conteúdo da aula não encontrado.");

            conteudo.Titulo = request.Titulo.Trim();
            conteudo.Tema = CleanText(request.Tema);
            conteudo.Versiculo = CleanText(request.Versiculo);
            conteudo.Resumo = request.Resumo.Trim();
            conteudo.AtividadeEmCasa = CleanText(request.AtividadeEmCasa);
            conteudo.ObservacaoResponsavel = CleanText(request.ObservacaoResponsavel);
            conteudo.DataReferencia = request.DataReferencia;
            conteudo.EventoOcorrenciaId = request.EventoOcorrenciaId;
            conteudo.SalaId = normalizedSalaId;
            conteudo.TurmaId = normalizedTurmaId;
            conteudo.AtualizadoEm = DateTime.UtcNow;

            if (conteudo.Status == "Archived")
            {
                conteudo.Status = "Draft";
            }

            await _conteudoRepository.UpdateWithoutSaveAsync(conteudo);
            await _anexoRepository.DeleteByConteudoAulaIdWithoutSaveAsync(conteudo.Id);
            await _unitOfWork.SaveChangesAsync();

            var anexos = BuildAnexos(conteudo.Id, request.Anexos);
            if (anexos.Count > 0)
            {
                await _anexoRepository.CreateRangeWithoutSaveAsync(anexos);
                await _unitOfWork.SaveChangesAsync();
            }

            var persisted = await _conteudoRepository.GetByIdAsync(conteudo.Id)
                ?? throw new InvalidOperationException("Falha ao recarregar o conteúdo da aula atualizado.");

            return persisted;
        });

        return MapAdminDto(updated);
    }

    public async Task<KidsConteudoAulaAdminDto> PublicarAsync(int id)
    {
        await _authorizationService.EnsureLiderAsync();
        var context = await _authorizationService.GetCurrentContextAsync();

        var item = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteudo = await _conteudoRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("Conteúdo da aula não encontrado.");

            if (string.IsNullOrWhiteSpace(conteudo.Resumo) || string.IsNullOrWhiteSpace(conteudo.Titulo))
            {
                throw new InvalidOperationException("O conteúdo precisa de título e resumo antes de ser publicado.");
            }

            conteudo.Status = "Published";
            conteudo.PublicadoEm = DateTime.UtcNow;
            conteudo.PublicadoPorPessoaId = context.PessoaId;
            conteudo.AtualizadoEm = DateTime.UtcNow;

            await _conteudoRepository.UpdateWithoutSaveAsync(conteudo);
            await _unitOfWork.SaveChangesAsync();

            return await _conteudoRepository.GetByIdAsync(conteudo.Id)
                ?? throw new InvalidOperationException("Falha ao recarregar o conteúdo da aula publicado.");
        });

        return MapAdminDto(item);
    }

    public async Task<KidsConteudoAulaAdminDto> ArquivarAsync(int id)
    {
        await _authorizationService.EnsureLiderAsync();

        var item = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var conteudo = await _conteudoRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("Conteúdo da aula não encontrado.");

            conteudo.Status = "Archived";
            conteudo.AtualizadoEm = DateTime.UtcNow;

            await _conteudoRepository.UpdateWithoutSaveAsync(conteudo);
            await _unitOfWork.SaveChangesAsync();

            return await _conteudoRepository.GetByIdAsync(conteudo.Id)
                ?? throw new InvalidOperationException("Falha ao recarregar o conteúdo da aula arquivado.");
        });

        return MapAdminDto(item);
    }

    private async Task ValidateEstruturaAsync(string? salaId, string? turmaId)
    {
        if (string.IsNullOrWhiteSpace(salaId) && !string.IsNullOrWhiteSpace(turmaId))
        {
            throw new ArgumentException("Turma não pode ser informada sem sala.");
        }

        KidsSala? sala = null;
        if (!string.IsNullOrWhiteSpace(salaId))
        {
            sala = await _estruturaRepository.GetSalaByIdAsync(salaId);
            if (sala == null || !sala.Ativo)
            {
                throw new ArgumentException("Sala não encontrada ou inativa.");
            }
        }

        if (!string.IsNullOrWhiteSpace(turmaId))
        {
            var turma = await _estruturaRepository.GetTurmaByIdAsync(turmaId);
            if (turma == null || !turma.Ativo)
            {
                throw new ArgumentException("Turma não encontrada ou inativa.");
            }

            if (!string.Equals(turma.SalaId, salaId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A turma informada não pertence à sala selecionada.");
            }
        }
    }

    private async Task ValidateEventoAsync(int? eventoOcorrenciaId)
    {
        if (!eventoOcorrenciaId.HasValue)
        {
            return;
        }

        if (!await _eventoOcorrenciaRepository.ExistsAsync(eventoOcorrenciaId.Value))
        {
            throw new ArgumentException("Ocorrência de evento não encontrada.");
        }
    }

    private static void ValidateAnexos(IEnumerable<CreateKidsConteudoAulaAnexoRequest> anexos)
    {
        foreach (var anexo in anexos)
        {
            if (!TiposAnexoPermitidos.Contains(anexo.Tipo.Trim()))
            {
                throw new ArgumentException($"Tipo de anexo inválido: {anexo.Tipo}.");
            }

            if (string.Equals(anexo.Tipo.Trim(), "Link", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(anexo.Url))
                {
                    throw new ArgumentException("Anexos do tipo Link exigem URL.");
                }
            }
            else if (string.IsNullOrWhiteSpace(anexo.Url) && string.IsNullOrWhiteSpace(anexo.StoragePath))
            {
                throw new ArgumentException("Anexos de arquivo exigem URL ou caminho de storage.");
            }
        }
    }

    private static List<KidsConteudoAulaAnexo> BuildAnexos(int conteudoAulaId, IEnumerable<CreateKidsConteudoAulaAnexoRequest> anexos)
    {
        return anexos
            .OrderBy(x => x.Ordem)
            .Select(anexo => new KidsConteudoAulaAnexo
            {
                ConteudoAulaId = conteudoAulaId,
                Tipo = NormalizeTipoAnexo(anexo.Tipo),
                NomeExibicao = anexo.NomeExibicao.Trim(),
                Url = CleanText(anexo.Url),
                StoragePath = CleanText(anexo.StoragePath),
                MimeType = CleanText(anexo.MimeType),
                TamanhoBytes = anexo.TamanhoBytes,
                Ordem = anexo.Ordem,
                CriadoEm = DateTime.UtcNow
            })
            .ToList();
    }

    private static KidsConteudoAulaAdminDto MapAdminDto(KidsConteudoAula item)
    {
        return new KidsConteudoAulaAdminDto
        {
            Id = item.Id,
            Titulo = item.Titulo,
            Tema = item.Tema,
            Versiculo = item.Versiculo,
            Resumo = item.Resumo,
            AtividadeEmCasa = item.AtividadeEmCasa,
            ObservacaoResponsavel = item.ObservacaoResponsavel,
            Status = item.Status,
            DataReferencia = item.DataReferencia,
            EventoOcorrenciaId = item.EventoOcorrenciaId,
            EventoDataHoraInicio = item.EventoOcorrencia?.DataHoraInicio,
            SalaId = item.SalaId,
            TurmaId = item.TurmaId,
            PublicadoEm = item.PublicadoEm,
            PublicadoPorPessoaId = item.PublicadoPorPessoaId,
            PublicadoPorNome = item.PublicadoPor?.Nome,
            CriadoEm = item.CriadoEm,
            AtualizadoEm = item.AtualizadoEm,
            Anexos = item.Anexos
                .OrderBy(x => x.Ordem)
                .Select(anexo => new KidsConteudoAulaAnexoDto
                {
                    Id = anexo.Id,
                    Tipo = anexo.Tipo,
                    NomeExibicao = anexo.NomeExibicao,
                    Url = anexo.Url,
                    StoragePath = anexo.StoragePath,
                    MimeType = anexo.MimeType,
                    TamanhoBytes = anexo.TamanhoBytes,
                    Ordem = anexo.Ordem
                })
                .ToList()
        };
    }

    private static MeuConteudoAulaDto MapMeuConteudoDto(KidsConteudoAula item, Pessoa crianca)
    {
        return new MeuConteudoAulaDto
        {
            Id = item.Id,
            CriancaPessoaId = crianca.Id,
            CriancaNome = crianca.Nome,
            Titulo = item.Titulo,
            Tema = item.Tema,
            Versiculo = item.Versiculo,
            Resumo = item.Resumo,
            AtividadeEmCasa = item.AtividadeEmCasa,
            ObservacaoResponsavel = item.ObservacaoResponsavel,
            DataReferencia = item.DataReferencia,
            SalaId = item.SalaId,
            TurmaId = item.TurmaId,
            PublicadoEm = item.PublicadoEm,
            Anexos = item.Anexos
                .OrderBy(x => x.Ordem)
                .Select(anexo => new KidsConteudoAulaAnexoDto
                {
                    Id = anexo.Id,
                    Tipo = anexo.Tipo,
                    NomeExibicao = anexo.NomeExibicao,
                    Url = anexo.Url,
                    StoragePath = anexo.StoragePath,
                    MimeType = anexo.MimeType,
                    TamanhoBytes = anexo.TamanhoBytes,
                    Ordem = anexo.Ordem
                })
                .ToList()
        };
    }

    private static bool MatchesEstrutura(KidsConteudoAula item, CriancaDetalhe detalhe)
    {
        if (string.IsNullOrWhiteSpace(item.SalaId) && string.IsNullOrWhiteSpace(item.TurmaId))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(item.TurmaId))
        {
            return string.Equals(item.TurmaId, detalhe.TurmaId, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(item.SalaId))
        {
            return string.Equals(item.SalaId, detalhe.SalaId, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string? NormalizeIdOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeStatusOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeTipoAnexo(string value)
    {
        var normalized = value.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "pdf" => "Pdf",
            "imagem" => "Imagem",
            "link" => "Link",
            _ => normalized
        };
    }

    private static string? CleanText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
