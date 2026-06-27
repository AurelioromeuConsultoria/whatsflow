import { api } from '@/lib/apiClient';

export const dashboardApi = {
  getEstatisticas: () => api.get('/dashboard/estatisticas'),
  getSerie: (meses = 6) => api.get('/dashboard/series', { params: { meses } }),
};

