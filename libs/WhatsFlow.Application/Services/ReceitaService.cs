using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IReceitaService
{
    Task<IEnumerable<ReceitaDto>> GetAllAsync(int? pessoaId = null);
    Task<ReceitaDto?> GetByIdAsync(int id);
    Task<ReceitaDto> CreateAsync(CriarReceitaDto dto);
    Task<ReceitaDto> UpdateAsync(int id, AtualizarReceitaDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<ReceitaDto>> LancarContribuicoesEmLoteAsync(LancarContribuicoesLoteDto dto);
    Task<RelatorioContribuicoesDto> GetRelatorioContribuicoesAsync(DateTime dataInicio, DateTime dataFim, int? categoriaId);
    Task<InformeContribuicoesDto> GetInformeAnualAsync(int pessoaId, int ano);
    Task<ReceitaDto> GerarProximaRecorrenciaAsync(int id);
}

public class ReceitaService : IReceitaService
{
    private readonly IReceitaRepository _repository;
    private readonly IPessoaRepository _pessoaRepository;

    public ReceitaService(IReceitaRepository repository, IPessoaRepository pessoaRepository)
    {
        _repository = repository;
        _pessoaRepository = pessoaRepository;
    }

    public async Task<IEnumerable<ReceitaDto>> GetAllAsync(int? pessoaId = null)
    {
        var items = pessoaId.HasValue
            ? await _repository.GetByPessoaIdAsync(pessoaId.Value)
            : await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<ReceitaDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<ReceitaDto> CreateAsync(CriarReceitaDto dto)
    {
        var entity = new Receita
        {
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            DataRecebimento = dto.DataRecebimento,
            DataConfirmacao = dto.DataConfirmacao,
            Status = dto.Status,
            Observacoes = dto.Observacoes,
            ComprovanteUrl = dto.ComprovanteUrl,
            CategoriaReceitaId = dto.CategoriaReceitaId,
            ContaBancariaId = dto.ContaBancariaId,
            CentroCustoId = dto.CentroCustoId,
            ProjetoId = dto.ProjetoId,
            UsuarioId = dto.UsuarioId,
            PessoaId = dto.PessoaId,
            Recorrente = dto.Recorrente,
            TipoRecorrencia = dto.TipoRecorrencia,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        // Recarregar com relacionamentos para o DTO
        var createdWithRelations = await _repository.GetByIdAsync(created.Id);
        return MapToDto(createdWithRelations!);
    }

    public async Task<ReceitaDto> UpdateAsync(int id, AtualizarReceitaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Receita não encontrada");

        entity.Descricao = dto.Descricao;
        entity.Valor = dto.Valor;
        entity.DataRecebimento = dto.DataRecebimento;
        entity.DataConfirmacao = dto.DataConfirmacao;
        entity.Status = dto.Status;
        entity.Observacoes = dto.Observacoes;
        entity.ComprovanteUrl = dto.ComprovanteUrl;
        entity.CategoriaReceitaId = dto.CategoriaReceitaId;
        entity.ContaBancariaId = dto.ContaBancariaId;
        entity.CentroCustoId = dto.CentroCustoId;
        entity.ProjetoId = dto.ProjetoId;
        entity.UsuarioId = dto.UsuarioId;
        entity.PessoaId = dto.PessoaId;
        entity.Recorrente = dto.Recorrente;
        entity.TipoRecorrencia = dto.TipoRecorrencia;

        var updated = await _repository.UpdateAsync(entity);
        // Recarregar com relacionamentos para o DTO
        var updatedWithRelations = await _repository.GetByIdAsync(updated.Id);
        return MapToDto(updatedWithRelations!);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ReceitaDto>> LancarContribuicoesEmLoteAsync(LancarContribuicoesLoteDto dto)
    {
        if (dto.Itens == null || dto.Itens.Count == 0)
            throw new ArgumentException("Nenhum item para lançar");

        var criadas = new List<ReceitaDto>();
        foreach (var item in dto.Itens)
        {
            var criar = new CriarReceitaDto
            {
                Descricao = item.Descricao ?? dto.DescricaoPadrao ?? "Contribuição",
                Valor = item.Valor,
                DataRecebimento = dto.Data,
                Status = StatusReceita.Recebida,
                CategoriaReceitaId = dto.CategoriaReceitaId,
                ContaBancariaId = dto.ContaBancariaId,
                CentroCustoId = dto.CentroCustoId,
                ProjetoId = dto.ProjetoId,
                PessoaId = item.PessoaId,
                Observacoes = item.Observacoes,
            };
            var criada = await CreateAsync(criar);
            criadas.Add(criada);
        }
        return criadas;
    }

    public async Task<RelatorioContribuicoesDto> GetRelatorioContribuicoesAsync(DateTime dataInicio, DateTime dataFim, int? categoriaId)
    {
        var receitas = (await _repository.GetContribuicoesNoPeriodoAsync(dataInicio, dataFim, categoriaId)).ToList();

        var porPessoa = receitas
            .GroupBy(r => r.PessoaId!.Value)
            .Select(g => new ContribuicaoMembroDto
            {
                PessoaId = g.Key,
                PessoaNome = g.First().Pessoa?.Nome ?? string.Empty,
                Total = g.Sum(r => r.Valor),
                QuantidadeLancamentos = g.Count(),
                UltimaContribuicao = g.Max(r => r.DataRecebimento),
                PorCategoria = g.GroupBy(r => r.CategoriaReceitaId).Select(gc => new ContribuicaoMembroCategoriaDto
                {
                    CategoriaId = gc.Key,
                    CategoriaNome = gc.First().CategoriaReceita?.Nome ?? "Sem categoria",
                    Total = gc.Sum(r => r.Valor),
                    Quantidade = gc.Count()
                }).ToList()
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        // Membros ativos sem contribuição no período
        var pessoasQueContribuiram = new HashSet<int>(porPessoa.Select(c => c.PessoaId));
        var todosMembros = (await _pessoaRepository.GetAllAsync()).Where(p => p.Ativo && p.TipoPessoa == TipoPessoa.Adulto).ToList();

        var semContribuicao = todosMembros
            .Where(p => !pessoasQueContribuiram.Contains(p.Id))
            .Select(p =>
            {
                // Buscar última contribuição conhecida fora do período não é eficiente aqui,
                // então retornamos null por ora (pode ser melhorado com cache)
                return new MembroSemContribuicaoDto
                {
                    PessoaId = p.Id,
                    PessoaNome = p.Nome,
                    UltimaContribuicaoConhecida = null
                };
            })
            .OrderBy(m => m.PessoaNome)
            .ToList();

        return new RelatorioContribuicoesDto
        {
            DataInicio = dataInicio,
            DataFim = dataFim,
            TotalGeral = receitas.Sum(r => r.Valor),
            TotalLancamentos = receitas.Count,
            TotalMembrosContribuiram = porPessoa.Count,
            TotalMembrosSemContribuicao = semContribuicao.Count,
            Contribuidores = porPessoa,
            SemContribuicao = semContribuicao
        };
    }

    public async Task<InformeContribuicoesDto> GetInformeAnualAsync(int pessoaId, int ano)
    {
        var receitas = (await _repository.GetInformeAnualAsync(pessoaId, ano)).ToList();
        var pessoa = await _pessoaRepository.GetByIdAsync(pessoaId)
            ?? throw new ArgumentException("Pessoa não encontrada");

        var mesesPtBr = new[] { "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
                                     "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" };

        return new InformeContribuicoesDto
        {
            PessoaId = pessoaId,
            PessoaNome = pessoa.Nome,
            PessoaEmail = pessoa.Email,
            Ano = ano,
            TotalAnual = receitas.Sum(r => r.Valor),
            DataEmissao = DateTime.Now,
            PorMes = Enumerable.Range(1, 12)
                .Select(mes =>
                {
                    var do_mes = receitas.Where(r => r.DataRecebimento.Month == mes).ToList();
                    return new InformeContribuicaoMesDto
                    {
                        Mes = mes,
                        MesNome = mesesPtBr[mes],
                        Total = do_mes.Sum(r => r.Valor),
                        Quantidade = do_mes.Count
                    };
                })
                .ToList(),
            PorCategoria = receitas
                .GroupBy(r => r.CategoriaReceitaId)
                .Select(g => new ContribuicaoMembroCategoriaDto
                {
                    CategoriaId = g.Key,
                    CategoriaNome = g.First().CategoriaReceita?.Nome ?? "Sem categoria",
                    Total = g.Sum(r => r.Valor),
                    Quantidade = g.Count()
                })
                .ToList()
        };
    }

    private static ReceitaDto MapToDto(Receita r)
    {
        return new ReceitaDto
        {
            Id = r.Id,
            Descricao = r.Descricao,
            Valor = r.Valor,
            DataRecebimento = r.DataRecebimento,
            DataConfirmacao = r.DataConfirmacao,
            Status = r.Status,
            StatusDescricao = GetStatusDescricao(r.Status),
            Observacoes = r.Observacoes,
            ComprovanteUrl = r.ComprovanteUrl,
            CategoriaReceitaId = r.CategoriaReceitaId,
            CategoriaReceitaNome = r.CategoriaReceita?.Nome,
            ContaBancariaId = r.ContaBancariaId,
            ContaBancariaNome = r.ContaBancaria?.Nome,
            CentroCustoId = r.CentroCustoId,
            CentroCustoNome = r.CentroCusto?.Nome,
            ProjetoId = r.ProjetoId,
            ProjetoNome = r.Projeto?.Nome,
            UsuarioId = r.UsuarioId,
            UsuarioNome = r.Usuario?.Pessoa?.Nome,
            PessoaId = r.PessoaId,
            PessoaNome = r.Pessoa?.Nome,
            Recorrente = r.Recorrente,
            TipoRecorrencia = r.TipoRecorrencia,
            TipoRecorrenciaDescricao = r.TipoRecorrencia.HasValue ? GetRecorrenciaDescricao(r.TipoRecorrencia.Value) : null,
            RecorrenciaOriginalId = r.RecorrenciaOriginalId,
            DataCriacao = r.DataCriacao,
        };
    }

    public async Task<ReceitaDto> GerarProximaRecorrenciaAsync(int id)
    {
        var original = await _repository.GetByIdAsync(id);
        if (original == null) throw new ArgumentException("Receita não encontrada");
        if (!original.Recorrente || original.TipoRecorrencia == null)
            throw new InvalidOperationException("Esta receita não está marcada como recorrente.");

        var novaData = CalcularProximaData(original.DataRecebimento, original.TipoRecorrencia.Value);

        var nova = new Receita
        {
            Descricao = original.Descricao,
            Valor = original.Valor,
            DataRecebimento = novaData,
            Status = StatusReceita.Pendente,
            Observacoes = original.Observacoes,
            CategoriaReceitaId = original.CategoriaReceitaId,
            ContaBancariaId = original.ContaBancariaId,
            CentroCustoId = original.CentroCustoId,
            ProjetoId = original.ProjetoId,
            UsuarioId = original.UsuarioId,
            Recorrente = true,
            TipoRecorrencia = original.TipoRecorrencia,
            RecorrenciaOriginalId = original.RecorrenciaOriginalId ?? original.Id,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(nova);
        var withRelations = await _repository.GetByIdAsync(created.Id);
        return MapToDto(withRelations!);
    }

    private static DateTime CalcularProximaData(DateTime data, TipoRecorrencia tipo) => tipo switch
    {
        TipoRecorrencia.Semanal => data.AddDays(7),
        TipoRecorrencia.Quinzenal => data.AddDays(15),
        TipoRecorrencia.Mensal => data.AddMonths(1),
        TipoRecorrencia.Bimestral => data.AddMonths(2),
        TipoRecorrencia.Trimestral => data.AddMonths(3),
        TipoRecorrencia.Semestral => data.AddMonths(6),
        TipoRecorrencia.Anual => data.AddYears(1),
        _ => data.AddMonths(1)
    };

    private static string GetStatusDescricao(StatusReceita status) => status switch
    {
        StatusReceita.Pendente => "Pendente",
        StatusReceita.Recebida => "Recebida",
        StatusReceita.Cancelada => "Cancelada",
        _ => status.ToString()
    };

    private static string GetRecorrenciaDescricao(TipoRecorrencia tipo) => tipo switch
    {
        TipoRecorrencia.Semanal => "Semanal",
        TipoRecorrencia.Quinzenal => "Quinzenal",
        TipoRecorrencia.Mensal => "Mensal",
        TipoRecorrencia.Bimestral => "Bimestral",
        TipoRecorrencia.Trimestral => "Trimestral",
        TipoRecorrencia.Semestral => "Semestral",
        TipoRecorrencia.Anual => "Anual",
        _ => tipo.ToString()
    };
}
