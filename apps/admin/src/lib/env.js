function normalizeBaseUrl(url) {
  if (!url) return '';
  return String(url).replace(/\/+$/, '');
}

const PRODUCTION_UPLOADS = 'https://api.kingdombr.com.br';

export const API_BASE_URL = normalizeBaseUrl(import.meta.env.VITE_API_URL || 'http://localhost:7000');

// Os endpoints do backend usam prefixo /api
export const API_BASE_URL_WITH_API = `${API_BASE_URL}/api`;

// URL base para imagens/uploads. Em dev com API local, forçar produção para as miniaturas carregarem (mesmo DB/storage).
const envUploads = normalizeBaseUrl(import.meta.env.VITE_UPLOADS_BASE_URL || '');
const isApiLocal = /localhost|127\.0\.0\.1/.test(API_BASE_URL);
const envUploadsIsLocal = envUploads && /localhost|127\.0\.0\.1/.test(envUploads);
const useProductionUploads = import.meta.env.DEV && isApiLocal && (!envUploads || envUploadsIsLocal);
export const UPLOADS_BASE_URL = useProductionUploads ? PRODUCTION_UPLOADS : (envUploads || PRODUCTION_UPLOADS);

/** Em dev com API local: enviar uploads e criação de galeria para a API de produção (storage sempre em produção). */
export const USE_PRODUCTION_API_FOR_STORAGE = import.meta.env.DEV && isApiLocal;
export const PRODUCTION_API_BASE_URL_WITH_API = `${PRODUCTION_UPLOADS}/api`;

if (import.meta.env.DEV && isApiLocal) {
  console.info('[Admin] API local: imagens e uploads de galeria usam produção (https://api.kingdombr.com.br)');
}
