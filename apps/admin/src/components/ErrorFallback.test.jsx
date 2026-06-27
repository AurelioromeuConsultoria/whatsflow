import { render, screen } from '@testing-library/react';
import { ErrorFallback } from '@/components/ErrorFallback';

// Smoke test: valida que a infra de testes (render + jest-dom) está de pé.
describe('ErrorFallback', () => {
  it('mostra a mensagem de erro e o botão de recarregar', () => {
    render(<ErrorFallback />);
    expect(screen.getByText('Algo deu errado')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Recarregar' })).toBeInTheDocument();
  });
});
