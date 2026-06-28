import { api } from '@/lib/apiClient';

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

// Mantido para o vínculo opcional de usuário↔contato no cadastro de usuários.
// Aponta para /contatos no backend WhatsFlow (substituto de /pessoas).
export const pessoasApi = {
  getAll: () => api.get('/contatos/paged', { params: { page: 1, pageSize: 200 } })
    .then((res) => ({ ...res, data: res.data?.items ?? [] })),
};
