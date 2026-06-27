import { api } from '@/lib/apiClient';

export const pessoasApi = {
  getAll: () => api.get('/pessoas'),
  getPaged: (params) => api.get('/pessoas/paged', { params }),
  getById: (id) => api.get(`/pessoas/${id}`),
  get360: (id) => api.get(`/pessoas/${id}/360`),
  getAniversariantes: (dias = 30, limite = 50, mes = null) =>
    api.get('/pessoas/aniversariantes', { params: { dias, limite, mes } }),
  getCampanhaAniversario: (params) => api.get('/pessoas/aniversarios-campanha', { params }),
  updateCampanhaAniversario: (data) => api.put('/pessoas/aniversarios-campanha', data),
  sendCampanhaAniversarioTeste: (data) => api.post('/pessoas/aniversarios-campanha/teste', data),
  resendCampanhaAniversarioHistorico: (envioId) => api.post(`/pessoas/aniversarios-campanha/historico/${envioId}/reenviar`),
  create: (data) => api.post('/pessoas', data),
  update: (id, data) => api.put(`/pessoas/${id}`, data),
  delete: (id) => api.delete(`/pessoas/${id}`),
  // LGPD — direitos do titular
  exportarDados: (id) => api.get(`/pessoas/${id}/dados-pessoais`),
  anonimizar: (id) => api.post(`/pessoas/${id}/anonimizar`),
};

// LGPD — requisições de titulares (Art. 18)
export const solicitacoesTitularApi = {
  listar: (status) => api.get('/solicitacoestitular', { params: status ? { status } : {} }),
  obter: (id) => api.get(`/solicitacoestitular/${id}`),
  criar: (data) => api.post('/solicitacoestitular', data),
  atender: (id) => api.put(`/solicitacoestitular/${id}/atender`),
  concluir: (id, observacao) => api.put(`/solicitacoestitular/${id}/concluir`, { observacao }),
  recusar: (id, motivo) => api.put(`/solicitacoestitular/${id}/recusar`, { motivo }),
};

export const pessoasPerfisApi = {
  getAll: () => api.get('/pessoasperfis'),
  getById: (id) => api.get(`/pessoasperfis/${id}`),
  getByPessoa: (pessoaId) => api.get(`/pessoasperfis/pessoa/${pessoaId}`),
  create: (data) => api.post('/pessoasperfis', data),
  update: (id, data) => api.put(`/pessoasperfis/${id}`, data),
  delete: (id) => api.delete(`/pessoasperfis/${id}`),
};

export const visitantesApi = {
  getAll: () => api.get('/visitantes'),
  getPaged: (params) => api.get('/visitantes/paged', { params }),
  getById: (id) => api.get(`/visitantes/${id}`),
  getByPessoa: (pessoaId) => api.get(`/visitantes/pessoa/${pessoaId}`),
  create: (data) => api.post('/visitantes', data),
  update: (id, data) => api.put(`/visitantes/${id}`, data),
  delete: (id) => api.delete(`/visitantes/${id}`),
  regerarMensagens: (id) => api.post(`/visitantes/${id}/regerar-mensagens`),
};

export const configuracoesMensagensApi = {
  getAll: () => api.get('/configuracoesMensagens'),
  getById: (id) => api.get(`/configuracoesMensagens/${id}`),
  create: (data) => api.post('/configuracoesMensagens', data),
  update: (id, data) => api.put(`/configuracoesMensagens/${id}`, data),
  delete: (id) => api.delete(`/configuracoesMensagens/${id}`),
};

export const mensagensAgendadasApi = {
  getAll: () => api.get('/mensagensAgendadas'),
  getPaged: (params) => api.get('/mensagensAgendadas/paged', { params }),
  getStats: () => api.get('/mensagensAgendadas/stats'),
  getById: (id) => api.get(`/mensagensAgendadas/${id}`),
  cancelar: (id) => api.patch(`/mensagensAgendadas/${id}/cancelar`),
};
