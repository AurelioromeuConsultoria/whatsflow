import { api } from '@/lib/apiClient';

export const tagsApi = {
  getAll: () => api.get('/tags'),
  getById: (id) => api.get(`/tags/${id}`),
  create: (payload) => api.post('/tags', payload),
  update: (id, payload) => api.put(`/tags/${id}`, payload),
  delete: (id) => api.delete(`/tags/${id}`),
};
