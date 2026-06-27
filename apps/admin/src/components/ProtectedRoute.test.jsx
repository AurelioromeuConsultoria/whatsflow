import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k) => k }) }));
vi.mock('@/context/AuthContext', () => ({ useAuth: vi.fn() }));

import { useAuth } from '@/context/AuthContext';
import { ProtectedRoute } from '@/components/ProtectedRoute';

function renderAt(initial = '/secret') {
  return render(
    <MemoryRouter initialEntries={[initial]}>
      <Routes>
        <Route
          path="/secret"
          element={(
            <ProtectedRoute>
              <div>conteudo secreto</div>
            </ProtectedRoute>
          )}
        />
        <Route path="/login" element={<div>tela de login</div>} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('ProtectedRoute', () => {
  it('redireciona para /login quando não autenticado', () => {
    useAuth.mockReturnValue({ isAuthenticated: false, loading: false });
    renderAt();
    expect(screen.getByText('tela de login')).toBeInTheDocument();
    expect(screen.queryByText('conteudo secreto')).not.toBeInTheDocument();
  });

  it('renderiza o conteúdo protegido quando autenticado', () => {
    useAuth.mockReturnValue({ isAuthenticated: true, loading: false });
    renderAt();
    expect(screen.getByText('conteudo secreto')).toBeInTheDocument();
  });

  it('não vaza conteúdo nem redireciona enquanto verifica a sessão', () => {
    useAuth.mockReturnValue({ isAuthenticated: false, loading: true });
    renderAt();
    expect(screen.queryByText('tela de login')).not.toBeInTheDocument();
    expect(screen.queryByText('conteudo secreto')).not.toBeInTheDocument();
  });
});
