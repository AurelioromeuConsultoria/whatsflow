import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';

const loginMock = vi.fn();
const toastError = vi.fn();
const toastSuccess = vi.fn();

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k) => k }) }));
vi.mock('sonner', () => ({
  toast: { error: (...a) => toastError(...a), success: (...a) => toastSuccess(...a) },
}));
vi.mock('@/components/ui/sonner', () => ({ Toaster: () => null }));
vi.mock('@/context/AuthContext', () => ({
  useAuth: () => ({ login: loginMock, isAuthenticated: false }),
}));

import Login from '@/pages/Login/Login';

function renderLogin() {
  return render(
    <MemoryRouter>
      <Login />
    </MemoryRouter>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Login', () => {
  it('chama login e mostra erro amigável quando as credenciais são inválidas', async () => {
    loginMock.mockResolvedValue({ success: false, message: 'Email ou senha inválidos' });
    renderLogin();

    await userEvent.type(screen.getByLabelText('login.email'), 'a@a.com');
    await userEvent.type(screen.getByLabelText('login.password'), 'errada');
    await userEvent.click(screen.getByRole('button', { name: 'login.submit' }));

    await waitFor(() => expect(loginMock).toHaveBeenCalledWith('a@a.com', 'errada'));
    expect(toastError).toHaveBeenCalledWith('Email ou senha inválidos');
    expect(toastSuccess).not.toHaveBeenCalled();
  });

  it('mostra toast de sucesso quando o login dá certo', async () => {
    loginMock.mockResolvedValue({ success: true });
    renderLogin();

    await userEvent.type(screen.getByLabelText('login.email'), 'a@a.com');
    await userEvent.type(screen.getByLabelText('login.password'), 'certa');
    await userEvent.click(screen.getByRole('button', { name: 'login.submit' }));

    await waitFor(() => expect(toastSuccess).toHaveBeenCalled());
    expect(toastError).not.toHaveBeenCalled();
  });
});
