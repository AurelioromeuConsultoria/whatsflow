import { renderHook, act, waitFor } from '@testing-library/react';

vi.mock('@/lib/api', () => ({
  authApi: { login: vi.fn(), me: vi.fn() },
  tenantsApi: { getAll: vi.fn() },
}));

import { authApi } from '@/lib/api';
import { AuthProvider, useAuth } from '@/context/AuthContext';

const wrapper = ({ children }) => <AuthProvider>{children}</AuthProvider>;

const usuarioFake = {
  id: 1,
  nome: 'Admin',
  isPlatformAdmin: false,
  tenantId: 5,
  tenantSlug: 'igreja-x',
};

beforeEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
});

describe('AuthContext', () => {
  it('faz login com sucesso e persiste o token', async () => {
    authApi.login.mockResolvedValue({
      data: { token: 'tok', refreshToken: 'ref', usuario: usuarioFake },
    });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.loading).toBe(false));

    let res;
    await act(async () => {
      res = await result.current.login('a@a.com', '123');
    });

    expect(res).toEqual({ success: true });
    expect(localStorage.getItem('token')).toBe('tok');
    expect(result.current.isAuthenticated).toBe(true);
    expect(authApi.login).toHaveBeenCalledWith({ email: 'a@a.com', senha: '123' });
  });

  it('retorna a mensagem do backend ({ message }) quando o login falha', async () => {
    authApi.login.mockRejectedValue({
      response: { status: 401, data: { message: 'Email ou senha inválidos' } },
    });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.loading).toBe(false));

    let res;
    await act(async () => {
      res = await result.current.login('a@a.com', 'errada');
    });

    expect(res.success).toBe(false);
    expect(res.message).toBe('Email ou senha inválidos');
    expect(result.current.isAuthenticated).toBe(false);
  });

  // Regressão do bug real: o backend já retornou string crua no corpo do 401.
  it('trata corpo de erro como string crua (sem campo message)', async () => {
    authApi.login.mockRejectedValue({
      response: { status: 401, data: 'Conta temporariamente bloqueada' },
    });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.loading).toBe(false));

    let res;
    await act(async () => {
      res = await result.current.login('a@a.com', 'x');
    });

    expect(res.message).toBe('Conta temporariamente bloqueada');
  });

  // Regressão: nunca expor "Request failed with status code 401" ao usuário.
  it('usa fallback amigável e nunca o erro cru do axios', async () => {
    authApi.login.mockRejectedValue({ message: 'Request failed with status code 401' });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.loading).toBe(false));

    let res;
    await act(async () => {
      res = await result.current.login('a@a.com', 'x');
    });

    expect(res.message).toBe('Email ou senha inválidos');
    expect(res.message).not.toContain('Request failed');
  });

  it('logout limpa o storage e o estado', async () => {
    authApi.login.mockResolvedValue({
      data: { token: 'tok', refreshToken: 'ref', usuario: usuarioFake },
    });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.loading).toBe(false));
    await act(async () => {
      await result.current.login('a@a.com', '123');
    });
    expect(result.current.isAuthenticated).toBe(true);

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(localStorage.getItem('token')).toBeNull();
  });
});
