import { api } from '@/lib/apiClient';

export const notificacoesApi = {
  getMinhas: (params) => api.get('/Notificacoes', { params }),
  getUnreadCount: () => api.get('/Notificacoes/nao-lidas/count'),
  marcarComoLida: (id) => api.post(`/Notificacoes/${id}/marcar-lida`),
  marcarTodasComoLidas: () => api.post('/Notificacoes/marcar-todas-lidas'),
};
