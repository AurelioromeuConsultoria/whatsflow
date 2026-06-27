import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k) => k }) }));
vi.mock('@/context/AuthContext', () => ({ useAuth: vi.fn() }));

import { useAuth } from '@/context/AuthContext';
import { RequirePermission } from '@/components/RequirePermission';

function renderAt(props) {
  return render(
    <MemoryRouter initialEntries={['/secret']}>
      <Routes>
        <Route
          path="/secret"
          element={(
            <RequirePermission {...props}>
              <div>conteudo protegido</div>
            </RequirePermission>
          )}
        />
        <Route path="/login" element={<div>tela de login</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('RequirePermission', () => {
  it('redireciona para /login quando não autenticado', () => {
    useAuth.mockReturnValue({ loading: false, isAuthenticated: false, isAdmin: false, can: () => true });
    renderAt({ resource: 'pessoas' });
    expect(screen.getByText('tela de login')).toBeInTheDocument();
    expect(screen.queryByText('conteudo protegido')).not.toBeInTheDocument();
  });

  it('bloqueia quando exige admin e o usuário não é admin', () => {
    useAuth.mockReturnValue({ loading: false, isAuthenticated: true, isAdmin: false, can: () => true });
    renderAt({ requireAdmin: true });
    expect(screen.queryByText('conteudo protegido')).not.toBeInTheDocument();
  });

  it('libera quando exige admin e o usuário é admin', () => {
    useAuth.mockReturnValue({ loading: false, isAuthenticated: true, isAdmin: true, can: () => false });
    renderAt({ requireAdmin: true });
    expect(screen.getByText('conteudo protegido')).toBeInTheDocument();
  });

  it('bloqueia quando o usuário não tem a permissão do recurso', () => {
    const can = vi.fn().mockReturnValue(false);
    useAuth.mockReturnValue({ loading: false, isAuthenticated: true, isAdmin: false, can });
    renderAt({ resource: 'financeiro', action: 'edit' });
    expect(screen.queryByText('conteudo protegido')).not.toBeInTheDocument();
    expect(can).toHaveBeenCalledWith('financeiro', 'edit');
  });

  it('libera quando o usuário tem a permissão do recurso', () => {
    useAuth.mockReturnValue({ loading: false, isAuthenticated: true, isAdmin: false, can: () => true });
    renderAt({ resource: 'pessoas', action: 'view' });
    expect(screen.getByText('conteudo protegido')).toBeInTheDocument();
  });

  it('não vaza conteúdo enquanto verifica a sessão', () => {
    useAuth.mockReturnValue({ loading: true, isAuthenticated: false, isAdmin: false, can: () => true });
    renderAt({ resource: 'pessoas' });
    expect(screen.queryByText('conteudo protegido')).not.toBeInTheDocument();
    expect(screen.queryByText('tela de login')).not.toBeInTheDocument();
  });
});
