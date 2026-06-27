import { api } from '@/lib/apiClient';

export const dashboardFinanceiroApi = {
  getDashboard: () => api.get('/dashboardfinanceiro'),
};

export const relatoriosFinanceirosApi = {
  getFluxoCaixa: (dataInicio, dataFim) =>
    api.get('/relatoriosfinanceiros/fluxo-caixa', { params: { dataInicio, dataFim } }),
  getPorCategoria: (dataInicio, dataFim) =>
    api.get('/relatoriosfinanceiros/por-categoria', { params: { dataInicio, dataFim } }),
  getPorCentroCusto: (dataInicio, dataFim) =>
    api.get('/relatoriosfinanceiros/por-centro-custo', { params: { dataInicio, dataFim } }),
  getPorProjeto: (dataInicio, dataFim) =>
    api.get('/relatoriosfinanceiros/por-projeto', { params: { dataInicio, dataFim } }),
  getDre: (ano) =>
    api.get('/relatoriosfinanceiros/dre', { params: { ano } }),
};

