import { api } from '@/lib/apiClient';

export const authApi = {
  login: (data) => api.post('/auth/login', data),
  me: () => api.get('/auth/me'),
  alterarSenha: (data) => api.post('/auth/alterar-senha', data),
};

