import i18n from '../i18n';

const FALLBACK_LOCALE = 'pt-BR';

function getSafeDate(value) {
  if (!value) return null;

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  return date;
}

export function getCurrentLocale() {
  return i18n.resolvedLanguage || i18n.language || FALLBACK_LOCALE;
}

export function formatDate(value, fallback = '-', options = {}) {
  const date = getSafeDate(value);
  if (!date) return fallback;

  return date.toLocaleDateString(getCurrentLocale(), options);
}

export function formatDateTime(value, fallback = '-', options = {}) {
  const date = getSafeDate(value);
  if (!date) return fallback;

  return date.toLocaleString(getCurrentLocale(), options);
}

export function formatCurrency(value, fallback = '-', currency = 'BRL') {
  if (value === null || value === undefined || value === '') return fallback;

  const numericValue = Number(value);
  if (Number.isNaN(numericValue)) return fallback;

  return new Intl.NumberFormat(getCurrentLocale(), {
    style: 'currency',
    currency,
  }).format(numericValue);
}

export function formatDateBr(value, fallback = '-') {
  return formatDate(value, fallback);
}

export function formatDateTimeBr(value, fallback = '-') {
  return formatDateTime(value, fallback);
}

export function formatShortTime(value, fallback = i18n.t('common.notDefined')) {
  if (!value) return fallback;
  return String(value).slice(0, 5);
}
