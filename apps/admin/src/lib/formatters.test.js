import { formatDate, formatDateTime, formatCurrency, formatShortTime } from '@/lib/formatters';

describe('formatters', () => {
  describe('formatDate', () => {
    it('retorna fallback para valor vazio ou inválido', () => {
      expect(formatDate(null)).toBe('-');
      expect(formatDate('')).toBe('-');
      expect(formatDate('não é data')).toBe('-');
      expect(formatDate(undefined, 'sem data')).toBe('sem data');
    });

    it('formata uma data válida (não retorna o fallback)', () => {
      const out = formatDate('2026-06-15T00:00:00Z');
      expect(out).not.toBe('-');
      expect(typeof out).toBe('string');
    });
  });

  describe('formatDateTime', () => {
    it('retorna fallback para data inválida', () => {
      expect(formatDateTime('xyz')).toBe('-');
    });
  });

  describe('formatCurrency', () => {
    it('retorna fallback para valores vazios/não numéricos', () => {
      expect(formatCurrency(null)).toBe('-');
      expect(formatCurrency('')).toBe('-');
      expect(formatCurrency('abc')).toBe('-');
    });

    it('formata valores numéricos em BRL', () => {
      const out = formatCurrency(1234.5);
      expect(out).toContain('R$');
      expect(out).not.toBe('-');
    });
  });

  describe('formatShortTime', () => {
    it('corta para HH:mm', () => {
      expect(formatShortTime('12:34:56')).toBe('12:34');
    });

    it('usa fallback explícito quando vazio', () => {
      expect(formatShortTime('', 'sem hora')).toBe('sem hora');
    });
  });
});
