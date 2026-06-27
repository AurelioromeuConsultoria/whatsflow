import { api } from '@/lib/apiClient';
import { USE_PRODUCTION_API_FOR_STORAGE, PRODUCTION_API_BASE_URL_WITH_API } from '@/lib/env';

export const categoriasMidiasApi = {
  getAll: () => api.get('/categoriasMidias'),
  getById: (id) => api.get(`/categoriasMidias/${id}`),
  create: (data) => api.post('/categoriasMidias', data),
  update: (id, data) => api.put(`/categoriasMidias/${id}`),
  delete: (id) => api.delete(`/categoriasMidias/${id}`),
};

export const galeriasFotosApi = {
  getAll: () => api.get('/galeriasFotos'),
  getAtivas: () => api.get('/galeriasFotos/ativas'),
  getById: (id) =>
    USE_PRODUCTION_API_FOR_STORAGE
      ? api.get(`${PRODUCTION_API_BASE_URL_WITH_API}/galeriasFotos/${id}`)
      : api.get(`/galeriasFotos/${id}`),
  getByEvento: (eventoId) => api.get(`/galeriasFotos/evento/${eventoId}`),
  getByCategoria: (categoriaId) => api.get(`/galeriasFotos/categoria/${categoriaId}`),
  create: (data) =>
    USE_PRODUCTION_API_FOR_STORAGE
      ? api.post(`${PRODUCTION_API_BASE_URL_WITH_API}/galeriasFotos`, data)
      : api.post('/galeriasFotos', data),
  update: (id, data) => api.put(`/galeriasFotos/${id}`, data),
  delete: (id) => api.delete(`/galeriasFotos/${id}`),
  upload: (id, formData) => {
    const url = USE_PRODUCTION_API_FOR_STORAGE
      ? `${PRODUCTION_API_BASE_URL_WITH_API}/galeriasFotos/${id}/upload`
      : `/galeriasFotos/${id}/upload`;
    return api.post(url, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  definirDestaque: (id, nomeArquivo) =>
    USE_PRODUCTION_API_FOR_STORAGE
      ? api.put(`${PRODUCTION_API_BASE_URL_WITH_API}/galeriasFotos/${id}/destaque`, nomeArquivo)
      : api.put(`/galeriasFotos/${id}/destaque`, nomeArquivo),
  listarFotos: (id) =>
    USE_PRODUCTION_API_FOR_STORAGE
      ? api.get(`${PRODUCTION_API_BASE_URL_WITH_API}/galeriasFotos/${id}/fotos`)
      : api.get(`/galeriasFotos/${id}/fotos`),
};

