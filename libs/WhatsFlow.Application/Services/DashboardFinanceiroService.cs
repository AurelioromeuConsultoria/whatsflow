using WhatsFlow.Application.DTOs;
using WhatsFlow.Application.Interfaces;
using WhatsFlow.Domain.Entities;

namespace WhatsFlow.Application.Services;

public interface IDashboardFinanceiroService
{
    Task<DashboardFinanceiroDto> GetDashboardAsync();
}

public class DashboardFinanceiroService : IDashboardFinanceiroService
{
    private readonly IFinanceiroQueryService _queryService;

    public DashboardFinanceiroService(IFinanceiroQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<DashboardFinanceiroDto> GetDashboardAsync()
    {
        var hoje = DateTime.Now;
        var primeiroDiaMes = new DateTime(hoje.Year, hoje.Month, 1);
        var ultimoDiaMes = primeiroDiaMes.AddMonths(1).AddDays(-1);
        var primeiroDiaAno = new DateTime(hoje.Year, 1, 1);
        var ultimoDiaAno = new DateTime(hoje.Year, 12, 31);

        // Totais do mês
        var totalReceitasMes = await _queryService.GetTotalReceitasAsync(primeiroDiaMes, ultimoDiaMes, StatusReceita.Recebida);
        var totalDespesasMes = await _queryService.GetTotalDespesasAsync(primeiroDiaMes, ultimoDiaMes, StatusDespesa.Paga);
        var saldoMes = totalReceitasMes - totalDespesasMes;

        // Totais do ano
        var totalReceitasAno = await _queryService.GetTotalReceitasAsync(primeiroDiaAno, ultimoDiaAno, StatusReceita.Recebida);
        var totalDespesasAno = await _queryService.GetTotalDespesasAsync(primeiroDiaAno, ultimoDiaAno, StatusDespesa.Paga);
        var saldoAno = totalReceitasAno - totalDespesasAno;

        // Fluxo de caixa mensal (últimos 12 meses)
        var fluxoCaixaMensal = await _queryService.GetFluxoCaixaMensalAsync(12);

        // Receitas e despesas por categoria (mês atual)
        var receitasPorCategoria = await _queryService.GetReceitasPorCategoriaAsync(primeiroDiaMes, ultimoDiaMes);
        var despesasPorCategoria = await _queryService.GetDespesasPorCategoriaAsync(primeiroDiaMes, ultimoDiaMes);

        // Últimas movimentações
        var ultimasMovimentacoes = await _queryService.GetUltimasMovimentacoesAsync(10);

        return new DashboardFinanceiroDto
        {
            TotalReceitasMes = totalReceitasMes,
            TotalDespesasMes = totalDespesasMes,
            SaldoMes = saldoMes,
            TotalReceitasAno = totalReceitasAno,
            TotalDespesasAno = totalDespesasAno,
            SaldoAno = saldoAno,
            FluxoCaixaMensal = fluxoCaixaMensal,
            ReceitasPorCategoria = receitasPorCategoria,
            DespesasPorCategoria = despesasPorCategoria,
            UltimasMovimentacoes = ultimasMovimentacoes
        };
    }
}
