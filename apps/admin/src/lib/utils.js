import { clsx } from "clsx";
import { twMerge } from "tailwind-merge"
import { API_BASE_URL, UPLOADS_BASE_URL } from "@/lib/env";

export function cn(...inputs) {
  return twMerge(clsx(inputs));
}

/**
 * Converte uma URL relativa para absoluta.
 * Usa a base da API atual (para preview no Admin: imagens recém-enviadas carregam do backend em uso).
 * @param {string} url - URL relativa (ex: /uploads/...) ou absoluta
 * @returns {string} URL absoluta
 */
export function getAbsoluteUrl(url) {
  if (!url) return null;
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  const normalizedUrl = url.startsWith('/') ? url.substring(1) : url;
  const cleanUrl = normalizedUrl.replace(/\/+/g, '/');
  return `${API_BASE_URL}/${cleanUrl}`;
}

/**
 * URL absoluta usando sempre o storage de produção (para listagens/relatórios que precisam de imagens centralizadas).
 */
export function getAbsoluteUrlProduction(url) {
  if (!url) return null;
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  const normalizedUrl = url.startsWith('/') ? url.substring(1) : url;
  const cleanUrl = normalizedUrl.replace(/\/+/g, '/');
  return `${UPLOADS_BASE_URL}/${cleanUrl}`;
}
