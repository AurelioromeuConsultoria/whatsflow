import { getApiErrorMessage } from '@/lib/apiError';

describe('getApiErrorMessage', () => {
  it('usa corpo string crua', () => {
    expect(getApiErrorMessage({ response: { data: 'Falha crua' } })).toBe('Falha crua');
  });

  it('usa data.message', () => {
    expect(getApiErrorMessage({ response: { data: { message: 'Mensagem do backend' } } }))
      .toBe('Mensagem do backend');
  });

  it('usa data.error quando não há message', () => {
    expect(getApiErrorMessage({ response: { data: { error: 'Erro do backend' } } }))
      .toBe('Erro do backend');
  });

  it('usa o primeiro item de errors (array)', () => {
    expect(getApiErrorMessage({ response: { data: { errors: ['Primeiro', 'Segundo'] } } }))
      .toBe('Primeiro');
  });

  it('usa o primeiro erro de validação (objeto de campos)', () => {
    expect(getApiErrorMessage({ response: { data: { errors: { email: ['E-mail inválido'] } } } }))
      .toBe('E-mail inválido');
  });

  it('cai no fallback quando não há nada útil', () => {
    expect(getApiErrorMessage({}, 'Fallback')).toBe('Fallback');
    expect(getApiErrorMessage(null, 'Fallback')).toBe('Fallback');
  });

  it('usa error.message como último recurso antes do fallback', () => {
    expect(getApiErrorMessage({ message: 'Network Error' }, 'Fallback')).toBe('Network Error');
  });
});
