import { api } from '@/lib/apiClient';
import { API_BASE_URL } from '@/lib/env';

export const comunicacaoCampanhasApi = {
  getPaged: (params) => api.get('/comunicacaocampanhas/paged', { params }),
  getStats: () => api.get('/comunicacaocampanhas/stats'),
  getById: (id) => api.get(`/comunicacaocampanhas/${id}`),
  getEntregas: (id) => api.get(`/comunicacaocampanhas/${id}/entregas`),
  create: (payload) => api.post('/comunicacaocampanhas', payload),
  update: (id, payload) => api.put(`/comunicacaocampanhas/${id}`, payload),
};

export const comunicacaoTemplatesApi = {
  getAll: () => api.get('/comunicacaotemplates'),
  getById: (id) => api.get(`/comunicacaotemplates/${id}`),
  create: (payload) => api.post('/comunicacaotemplates', payload),
  update: (id, payload) => api.put(`/comunicacaotemplates/${id}`, payload),
};

export const comunicacaoSegmentosApi = {
  getAll: () => api.get('/comunicacaosegmentos'),
  getById: (id) => api.get(`/comunicacaosegmentos/${id}`),
  getEstimativa: (params) => api.get('/comunicacaosegmentos/estimativa', { params }),
  create: (payload) => api.post('/comunicacaosegmentos', payload),
  update: (id, payload) => api.put(`/comunicacaosegmentos/${id}`, payload),
};

export const comunicacaoPreferenciasApi = {
  getByPessoaId: (pessoaId) => api.get(`/comunicacaopreferencias/pessoa/${pessoaId}`),
  update: (pessoaId, canal, payload) => api.put(`/comunicacaopreferencias/pessoa/${pessoaId}/canal/${canal}`, payload),
};

export const comunicacaoEntregasApi = {
  getPaged: (params) => api.get('/comunicacaoentregas/paged', { params }),
  processarPendentes: (limit = 50) => api.post('/comunicacaoentregas/processar', null, { params: { limit } }),
  reprocessar: (id) => api.post(`/comunicacaoentregas/reprocessar/${id}`),
};

export const comunicacaoAutomacoesApi = {
  getHistorico: (params) => api.get('/comunicacaoautomacoes/historico', { params }),
};

export const comunicacaoDiagnosticoApi = {
  getHealth: () => api.get('/health', { baseURL: API_BASE_URL }),
};
