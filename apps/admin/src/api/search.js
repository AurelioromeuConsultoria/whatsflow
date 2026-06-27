import { api } from '@/lib/apiClient';

export const searchApi = {
  search: (q, limit = 20) => api.get('/search', { params: { q, limit } }),
};

