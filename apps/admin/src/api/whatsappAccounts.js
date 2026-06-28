import { api } from '@/lib/apiClient';

export const whatsappAccountsApi = {
  getAll: () => api.get('/whatsappaccounts'),
  getById: (id) => api.get(`/whatsappaccounts/${id}`),
  create: (payload) => api.post('/whatsappaccounts', payload),
  update: (id, payload) => api.put(`/whatsappaccounts/${id}`, payload),
  delete: (id) => api.delete(`/whatsappaccounts/${id}`),
};

// Enum WhatsAppProviderType (numérico) do backend.
export const WHATSAPP_PROVIDER = {
  FAKE: 0,
  OFFICIAL_CLOUD_API: 1,
  EVOLUTION_API: 2,
  TWILIO: 3,
  ZENVIA: 4,
  OTHER: 99,
};

// Enum WhatsAppAccountStatus (numérico) do backend.
export const WHATSAPP_ACCOUNT_STATUS = {
  ATIVA: 1,
  INATIVA: 2,
  ERRO_CONFIGURACAO: 3,
};
