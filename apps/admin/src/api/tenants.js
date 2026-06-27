import { api } from '@/lib/apiClient';

export const tenantsApi = {
  getAll: () => api.get('/tenants'),
  getById: (id) => api.get(`/tenants/${id}`),
  create: (payload) => api.post('/tenants', payload),
  update: (id, payload) => api.put(`/tenants/${id}`, payload),
  updateStatus: (id, ativo) => api.put(`/tenants/${id}/status`, { ativo }),
  delete: (id) => api.delete(`/tenants/${id}`),
  auditOperationalContext: (payload) => api.post('/tenants/contexto-operacional', payload),
  getAdministrativeAuditTrail: (id) => api.get(`/tenants/${id}/auditoria-administrativa`),
};
