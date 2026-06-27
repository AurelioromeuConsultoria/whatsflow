import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';

const signupMock = vi.fn();
const toastError = vi.fn();

vi.mock('@/lib/api', () => ({ signupApi: { signup: (...a) => signupMock(...a) } }));
vi.mock('sonner', () => ({ toast: { error: (...a) => toastError(...a), success: vi.fn() } }));
vi.mock('@/components/ui/sonner', () => ({ Toaster: () => null }));
vi.mock('@/lib/apiError', () => ({
  getApiErrorMessage: (err, fallback) => err?.response?.data?.message || fallback,
}));

import Signup from '@/pages/Signup/Signup';

function renderSignup() {
  return render(
    <MemoryRouter>
      <Signup />
    </MemoryRouter>,
  );
}

async function preencherValido(user) {
  await user.type(screen.getByLabelText(/nome da igreja/i), 'Igreja Teste');
  await user.type(screen.getByLabelText(/seu nome/i), 'Fulano');
  await user.type(screen.getByLabelText(/e-mail/i), 'fulano@teste.com');
  await user.type(screen.getByLabelText(/senha/i), 'Senha1234');
  await user.click(screen.getByRole('checkbox'));
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Signup', () => {
  it('valida campos obrigatórios e não chama a API', async () => {
    const user = userEvent.setup();
    renderSignup();
    await user.click(screen.getByRole('button', { name: /criar conta/i }));
    expect(toastError).toHaveBeenCalledWith('Preencha todos os campos obrigatórios.');
    expect(signupMock).not.toHaveBeenCalled();
  });

  it('exige senha com no mínimo 8 caracteres', async () => {
    const user = userEvent.setup();
    renderSignup();
    await user.type(screen.getByLabelText(/nome da igreja/i), 'Igreja Teste');
    await user.type(screen.getByLabelText(/seu nome/i), 'Fulano');
    await user.type(screen.getByLabelText(/e-mail/i), 'fulano@teste.com');
    await user.type(screen.getByLabelText(/senha/i), 'curta');
    await user.click(screen.getByRole('checkbox'));
    await user.click(screen.getByRole('button', { name: /criar conta/i }));
    expect(toastError).toHaveBeenCalledWith('A senha deve ter ao menos 8 caracteres.');
    expect(signupMock).not.toHaveBeenCalled();
  });

  it('exige aceite dos termos', async () => {
    const user = userEvent.setup();
    renderSignup();
    await user.type(screen.getByLabelText(/nome da igreja/i), 'Igreja Teste');
    await user.type(screen.getByLabelText(/seu nome/i), 'Fulano');
    await user.type(screen.getByLabelText(/e-mail/i), 'fulano@teste.com');
    await user.type(screen.getByLabelText(/senha/i), 'Senha1234');
    await user.click(screen.getByRole('button', { name: /criar conta/i }));
    expect(toastError).toHaveBeenCalledWith(
      'É necessário aceitar os Termos de Uso e a Política de Privacidade.',
    );
    expect(signupMock).not.toHaveBeenCalled();
  });

  it('envia o cadastro e mostra a tela de confirmação de e-mail', async () => {
    signupMock.mockResolvedValue({ data: { email: 'fulano@teste.com' } });
    const user = userEvent.setup();
    renderSignup();
    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /criar conta/i }));

    await waitFor(() => expect(signupMock).toHaveBeenCalledTimes(1));
    expect(signupMock).toHaveBeenCalledWith(
      expect.objectContaining({
        nomeIgreja: 'Igreja Teste',
        adminNome: 'Fulano',
        email: 'fulano@teste.com',
        senha: 'Senha1234',
        planoSlug: 'organizacao',
        aceiteTermosVersao: 'v1',
      }),
    );
    expect(await screen.findByText('Verifique seu e-mail')).toBeInTheDocument();
    expect(screen.getByText('fulano@teste.com')).toBeInTheDocument();
  });

  it('mostra erro amigável quando a API falha', async () => {
    signupMock.mockRejectedValue({ response: { data: { message: 'E-mail já cadastrado.' } } });
    const user = userEvent.setup();
    renderSignup();
    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /criar conta/i }));

    await waitFor(() => expect(toastError).toHaveBeenCalledWith('E-mail já cadastrado.'));
  });
});
