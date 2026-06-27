import { api } from '@/lib/apiClient';

export const hubCasasApi = {
  getAll: () => api.get('/hub/casas'),
  getById: (id) => api.get(`/hub/casas/${id}`),
  create: (data) => api.post('/hub/casas', data),
  update: (id, data) => api.put(`/hub/casas/${id}`, data),
  delete: (id) => api.delete(`/hub/casas/${id}`),
};

