import { api } from '@/lib/apiClient';

export const usuariosApi = {
  getAll: () => api.get('/usuarios'),
  getById: (id) => api.get(`/usuarios/${id}`),
  create: (data) => api.post('/usuarios', data),
  update: (id, data) => api.put(`/usuarios/${id}`, data),
  delete: (id) => api.delete(`/usuarios/${id}`),
};

export const perfisAcessoApi = {
  getAll: () => api.get('/perfis-acesso'),
  getById: (id) => api.get(`/perfis-acesso/${id}`),
  create: (data) => api.post('/perfis-acesso', data),
  update: (id, data) => api.put(`/perfis-acesso/${id}`),
  delete: (id) => api.delete(`/perfis-acesso/${id}`),
};

