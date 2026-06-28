import { api } from '@/lib/apiClient';

export const contatosApi = {
  getPaged: (params) => api.get('/contatos/paged', { params }),
  getById: (id) => api.get(`/contatos/${id}`),
  create: (payload) => api.post('/contatos', payload),
  update: (id, payload) => api.put(`/contatos/${id}`, payload),
  delete: (id) => api.delete(`/contatos/${id}`),
};

// Enum ContatoStatus (numérico) do backend.
export const CONTATO_STATUS = {
  ATIVO: 1,
  INATIVO: 2,
  BLOQUEADO: 3,
};
