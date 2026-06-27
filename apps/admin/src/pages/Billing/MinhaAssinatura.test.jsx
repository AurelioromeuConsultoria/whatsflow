import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

const billing = {
  planos: vi.fn(),
  minhaAssinatura: vi.fn(),
  faturas: vi.fn(),
  assinar: vi.fn(),
  cancelar: vi.fn(),
};
const toastSuccess = vi.fn();
const toastError = vi.fn();

vi.mock('@/lib/api', () => ({
  billingApi: {
    planos: (...a) => billing.planos(...a),
    minhaAssinatura: (...a) => billing.minhaAssinatura(...a),
    faturas: (...a) => billing.faturas(...a),
    assinar: (...a) => billing.assinar(...a),
    cancelar: (...a) => billing.cancelar(...a),
  },
}));
vi.mock('sonner', () => ({ toast: { success: (...a) => toastSuccess(...a), error: (...a) => toastError(...a) } }));
vi.mock('@/lib/apiError', () => ({ getApiErrorMessage: (err, fallback) => fallback }));
vi.mock('@/lib/formatters', () => ({ formatDate: (d) => String(d ?? '') }));

import MinhaAssinatura from '@/pages/Billing/MinhaAssinatura';

beforeEach(() => {
  vi.clearAllMocks();
});

describe('MinhaAssinatura', () => {
  it('mostra a assinatura existente (plano, status e valor)', async () => {
    billing.planos.mockResolvedValue({ data: [] });
    billing.minhaAssinatura.mockResolvedValue({
      data: { status: 'Ativa', planoNome: 'Organização', valor: 149, ciclo: 'Mensal', emTrial: false },
    });
    billing.faturas.mockResolvedValue({ data: [] });

    render(<MinhaAssinatura />);

    expect(await screen.findByText('Plano Organização')).toBeInTheDocument();
    expect(screen.getByText('Ativa')).toBeInTheDocument();
    expect(billing.faturas).toHaveBeenCalled();
  });

  it('mostra a seleção de planos quando ainda não há assinatura (404)', async () => {
    billing.planos.mockResolvedValue({
      data: [
        { id: 1, nome: 'Essencial', precoMensal: 49 },
        { id: 2, nome: 'Organização', precoMensal: 149 },
      ],
    });
    billing.minhaAssinatura.mockRejectedValue({ response: { status: 404 } });

    render(<MinhaAssinatura />);

    expect(await screen.findByText('Escolha um plano')).toBeInTheDocument();
    expect(screen.getByText('Essencial')).toBeInTheDocument();
    expect(screen.getByText('Organização')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Assinar' })).toHaveLength(2);
  });

  it('assina um plano enviando o payload correto', async () => {
    billing.planos.mockResolvedValue({ data: [{ id: 2, nome: 'Organização', precoMensal: 149 }] });
    billing.minhaAssinatura.mockRejectedValue({ response: { status: 404 } });
    billing.assinar.mockResolvedValue({ data: {} });
    const user = userEvent.setup();

    render(<MinhaAssinatura />);
    await user.click(await screen.findByRole('button', { name: 'Assinar' }));

    // Dialog aberto: preenche o nome de cobrança (ciclo já vem Mensal por padrão).
    await user.type(await screen.findByLabelText('Nome para cobrança *'), 'Igreja Teste');
    await user.click(screen.getByRole('button', { name: 'Iniciar assinatura' }));

    await waitFor(() => expect(billing.assinar).toHaveBeenCalledTimes(1));
    expect(billing.assinar).toHaveBeenCalledWith(
      expect.objectContaining({ planoId: 2, ciclo: 1, nomeCliente: 'Igreja Teste' }),
    );
    expect(toastSuccess).toHaveBeenCalled();
  });

  it('exige o nome de cobrança antes de assinar', async () => {
    billing.planos.mockResolvedValue({ data: [{ id: 2, nome: 'Organização', precoMensal: 149 }] });
    billing.minhaAssinatura.mockRejectedValue({ response: { status: 404 } });
    const user = userEvent.setup();

    render(<MinhaAssinatura />);
    await user.click(await screen.findByRole('button', { name: 'Assinar' }));
    await user.click(await screen.findByRole('button', { name: 'Iniciar assinatura' }));

    expect(toastError).toHaveBeenCalledWith('Informe o nome para a cobrança.');
    expect(billing.assinar).not.toHaveBeenCalled();
  });
});
