import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { lazy, Suspense } from 'react';
import { AuthProvider } from './context/AuthContext';
import { ThemeProvider } from './context/ThemeContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { RequirePermission } from './components/RequirePermission';
import { Layout } from './components/Layout/Layout';
import Login from './pages/Login/Login';
const Signup = lazy(() => import('./pages/Signup/Signup'));

const Dashboard = lazy(() => import('./pages/Dashboard'));
const MinhaAssinatura = lazy(() => import('./pages/Billing/MinhaAssinatura'));
const AdminAssinaturas = lazy(() => import('./pages/Billing/AdminAssinaturas'));
const ConfiguracoesList = lazy(() => import('./pages/ConfiguracoesMensagens/ConfiguracoesList'));
const ConfiguracaoForm = lazy(() => import('./pages/ConfiguracoesMensagens/ConfiguracaoForm'));
const MensagensAgendadas = lazy(() => import('./pages/MensagensAgendadas/MensagensAgendadas'));
const ComunicacaoCampanhasList = lazy(() => import('./pages/Comunicacao/ComunicacaoCampanhasList'));
const ComunicacaoCampanhaForm = lazy(() => import('./pages/Comunicacao/ComunicacaoCampanhaForm'));
const ComunicacaoCampanhaDetails = lazy(() => import('./pages/Comunicacao/ComunicacaoCampanhaDetails'));
const ComunicacaoTemplatesList = lazy(() => import('./pages/Comunicacao/ComunicacaoTemplatesList'));
const ComunicacaoTemplateForm = lazy(() => import('./pages/Comunicacao/ComunicacaoTemplateForm'));
const ComunicacaoSegmentosList = lazy(() => import('./pages/Comunicacao/ComunicacaoSegmentosList'));
const ComunicacaoSegmentoForm = lazy(() => import('./pages/Comunicacao/ComunicacaoSegmentoForm'));
const ComunicacaoEntregasList = lazy(() => import('./pages/Comunicacao/ComunicacaoEntregasList'));
const ContatosList = lazy(() => import('./pages/Contatos/ContatosList'));
const ContatoForm = lazy(() => import('./pages/Contatos/ContatoForm'));
const TagsList = lazy(() => import('./pages/Tags/TagsList'));
const TagForm = lazy(() => import('./pages/Tags/TagForm'));
const WhatsAppAccountsList = lazy(() => import('./pages/WhatsAppAccounts/WhatsAppAccountsList'));
const WhatsAppAccountForm = lazy(() => import('./pages/WhatsAppAccounts/WhatsAppAccountForm'));
const NotificacoesList = lazy(() => import('./pages/Notificacoes/NotificacoesList'));
const UsuariosList = lazy(() => import('./pages/Usuarios/UsuariosList'));
const AuditoriaList = lazy(() => import('./pages/Auditoria/AuditoriaList'));
const OperacaoDashboard = lazy(() => import('./pages/Operacao/OperacaoDashboard'));
const Perfil = lazy(() => import('./pages/Perfil/Perfil'));
const PerfisAcessoList = lazy(() => import('./pages/PerfisAcesso/PerfisAcessoList'));
const PerfilAcessoForm = lazy(() => import('./pages/PerfisAcesso/PerfilAcessoForm'));
const TenantsPage = lazy(() => import('./pages/Platform/TenantsPage'));
const TenantDetailsPage = lazy(() => import('./pages/Platform/TenantDetailsPage'));
import { RESOURCES, ACTIONS } from './utils/permissions';
import './App.css';
import i18n from './i18n';

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <Router>
        <Suspense fallback={<div className="p-6 text-muted-foreground">{i18n.t('common.loading')}</div>}>
        <Routes>
          {/* Rotas públicas */}
          <Route path="/login" element={<Login />} />
          <Route path="/signup" element={<Signup />} />

          {/* Rotas protegidas */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            <Route index element={
              <RequirePermission resource={RESOURCES.DASHBOARD}>
                <Dashboard />
              </RequirePermission>
            } />

          <Route path="billing" element={<MinhaAssinatura />} />
          <Route path="admin/assinaturas" element={<AdminAssinaturas />} />

          {/* Contatos */}
          <Route path="contatos" element={
            <RequirePermission resource={RESOURCES.CONTATOS}>
              <ContatosList />
            </RequirePermission>
          } />
          <Route path="contatos/novo" element={
            <RequirePermission resource={RESOURCES.CONTATOS} action={ACTIONS.EDIT}>
              <ContatoForm />
            </RequirePermission>
          } />
          <Route path="contatos/:id/editar" element={
            <RequirePermission resource={RESOURCES.CONTATOS} action={ACTIONS.EDIT}>
              <ContatoForm />
            </RequirePermission>
          } />

          {/* Tags */}
          <Route path="tags" element={
            <RequirePermission resource={RESOURCES.CONTATOS}>
              <TagsList />
            </RequirePermission>
          } />
          <Route path="tags/novo" element={
            <RequirePermission resource={RESOURCES.CONTATOS} action={ACTIONS.EDIT}>
              <TagForm />
            </RequirePermission>
          } />
          <Route path="tags/:id/editar" element={
            <RequirePermission resource={RESOURCES.CONTATOS} action={ACTIONS.EDIT}>
              <TagForm />
            </RequirePermission>
          } />

          {/* Configurações de Mensagens */}
          <Route path="configuracoes-mensagens" element={
            <RequirePermission resource={RESOURCES.CONFIG_MENSAGENS}>
              <ConfiguracoesList />
            </RequirePermission>
          } />
          <Route path="configuracoes-mensagens/novo" element={
            <RequirePermission resource={RESOURCES.CONFIG_MENSAGENS} action={ACTIONS.EDIT}>
              <ConfiguracaoForm />
            </RequirePermission>
          } />
          <Route path="configuracoes-mensagens/editar/:id" element={
            <RequirePermission resource={RESOURCES.CONFIG_MENSAGENS} action={ACTIONS.EDIT}>
              <ConfiguracaoForm />
            </RequirePermission>
          } />

          {/* Conta WhatsApp */}
          <Route path="whatsapp/contas" element={
            <RequirePermission resource={RESOURCES.CONFIG_MENSAGENS}>
              <WhatsAppAccountsList />
            </RequirePermission>
          } />
          <Route path="whatsapp/contas/novo" element={
            <RequirePermission resource={RESOURCES.CONFIG_MENSAGENS} action={ACTIONS.EDIT}>
              <WhatsAppAccountForm />
            </RequirePermission>
          } />
          <Route path="whatsapp/contas/:id/editar" element={
            <RequirePermission resource={RESOURCES.CONFIG_MENSAGENS} action={ACTIONS.EDIT}>
              <WhatsAppAccountForm />
            </RequirePermission>
          } />

          {/* Mensagens Agendadas */}
          <Route path="mensagens-agendadas" element={
            <RequirePermission resource={RESOURCES.MENSAGENS_AGENDADAS}>
              <MensagensAgendadas />
            </RequirePermission>
          } />

          {/* Comunicação */}
          <Route path="comunicacao/campanhas" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO}>
              <ComunicacaoCampanhasList />
            </RequirePermission>
          } />
          <Route path="comunicacao/campanhas/nova" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO} action={ACTIONS.EDIT}>
              <ComunicacaoCampanhaForm />
            </RequirePermission>
          } />
          <Route path="comunicacao/campanhas/:id" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO}>
              <ComunicacaoCampanhaDetails />
            </RequirePermission>
          } />
          <Route path="comunicacao/templates" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO}>
              <ComunicacaoTemplatesList />
            </RequirePermission>
          } />
          <Route path="comunicacao/templates/novo" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO} action={ACTIONS.EDIT}>
              <ComunicacaoTemplateForm />
            </RequirePermission>
          } />
          <Route path="comunicacao/templates/:id/editar" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO} action={ACTIONS.EDIT}>
              <ComunicacaoTemplateForm />
            </RequirePermission>
          } />
          <Route path="comunicacao/segmentos" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO}>
              <ComunicacaoSegmentosList />
            </RequirePermission>
          } />
          <Route path="comunicacao/segmentos/novo" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO} action={ACTIONS.EDIT}>
              <ComunicacaoSegmentoForm />
            </RequirePermission>
          } />
          <Route path="comunicacao/segmentos/:id/editar" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO} action={ACTIONS.EDIT}>
              <ComunicacaoSegmentoForm />
            </RequirePermission>
          } />
          <Route path="comunicacao/logs" element={
            <RequirePermission resource={RESOURCES.COMUNICACAO}>
              <ComunicacaoEntregasList />
            </RequirePermission>
          } />

          <Route path="notificacoes" element={<NotificacoesList />} />

          {/* Administração */}
          <Route path="usuarios" element={
            <RequirePermission resource={RESOURCES.USUARIOS} requireAdmin>
              <UsuariosList />
            </RequirePermission>
          } />
          <Route path="auditoria" element={
            <RequirePermission resource={RESOURCES.AUDITORIA} requireAdmin>
              <AuditoriaList />
            </RequirePermission>
          } />
          <Route path="operacao" element={
            <RequirePermission resource={RESOURCES.AUDITORIA} requireAdmin>
              <OperacaoDashboard />
            </RequirePermission>
          } />
          <Route path="perfis-acesso" element={
            <RequirePermission resource={RESOURCES.PERFIS_ACESSO} requireAdmin>
              <PerfisAcessoList />
            </RequirePermission>
          } />
          <Route path="perfis-acesso/novo" element={
            <RequirePermission resource={RESOURCES.PERFIS_ACESSO} action={ACTIONS.EDIT} requireAdmin>
              <PerfilAcessoForm />
            </RequirePermission>
          } />
          <Route path="perfis-acesso/:id/editar" element={
            <RequirePermission resource={RESOURCES.PERFIS_ACESSO} action={ACTIONS.EDIT} requireAdmin>
              <PerfilAcessoForm />
            </RequirePermission>
          } />

          {/* Plataforma */}
          <Route path="plataforma/tenants" element={<TenantsPage />} />
          <Route path="plataforma/tenants/:id" element={<TenantDetailsPage />} />

          {/* Perfil do usuário */}
          <Route path="perfil" element={<Perfil />} />
        </Route>

        {/* Redireciona rotas não encontradas */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      </Suspense>
        </Router>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
