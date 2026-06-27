import { api } from '@/lib/apiClient';

export const fornecedoresApi = {
  getAll: () => api.get('/fornecedores'),
  getById: (id) => api.get(`/fornecedores/${id}`),
  create: (data) => api.post('/fornecedores', data),
  update: (id, data) => api.put(`/fornecedores/${id}`, data),
  delete: (id) => api.delete(`/fornecedores/${id}`),
};

export const categoriasDespesasApi = {
  getAll: () => api.get('/categoriasDespesas'),
  getById: (id) => api.get(`/categoriasDespesas/${id}`),
  create: (data) => api.post('/categoriasDespesas', data),
  update: (id, data) => api.put(`/categoriasDespesas/${id}`, data),
  delete: (id) => api.delete(`/categoriasDespesas/${id}`),
};

export const contasBancariasApi = {
  getAll: () => api.get('/contasBancarias'),
  getById: (id) => api.get(`/contasBancarias/${id}`),
  create: (data) => api.post('/contasBancarias', data),
  update: (id, data) => api.put(`/contasBancarias/${id}`, data),
  delete: (id) => api.delete(`/contasBancarias/${id}`),
};

export const centrosCustosApi = {
  getAll: () => api.get('/centrosCustos'),
  getById: (id) => api.get(`/centrosCustos/${id}`),
  create: (data) => api.post('/centrosCustos', data),
  update: (id, data) => api.put(`/centrosCustos/${id}`, data),
  delete: (id) => api.delete(`/centrosCustos/${id}`),
};

export const projetosApi = {
  getAll: () => api.get('/projetos'),
  getById: (id) => api.get(`/projetos/${id}`),
  create: (data) => api.post('/projetos', data),
  update: (id, data) => api.put(`/projetos/${id}`, data),
  delete: (id) => api.delete(`/projetos/${id}`),
};

export const categoriasPatrimonioApi = {
  getAll: () => api.get('/categoriaspatrimonio'),
  getById: (id) => api.get(`/categoriaspatrimonio/${id}`),
  create: (data) => api.post('/categoriaspatrimonio', data),
  update: (id, data) => api.put(`/categoriaspatrimonio/${id}`, data),
  delete: (id) => api.delete(`/categoriaspatrimonio/${id}`),
};

export const patrimonioApi = {
  getAll: () => api.get('/patrimonio'),
  getById: (id) => api.get(`/patrimonio/${id}`),
  create: (data) => api.post('/patrimonio', data),
  update: (id, data) => api.put(`/patrimonio/${id}`, data),
  delete: (id) => api.delete(`/patrimonio/${id}`),
};

export const patrimonioMovimentacoesApi = {
  getByPatrimonioId: (patrimonioItemId) => api.get(`/patrimonio/${patrimonioItemId}/movimentacoes`),
  create: (patrimonioItemId, data) => api.post(`/patrimonio/${patrimonioItemId}/movimentacoes`, data),
};

export const categoriasReceitasApi = {
  getAll: () => api.get('/categoriasreceitas'),
  getById: (id) => api.get(`/categoriasreceitas/${id}`),
  create: (data) => api.post('/categoriasreceitas', data),
  update: (id, data) => api.put(`/categoriasreceitas/${id}`, data),
  delete: (id) => api.delete(`/categoriasreceitas/${id}`),
};
