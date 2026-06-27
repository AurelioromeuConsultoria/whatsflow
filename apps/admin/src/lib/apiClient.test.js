import { api } from '@/lib/apiClient';

// Lê um header do config independente de ser objeto simples ou AxiosHeaders.
function header(config, name) {
  const h = config.headers;
  return typeof h?.get === 'function' ? h.get(name) : h?.[name];
}

// Adapter que ecoa o config (após passar pelos interceptors de request).
function useEchoAdapter() {
  api.defaults.adapter = async (config) => ({
    data: config,
    status: 200,
    statusText: 'OK',
    headers: {},
    config,
  });
}

beforeEach(() => {
  localStorage.clear();
  useEchoAdapter();
});

describe('apiClient request interceptor', () => {
  it('injeta Authorization quando há token', async () => {
    localStorage.setItem('token', 'abc123');
    const resp = await api.get('/pessoas');
    expect(header(resp.data, 'Authorization')).toBe('Bearer abc123');
  });

  it('não injeta Authorization sem token', async () => {
    const resp = await api.get('/pessoas');
    expect(header(resp.data, 'Authorization')).toBeFalsy();
  });

  it('envia X-Tenant-Id/Slug para platform admin com tenant selecionado', async () => {
    localStorage.setItem('token', 'abc');
    localStorage.setItem('usuario', JSON.stringify({ isPlatformAdmin: true }));
    localStorage.setItem('selectedTenantId', '7');
    localStorage.setItem('selectedTenantSlug', 'igreja-x');

    const resp = await api.get('/pessoas');

    expect(header(resp.data, 'X-Tenant-Id')).toBe('7');
    expect(header(resp.data, 'X-Tenant-Slug')).toBe('igreja-x');
  });

  it('NÃO envia override de tenant em requests de auth (usa o tenant do token)', async () => {
    localStorage.setItem('token', 'abc');
    localStorage.setItem('usuario', JSON.stringify({ isPlatformAdmin: true }));
    localStorage.setItem('selectedTenantId', '7');

    const resp = await api.get('/auth/me');

    expect(header(resp.data, 'X-Tenant-Id')).toBeFalsy();
  });

  it('não envia override de tenant para usuário não-admin', async () => {
    localStorage.setItem('token', 'abc');
    localStorage.setItem('usuario', JSON.stringify({ isPlatformAdmin: false }));
    localStorage.setItem('selectedTenantId', '7');

    const resp = await api.get('/pessoas');

    expect(header(resp.data, 'X-Tenant-Id')).toBeFalsy();
  });
});
