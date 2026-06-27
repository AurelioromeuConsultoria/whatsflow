using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IDespesaService
{
    Task<IEnumerable<DespesaDto>> GetAllAsync();
    Task<DespesaDto?> GetByIdAsync(int id);
    Task<DespesaDto> CreateAsync(CriarDespesaDto dto);
    Task<DespesaDto> UpdateAsync(int id, AtualizarDespesaDto dto);
    Task DeleteAsync(int id);
    Task<VencimentosResumoDto> GetVencimentosAsync();
    Task<DespesaDto> GerarProximaRecorrenciaAsync(int id);
}

public class DespesaService : IDespesaService
{
    private readonly IDespesaRepository _repository;

    public DespesaService(IDespesaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<DespesaDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<DespesaDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<DespesaDto> CreateAsync(CriarDespesaDto dto)
    {
        var entity = new Despesa
        {
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            DataVencimento = dto.DataVencimento,
            DataPagamento = dto.DataPagamento,
            Status = dto.Status,
            Observacoes = dto.Observacoes,
            ComprovanteUrl = dto.ComprovanteUrl,
            FornecedorId = dto.FornecedorId,
            CategoriaDespesaId = dto.CategoriaDespesaId,
            ContaBancariaId = dto.ContaBancariaId,
            CentroCustoId = dto.CentroCustoId,
            ProjetoId = dto.ProjetoId,
            UsuarioId = dto.UsuarioId,
            Recorrente = dto.Recorrente,
            TipoRecorrencia = dto.TipoRecorrencia,
            DataCriacao = DateTime.Now,
        };

        var created = await _repository.CreateAsync(entity);
        var withRelations = await _repository.GetByIdAsync(created.Id);
        return MapToDto(withRelations!);
    }

    public async Task<DespesaDto> UpdateAsync(int id, AtualizarDespesaDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new ArgumentException("Despesa não encontrada");

        entity.Descricao = dto.Descricao;
        entity.Valor = dto.Valor;
        entity.DataVencimento = dto.DataVencimento;
        entity.DataPagamento = dto.DataPagamento;
        entity.Status = dto.Status;
        entity.Observacoes = dto.Observacoes;
        entity.ComprovanteUrl = dto.ComprovanteUrl;
        entity.FornecedorId = dto.FornecedorId;
        entity.CategoriaDespesaId = dto.CategoriaDespesaId;
        entity.ContaBancariaId = dto.ContaBancariaId;
        entity.CentroCustoId = dto.CentroCustoId;
        entity.ProjetoId = dto.ProjetoId;
        entity.UsuarioId = dto.UsuarioId;
        entity.Recorrente = dto.Recorrente;
        entity.TipoRecorrencia = dto.TipoRecorrencia;

        var updated = await _repository.UpdateAsync(entity);
        var withRelations = await _repository.GetByIdAsync(updated.Id);
        return MapToDto(withRelations!);
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<VencimentosResumoDto> GetVencimentosAsync()
    {
        var hoje = DateTime.Today;
        var em30Dias = hoje.AddDays(30);

        var pendentes = await _repository.GetPendentesAteDataAsync(em30Dias);

        var vencidas = pendentes.Where(d => d.DataVencimento.Date < hoje).ToList();
        var venceHoje = pendentes.Where(d => d.DataVencimento.Date == hoje).ToList();
        var proximos7 = pendentes.Where(d => d.DataVencimento.Date > hoje && d.DataVencimento.Date <= hoje.AddDays(7)).ToList();
        var proximos30 = pendentes.Where(d => d.DataVencimento.Date > hoje.AddDays(7) && d.DataVencimento.Date <= em30Dias).ToList();

        return new VencimentosResumoDto
        {
            TotalVencido = vencidas.Sum(d => d.Valor),
            TotalHoje = venceHoje.Sum(d => d.Valor),
            TotalProximos7Dias = proximos7.Sum(d => d.Valor),
            TotalProximos30Dias = proximos30.Sum(d => d.Valor),
            Vencidas = vencidas.Select(MapToDto).ToList(),
            Hoje = venceHoje.Select(MapToDto).ToList(),
            Proximos7Dias = proximos7.Select(MapToDto).ToList(),
            Proximos30Dias = proximos30.Select(MapToDto).ToList(),
        };
    }

    public async Task<DespesaDto> GerarProximaRecorrenciaAsync(int id)
    {
        var original = await _repository.GetByIdAsync(id);
        if (original == null) throw new ArgumentException("Despesa não encontrada");
        if (!original.Recorrente || original.TipoRecorrencia == null)
            throw new InvalidOperationException("Esta despesa não está marcada como recorrente.");

        var novaData = CalcularProximaData(original.DataVencimento, original.TipoRecorrencia.Value);

        var nova = new Despesa
        {
            Descricao = original.Descricao,
            Valor = original.Valor,
            DataVencimento = novaData,
            Status = StatusDespesa.Pendente,
            Observacoes = original.Observacoes,
            FornecedorId = original.FornecedorId,
            CategoriaDespesaId = original.CategoriaDespesaId,
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

    private static DespesaDto MapToDto(Despesa d)
    {
        return new DespesaDto
        {
            Id = d.Id,
            Descricao = d.Descricao,
            Valor = d.Valor,
            DataVencimento = d.DataVencimento,
            DataPagamento = d.DataPagamento,
            Status = d.Status,
            StatusDescricao = GetStatusDescricao(d.Status),
            Observacoes = d.Observacoes,
            ComprovanteUrl = d.ComprovanteUrl,
            FornecedorId = d.FornecedorId,
            FornecedorNome = d.Fornecedor?.Nome,
            CategoriaDespesaId = d.CategoriaDespesaId,
            CategoriaDespesaNome = d.CategoriaDespesa?.Nome,
            ContaBancariaId = d.ContaBancariaId,
            ContaBancariaNome = d.ContaBancaria?.Nome,
            CentroCustoId = d.CentroCustoId,
            CentroCustoNome = d.CentroCusto?.Nome,
            ProjetoId = d.ProjetoId,
            ProjetoNome = d.Projeto?.Nome,
            UsuarioId = d.UsuarioId,
            UsuarioNome = d.Usuario?.Pessoa?.Nome,
            Recorrente = d.Recorrente,
            TipoRecorrencia = d.TipoRecorrencia,
            TipoRecorrenciaDescricao = d.TipoRecorrencia.HasValue ? GetRecorrenciaDescricao(d.TipoRecorrencia.Value) : null,
            RecorrenciaOriginalId = d.RecorrenciaOriginalId,
            DataCriacao = d.DataCriacao,
        };
    }

    private static string GetStatusDescricao(StatusDespesa status) => status switch
    {
        StatusDespesa.Pendente => "Pendente",
        StatusDespesa.Paga => "Paga",
        StatusDespesa.Cancelada => "Cancelada",
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
