import { api } from '@/lib/apiClient';

export const destaquesSiteApi = {
  getAll: () => api.get('/DestaquesSite'),
  getById: (id) => api.get(`/DestaquesSite/${id}`),
  create: (data) => api.post('/DestaquesSite', data),
  update: (id, data) => api.put(`/DestaquesSite/${id}`, data),
  delete: (id) => api.delete(`/DestaquesSite/${id}`),
};

export const categoriasNoticiasApi = {
  getAll: () => api.get('/CategoriasNoticias'),
  getById: (id) => api.get(`/CategoriasNoticias/${id}`),
  create: (data) => api.post('/CategoriasNoticias', data),
  update: (id, data) => api.put(`/CategoriasNoticias/${id}`, data),
  delete: (id) => api.delete(`/CategoriasNoticias/${id}`),
};

export const noticiasApi = {
  getAll: () => api.get('/Noticias'),
  getById: (id) => api.get(`/Noticias/${id}`),
  create: (data) => api.post('/Noticias', data),
  update: (id, data) => api.put(`/Noticias/${id}`, data),
  delete: (id) => api.delete(`/Noticias/${id}`),
  getByCategoria: (categoriaId) => api.get(`/Noticias/categoria/${categoriaId}`),
  /** Extrai título, data, descrição e texto de uma URL de notícia */
  extrairDeUrl: (url) => api.post('/Noticias/extrair-de-url', { url }),
};

export const contatosApi = {
  getAll: () => api.get('/Contatos'),
  getById: (id) => api.get(`/Contatos/${id}`),
  create: (data) => api.post('/Contatos', data),
  update: (id, data) => api.put(`/Contatos/${id}`),
  delete: (id) => api.delete(`/Contatos/${id}`),
};

export const configuracaoPortalApi = {
  get: () => api.get('/configuracaoPortal'),
  update: (data) => api.put('/configuracaoPortal', data),
};

export const enquetesApi = {
  getAll: () => api.get('/Enquetes'),
  getAtivas: () => api.get('/Enquetes/ativas'),
  getById: (id) => api.get(`/Enquetes/${id}`),
  create: (data) => api.post('/Enquetes', data),
  update: (id, data) => api.put(`/Enquetes/${id}`),
  delete: (id) => api.delete(`/Enquetes/${id}`),
};

