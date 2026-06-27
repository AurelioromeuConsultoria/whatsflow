import axios from 'axios';
import { API_BASE_URL_WITH_API } from './env';

export const api = axios.create({
  baseURL: API_BASE_URL_WITH_API,
  headers: {
    'Content-Type': 'application/json',
  },
});

let refreshPromise = null;

function clearAuthSession() {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('usuario');
}

function redirectToLogin() {
  if (window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
}

function redirectToBilling() {
  if (window.location.pathname !== '/billing') {
    window.location.href = '/billing';
  }
}

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    const usuario = JSON.parse(localStorage.getItem('usuario') || 'null');
    const selectedTenantId = localStorage.getItem('selectedTenantId');
    const selectedTenantSlug = localStorage.getItem('selectedTenantSlug');
    const requestUrl = String(config.url || '').toLowerCase();
    const isAuthRequest =
      requestUrl.includes('/auth/login') ||
      requestUrl.includes('/auth/me') ||
      requestUrl.includes('/auth/refresh') ||
      requestUrl.includes('/auth/alterar-senha') ||
      requestUrl.includes('/tenants/contexto-operacional') ||
      requestUrl.includes('/auditoria-administrativa');

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Auth endpoints must always resolve the user's home tenant from the token.
    // The operational tenant override is only for business/admin screens.
    if (!isAuthRequest && usuario?.isPlatformAdmin && selectedTenantId) {
      config.headers['X-Tenant-Id'] = selectedTenantId;
      if (selectedTenantSlug) {
        config.headers['X-Tenant-Slug'] = selectedTenantSlug;
      }
    }

    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    console.error('Erro na API:', error);

    const originalRequest = error.config || {};
    const requestUrl = String(originalRequest.url || '').toLowerCase();
    const isLoginRequest = requestUrl.includes('/auth/login');
    const isRefreshRequest = requestUrl.includes('/auth/refresh');

    if (error.response?.status === 401 && !isLoginRequest) {
      if (isRefreshRequest || originalRequest._retry) {
        clearAuthSession();
        redirectToLogin();
        return Promise.reject(error);
      }

      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) {
        clearAuthSession();
        redirectToLogin();
        return Promise.reject(error);
      }

      originalRequest._retry = true;

      try {
        if (!refreshPromise) {
          refreshPromise = api
            .post('/auth/refresh', { refreshToken })
            .then((response) => response.data)
            .finally(() => {
              refreshPromise = null;
            });
        }

        const result = await refreshPromise;
        if (!result?.token) {
          clearAuthSession();
          redirectToLogin();
          return Promise.reject(error);
        }

        localStorage.setItem('token', result.token);
        if (result.refreshToken) {
          localStorage.setItem('refreshToken', result.refreshToken);
        }
        if (result.usuario) {
          localStorage.setItem('usuario', JSON.stringify(result.usuario));
        }

        originalRequest.headers = originalRequest.headers || {};
        originalRequest.headers.Authorization = `Bearer ${result.token}`;
        return api(originalRequest);
      } catch (refreshError) {
        clearAuthSession();
        redirectToLogin();
        return Promise.reject(refreshError);
      }
    }

    // Assinatura suspensa/cancelada — leva o usuário para regularizar.
    if (error.response?.status === 402) {
      redirectToBilling();
      return Promise.reject(error);
    }

    return Promise.reject(error);
  }
);
