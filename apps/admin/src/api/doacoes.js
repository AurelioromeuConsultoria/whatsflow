import { api } from '@/lib/apiClient';

export const doacoesApi = {
  getAll: () => api.get('/doacoes'),
  getById: (id) => api.get(`/doacoes/${id}`),
  create: (data) => api.post('/doacoes', data),
};

export const finalidadesDoacaoApi = {
  getAll: () => api.get('/doacoes/finalidades'),
  getPublicas: () => api.get('/doacoes/finalidades/publicas'),
  getById: (id) => api.get(`/doacoes/finalidades/${id}`),
  create: (data) => api.post('/doacoes/finalidades', data),
  update: (id, data) => api.put(`/doacoes/finalidades/${id}`, data),
  delete: (id) => api.delete(`/doacoes/finalidades/${id}`),
};

export const doacoesConfigApi = {
  getAsaas: () => api.get('/doacoes/configuracao/asaas'),
  saveAsaas: (data) => api.put('/doacoes/configuracao/asaas', data),
};
