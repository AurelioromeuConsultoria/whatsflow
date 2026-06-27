import { renderHook, act } from '@testing-library/react';
import { useFormValidation } from '@/hooks/useFormValidation';

describe('useFormValidation', () => {
  it('reprova campo required vazio', () => {
    const { result } = renderHook(() => useFormValidation({ nome: { required: true } }, { nome: '' }));
    let ok;
    act(() => { ok = result.current.validate(); });
    expect(ok).toBe(false);
    expect(result.current.errors.nome).toContain('obrigatório');
  });

  it('aprova quando os valores atendem às regras', () => {
    const { result } = renderHook(() =>
      useFormValidation({ nome: { required: true, minLength: 3 } }, { nome: 'João' }));
    let ok;
    act(() => { ok = result.current.validate(); });
    expect(ok).toBe(true);
    expect(result.current.errors).toEqual({});
  });

  it('valida e-mail', () => {
    const { result } = renderHook(() => useFormValidation({ email: { email: true } }, { email: 'invalido' }));
    act(() => { result.current.validate(); });
    expect(result.current.errors.email).toBe('Email inválido');
  });

  it('aplica minLength', () => {
    const { result } = renderHook(() => useFormValidation({ senha: { minLength: 8 } }, { senha: 'curta' }));
    act(() => { result.current.validate(); });
    expect(result.current.errors.senha).toContain('Mínimo');
  });

  it('usa função de validação custom', () => {
    const custom = (v) => (v === 'ruim' ? 'valor inválido' : null);
    const { result } = renderHook(() => useFormValidation({ x: { custom } }, { x: 'ruim' }));
    act(() => { result.current.validate(); });
    expect(result.current.errors.x).toBe('valor inválido');
  });

  it('handleBlur marca como touched e valida o campo', () => {
    const { result } = renderHook(() => useFormValidation({ nome: { required: true } }, { nome: '' }));
    act(() => { result.current.handleBlur('nome'); });
    expect(result.current.touched.nome).toBe(true);
    expect(result.current.errors.nome).toBeTruthy();
  });

  it('reset limpa erros e touched', () => {
    const { result } = renderHook(() => useFormValidation({ nome: { required: true } }, { nome: 'A' }));
    act(() => { result.current.handleBlur('nome'); });
    act(() => { result.current.reset(); });
    expect(result.current.touched).toEqual({});
    expect(result.current.errors).toEqual({});
  });
});
