import * as Sentry from '@sentry/react';

const dsn = import.meta.env.VITE_SENTRY_DSN;

/**
 * Inicializa o Sentry no admin. Sem DSN (ex.: dev/local) vira no-op.
 * Configurado para reportar SÓ erros (sem performance/replay) para ficar no
 * plano gratuito, e sem PII por padrão (LGPD).
 */
export function initSentry() {
  if (!dsn) return;

  Sentry.init({
    dsn,
    environment: import.meta.env.VITE_SENTRY_ENVIRONMENT || 'production',
    sendDefaultPii: false,
    tracesSampleRate: 0,
    // Ruído comum de browsers/extensões que não é bug da aplicação.
    ignoreErrors: [
      'ResizeObserver loop limit exceeded',
      'ResizeObserver loop completed with undelivered notifications.',
      'Non-Error promise rejection captured',
    ],
  });
}

export { Sentry };
