import { api } from '@/lib/apiClient';

export const billingApi = {
  planos: () => api.get('/billing/planos'),
  minhaAssinatura: () => api.get('/billing/minha-assinatura'),
  faturas: () => api.get('/billing/faturas'),
  // ciclo e metodoPagamento são enviados como inteiro (a API binda enums como número)
  assinar: (data) => api.post('/billing/assinar', data),
  cancelar: () => api.post('/billing/cancelar'),
};

// Admin de plataforma (VerboPlus) — restrito a platform admin no backend
export const platformBillingApi = {
  assinaturas: () => api.get('/platform-billing/assinaturas'),
  suspender: (tenantId) => api.put(`/platform-billing/${tenantId}/suspender`),
  reativar: (tenantId) => api.put(`/platform-billing/${tenantId}/reativar`),
  processarCiclo: () => api.post('/platform-billing/processar-ciclo'),
};
