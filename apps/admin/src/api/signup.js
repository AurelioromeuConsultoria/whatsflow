import { api } from '@/lib/apiClient';

export const signupApi = {
  // ciclo/enum vão como inteiro quando enviados; aqui omitimos ciclo (default Mensal no backend)
  signup: (data) => api.post('/signup', data),
};
