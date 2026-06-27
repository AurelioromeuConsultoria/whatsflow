import { Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/context/AuthContext';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';

export function RequirePermission({ resource, action = 'view', requireAdmin = false, children }) {
  const { t } = useTranslation();
  const { loading, isAuthenticated, isAdmin, can } = useAuth();

  if (loading) return <LoadingPage text={t('permissions.checking')} />;
  if (!isAuthenticated) return <Navigate to="/login" replace />;

  if (requireAdmin && !isAdmin) {
    return <ErrorPage message={t('permissions.adminOnly')} />;
  }

  if (requireAdmin && isAdmin) {
    return children;
  }

  if (!can(resource, action)) {
    return <ErrorPage message={t('permissions.noAccess')} />;
  }

  return children;
}
