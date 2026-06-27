import { api } from '@/lib/apiClient';

export const despesasApi = {
  getAll: () => api.get('/despesas'),
  getById: (id) => api.get(`/despesas/${id}`),
  create: (data) => api.post('/despesas', data),
  update: (id, data) => api.put(`/despesas/${id}`, data),
  delete: (id) => api.delete(`/despesas/${id}`),
  getVencimentos: () => api.get('/despesas/vencimentos'),
  gerarProxima: (id) => api.post(`/despesas/${id}/gerar-proxima`),
};

export const orcamentoCategoriasApi = {
  getByAno: (ano) => api.get('/orcamentocategorias', { params: { ano } }),
  save: (data) => api.post('/orcamentocategorias', data),
  delete: (id) => api.delete(`/orcamentocategorias/${id}`),
  getComparacao: (ano) => api.get('/orcamentocategorias/comparacao', { params: { ano } }),
};


export const receitasApi = {
  getAll: (params) => api.get('/receitas', { params }),
  getById: (id) => api.get(`/receitas/${id}`),
  create: (data) => api.post('/receitas', data),
  update: (id, data) => api.put(`/receitas/${id}`, data),
  delete: (id) => api.delete(`/receitas/${id}`),
  lancarLote: (data) => api.post('/receitas/lote', data),
  getRelatorioContribuicoes: (params) => api.get('/receitas/relatorio/contribuicoes', { params }),
  getInformeAnual: (pessoaId, ano) => api.get(`/receitas/informe/${pessoaId}`, { params: { ano } }),
  gerarProxima: (id) => api.post(`/receitas/${id}/gerar-proxima`),
};

