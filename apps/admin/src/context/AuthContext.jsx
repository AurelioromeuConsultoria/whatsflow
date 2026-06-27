import { createContext, useContext, useState, useEffect } from 'react';
import { authApi, tenantsApi } from '@/lib/api';

const AuthContext = createContext(null);
const SELECTED_TENANT_ID_KEY = 'selectedTenantId';
const SELECTED_TENANT_SLUG_KEY = 'selectedTenantSlug';

function persistSelectedTenant(tenant) {
  if (!tenant?.id) {
    localStorage.removeItem(SELECTED_TENANT_ID_KEY);
    localStorage.removeItem(SELECTED_TENANT_SLUG_KEY);
    return;
  }

  localStorage.setItem(SELECTED_TENANT_ID_KEY, String(tenant.id));
  localStorage.setItem(SELECTED_TENANT_SLUG_KEY, tenant.slug || '');
}

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(null);
  const [availableTenants, setAvailableTenants] = useState([]);
  const [selectedTenant, setSelectedTenant] = useState(null);
  const [loading, setLoading] = useState(true);

  const syncOperationalTenant = (usuarioData, tenants = []) => {
    const homeTenant = usuarioData
      ? {
          id: usuarioData.tenantId,
          slug: usuarioData.tenantSlug,
          nome: usuarioData.tenantNome || usuarioData.tenantSlug,
          nomeExibicao: usuarioData.tenantNomeExibicao || usuarioData.tenantNome || usuarioData.tenantSlug,
          logoUrl: usuarioData.tenantLogoUrl || null,
          faviconUrl: usuarioData.tenantFaviconUrl || null,
          corPrimaria: usuarioData.tenantCorPrimaria || null,
          corSecundaria: usuarioData.tenantCorSecundaria || null,
          isRootTenant: !!usuarioData.isRootTenant,
        }
      : null;

    if (!usuarioData?.isPlatformAdmin) {
      setSelectedTenant(homeTenant);
      persistSelectedTenant(homeTenant);
      setAvailableTenants(homeTenant ? [homeTenant] : []);
      return;
    }

    const storedTenantId = Number(localStorage.getItem(SELECTED_TENANT_ID_KEY) || 0);
    const candidates = tenants.length > 0 ? tenants : homeTenant ? [homeTenant] : [];
    const matchedTenant = candidates.find((tenant) => tenant.id === storedTenantId);
    const nextTenant = matchedTenant || candidates.find((tenant) => tenant.id === homeTenant?.id) || homeTenant;

    setAvailableTenants(candidates);
    setSelectedTenant(nextTenant || null);
    persistSelectedTenant(nextTenant || homeTenant);
  };

  const loadPlatformTenants = async (usuarioData) => {
    if (!usuarioData?.isPlatformAdmin) {
      syncOperationalTenant(usuarioData);
      return;
    }

    try {
      const response = await tenantsApi.getAll();
      syncOperationalTenant(usuarioData, response.data || []);
    } catch (error) {
      console.error('Erro ao carregar tenants operacionais:', error);
      syncOperationalTenant(usuarioData);
    }
  };

  useEffect(() => {
    // Verificar se há token salvo
    const token = localStorage.getItem('token');
    const usuarioSalvo = localStorage.getItem('usuario');

    if (token && usuarioSalvo) {
      try {
        setUsuario(JSON.parse(usuarioSalvo));
        // Verificar se o token ainda é válido
        authApi.me()
          .then((res) => {
            setUsuario(res.data);
            localStorage.setItem('usuario', JSON.stringify(res.data));
            return loadPlatformTenants(res.data);
          })
          .catch(() => {
            // Token inválido, limpar
            logout();
          })
          .finally(() => setLoading(false));
      } catch {
        logout();
        setLoading(false);
      }
    } else {
      setLoading(false);
    }
  }, []);

  const login = async (email, senha) => {
    try {
      const response = await authApi.login({ email, senha });
      const { token, refreshToken, usuario: usuarioData } = response.data;

      localStorage.setItem('token', token);
      localStorage.setItem('refreshToken', refreshToken);
      localStorage.setItem('usuario', JSON.stringify(usuarioData));

      setUsuario(usuarioData);
      await loadPlatformTenants(usuarioData);
      return { success: true };
    } catch (error) {
      console.error('Erro no login:', error);
      const data = error.response?.data;
      const errorMessage = (typeof data === 'string' ? data : null) ||
                          data?.message ||
                          data?.error ||
                          'Email ou senha inválidos';
      return {
        success: false,
        message: errorMessage,
      };
    }
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('usuario');
    localStorage.removeItem(SELECTED_TENANT_ID_KEY);
    localStorage.removeItem(SELECTED_TENANT_SLUG_KEY);
    setUsuario(null);
    setAvailableTenants([]);
    setSelectedTenant(null);
  };

  const atualizarUsuario = (usuarioData) => {
    setUsuario(usuarioData);
    localStorage.setItem('usuario', JSON.stringify(usuarioData));
    loadPlatformTenants(usuarioData);
  };

  const registrarMudancaContextoOperacional = async (nextTenant, action) => {
    if (!isPlatformAdmin || !homeTenant?.id || !nextTenant?.id) return;

    await tenantsApi.auditOperationalContext({
      tenantOrigemId: currentTenant?.id || homeTenant.id,
      tenantOrigemSlug: currentTenant?.slug || homeTenant.slug,
      tenantDestinoId: nextTenant.id,
      tenantDestinoSlug: nextTenant.slug || null,
      acao: action,
    });
  };

  const atualizarTenantOperacional = async (tenantId) => {
    const nextTenant = availableTenants.find((tenant) => tenant.id === Number(tenantId));
    if (!nextTenant) return;

    await registrarMudancaContextoOperacional(nextTenant, 'EntrarTenantOperacional');
    setSelectedTenant(nextTenant);
    persistSelectedTenant(nextTenant);
  };

  const voltarParaTenantOrigem = async () => {
    if (!homeTenant?.id) return;

    await registrarMudancaContextoOperacional(homeTenant, 'VoltarTenantOrigem');
    setSelectedTenant(homeTenant);
    persistSelectedTenant(homeTenant);
  };

  const isAuthenticated = !!usuario;
  const isAdmin = Number(usuario?.tipoUsuario) === 1 || Number(usuario?.tipoUsuario) === 3;
  const isPlatformAdmin = !!usuario?.isPlatformAdmin;
  const homeTenant = usuario
    ? {
        id: usuario.tenantId,
        slug: usuario.tenantSlug,
        nome: usuario.tenantNome || usuario.tenantSlug,
        nomeExibicao: usuario.tenantNomeExibicao || usuario.tenantNome || usuario.tenantSlug,
        logoUrl: usuario.tenantLogoUrl || null,
        faviconUrl: usuario.tenantFaviconUrl || null,
        corPrimaria: usuario.tenantCorPrimaria || null,
        corSecundaria: usuario.tenantCorSecundaria || null,
        isRootTenant: !!usuario.isRootTenant,
      }
    : null;
  const currentTenant = selectedTenant || homeTenant;
  const isOperatingHomeTenant = !currentTenant?.id || currentTenant.id === homeTenant?.id;
  const operandoTenantRemoto = isPlatformAdmin && !isOperatingHomeTenant;

  const can = (resource, action = 'view') => {
    if (!usuario) return false;
    if (isPlatformAdmin) return true;
    const perm = usuario.permissoes?.find((p) => String(p.recurso).toLowerCase() === String(resource).toLowerCase());
    if (!perm) return false;
    if (action === 'view') return !!perm.podeVer;
    if (action === 'edit') return !!perm.podeEditar;
    if (action === 'delete') return !!perm.podeExcluir;
    return false;
  };

  return (
    <AuthContext.Provider
      value={{
        usuario,
        loading,
        login,
        logout,
        atualizarUsuario,
        isAuthenticated,
        isAdmin,
        isPlatformAdmin,
        homeTenant,
        currentTenant,
        isOperatingHomeTenant,
        operandoTenantRemoto,
        availableTenants,
        atualizarTenantOperacional,
        voltarParaTenantOrigem,
        can,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth deve ser usado dentro de AuthProvider');
  }
  return context;
}
