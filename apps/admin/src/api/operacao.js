import axios from 'axios';
import { api } from '@/lib/apiClient';
import { API_BASE_URL } from '@/lib/env';

export const operacaoApi = {
  getHealth: () => axios.get(`${API_BASE_URL}/health`),
  getSchedulers: () => api.get('/operacao/schedulers'),
};
