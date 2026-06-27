import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Activity, ArrowLeft, ArrowRightLeft, Building2, Globe, Palette, RefreshCw, Shield, Trash2, TriangleAlert, Users } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { useAuth } from '@/context/AuthContext';
import { auditLogsApi, tenantsApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Progress } from '@/components/ui/progress';
import { buildTenantOnboardingChecklist, deriveTenantOperationalStatus, hasTenantOperationalContract } from './platformTenantHelpers';

function StatusBadge({ ativo, t }) {
  return <Badge variant={ativo ? 'default' : 'secondary'}>{t(ativo ? 'platformTenants.status.active' : 'platformTenants.status.inactive')}</Badge>;
}

function toAuditDateInput(date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function buildAuditLink(patch = {}) {
  const now = new Date();
  const weekAgo = new Date(now);
  weekAgo.setDate(now.getDate() - 7);

  const params = new URLSearchParams({
    createdAt_from: toAuditDateInput(weekAgo),
    createdAt_to: toAuditDateInput(now),
  });

  Object.entries(patch).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return;
    params.set(key, String(value));
  });

  return `/auditoria?${params.toString()}`;
}

export default function TenantDetailsPage() {
  const { t } = useTranslation();
  const { id } = useParams();
  const navigate = useNavigate();
  const {
    isPlatformAdmin,
    currentTenant,
    homeTenant,
    isOperatingHomeTenant,
    atualizarTenantOperacional,
    voltarParaTenantOrigem,
  } = useAuth();
  const confirmDialog = useConfirmDialog();
  const [tenant, setTenant] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [auditMetrics, setAuditMetrics] = useState({
    totalLogs: 0,
    criticalActions: 0,
    failureActions: 0,
    distinctUsers: 0,
    topActionName: '-',
    topEntityName: '-',
  });
  const [auditLoading, setAuditLoading] = useState(false);
  const [auditError, setAuditError] = useState(null);
  const [adminTrail, setAdminTrail] = useState([]);
  const [adminTrailLoading, setAdminTrailLoading] = useState(false);
  const [adminTrailError, setAdminTrailError] = useState(null);

  const getOperationalStatusLabel = (statusKey) => {
    const keyMap = {
      inativo: 'inactive',
      'homologacao-inicial': 'initialValidation',
      onboarding: 'onboarding',
      provisionado: 'provisioned',
      rascunho: 'draft',
    };

    return t(`platformTenants.operationalStatus.${keyMap[statusKey] || 'draft'}`);
  };

  const getChecklistLabel = (itemId) => t(`platformTenants.details.checklist.items.${itemId}`);

  const getAdminTrailActionLabel = (action) => {
    const labels = {
      ProvisionarTenant: 'Provisionamento do tenant',
      AtualizarTenant: 'Atualização do tenant',
      AtivarTenant: 'Ativação do tenant',
      InativarTenant: 'Inativação do tenant',
      ExcluirTenant: 'Exclusão do tenant',
      EntrarTenantOperacional: 'Entrada em tenant operacional',
      VoltarTenantOrigem: 'Retorno ao tenant de origem',
    };

    return labels[action] || action;
  };

  const loadTenant = useCallback(async ({ silent = false } = {}) => {
    if (!isPlatformAdmin || !id) {
      setLoading(false);
      return;
    }

    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const response = await tenantsApi.getById(id);
      setTenant(response.data || null);
    } catch (err) {
      setError(getApiErrorMessage(err, t('platformTenants.details.errorLoad')));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [id, isPlatformAdmin]);

  useEffect(() => {
    loadTenant();
  }, [loadTenant]);

  const loadAuditMetrics = useCallback(async () => {
    if (!isPlatformAdmin || !tenant?.id || tenant.id !== currentTenant?.id) {
      setAuditMetrics({
        totalLogs: 0,
        criticalActions: 0,
        failureActions: 0,
        distinctUsers: 0,
        topActionName: '-',
        topEntityName: '-',
      });
      setAuditError(null);
      return;
    }

    try {
      setAuditLoading(true);
      setAuditError(null);
      const now = new Date();
      const weekAgo = new Date(now);
      weekAgo.setDate(now.getDate() - 7);

      const response = await auditLogsApi.getMetrics({
        from: weekAgo.toISOString(),
        to: now.toISOString(),
      });

      setAuditMetrics({
        totalLogs: Number(response.data?.totalLogs || 0),
        criticalActions: Number(response.data?.criticalActions || 0),
        failureActions: Number(response.data?.failureActions || 0),
        distinctUsers: Number(response.data?.distinctUsers || 0),
        topActionName: response.data?.topActionName || '-',
        topEntityName: response.data?.topEntityName || '-',
      });
    } catch (err) {
      setAuditError(getApiErrorMessage(err, t('platformTenants.details.audit.errorLoad')));
    } finally {
      setAuditLoading(false);
    }
  }, [currentTenant?.id, isPlatformAdmin, tenant?.id]);

  useEffect(() => {
    loadAuditMetrics();
  }, [loadAuditMetrics]);

  const loadAdministrativeTrail = useCallback(async () => {
    if (!isPlatformAdmin || !tenant?.id) {
      setAdminTrail([]);
      setAdminTrailError(null);
      return;
    }

    try {
      setAdminTrailLoading(true);
      setAdminTrailError(null);
      const response = await tenantsApi.getAdministrativeAuditTrail(tenant.id);
      setAdminTrail(response.data?.items || []);
    } catch (err) {
      setAdminTrailError(getApiErrorMessage(err, 'Erro ao carregar trilha administrativa do tenant.'));
    } finally {
      setAdminTrailLoading(false);
    }
  }, [isPlatformAdmin, tenant?.id]);

  useEffect(() => {
    loadAdministrativeTrail();
  }, [loadAdministrativeTrail]);

  const handleOperateTenant = () => {
    if (!tenant?.id) return;
    if (tenant.id === currentTenant?.id) {
      navigate('/');
      return;
    }

    const isRemoteTarget = tenant.id !== homeTenant?.id;
    if (!isRemoteTarget) {
      voltarParaTenantOrigem()
        .then(() => {
          navigate('/');
          window.location.reload();
        })
        .catch((error) => {
          console.error('Erro ao registrar retorno ao tenant de origem', error);
          toast.error('Não foi possível registrar o retorno ao tenant de origem.');
        });
      return;
    }

    confirmDialog.show({
      title: t('platformTenants.details.operate.title'),
      description: t('platformTenants.details.operate.description', {
        name: tenant.nomeExibicao || tenant.nome || tenant.slug,
      }),
      confirmText: t('platformTenants.details.operate.confirm'),
      cancelText: t('actions.cancel'),
      onConfirm: async () => {
        try {
          await atualizarTenantOperacional(tenant.id);
          navigate('/');
          window.location.reload();
        } catch (error) {
          console.error('Erro ao registrar troca de contexto operacional', error);
          toast.error('Não foi possível registrar a troca de tenant.');
        }
      },
    });
  };

  const handleBackToHomeTenant = async () => {
    if (!homeTenant?.id || isOperatingHomeTenant) return;

    try {
      await voltarParaTenantOrigem();
      navigate('/');
      window.location.reload();
    } catch (error) {
      console.error('Erro ao registrar retorno ao tenant de origem', error);
      toast.error('Não foi possível registrar o retorno ao tenant de origem.');
    }
  };

  const handleToggleStatus = () => {
    if (!tenant?.id) return;

    const nextActiveState = !tenant.ativo;
    confirmDialog.show({
      title: nextActiveState ? t('platformTenants.details.safeActions.activate') : t('platformTenants.details.safeActions.deactivate'),
      description: nextActiveState
        ? t('platformTenants.details.safeActions.activateDescription', { name: tenant.nomeExibicao || tenant.nome })
        : t('platformTenants.details.safeActions.deactivateDescription', { name: tenant.nomeExibicao || tenant.nome }),
      confirmText: nextActiveState ? t('platformTenants.details.safeActions.activate') : t('platformTenants.details.safeActions.deactivate'),
      cancelText: t('actions.cancel'),
      onConfirm: async () => {
        await tenantsApi.updateStatus(tenant.id, nextActiveState);
        toast.success(
          t(nextActiveState ? 'platformTenants.status.activatedToast' : 'platformTenants.status.deactivatedToast', {
            name: tenant.nome,
          }),
        );
        await loadTenant({ silent: true });
      },
    });
  };

  const handleDeleteTenant = () => {
    if (!tenant?.id || tenant.canDelete === false) return;

    confirmDialog.show({
      title: t('platformTenants.delete.title'),
      description: t('platformTenants.delete.description', { name: tenant.nome }),
      confirmText: t('platformTenants.delete.confirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        await tenantsApi.delete(tenant.id);
        toast.success(t('platformTenants.delete.successToast', { name: tenant.nome }));
        navigate('/plataforma/tenants');
      },
    });
  };

  if (!isPlatformAdmin) {
    return <ErrorPage message={t('platformTenants.restricted')} />;
  }

  if (loading) return <LoadingPage text={t('platformTenants.details.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={() => loadTenant()} />;
  if (!tenant) return <ErrorPage message={t('platformTenants.details.notFound')} />;

  const isCurrentTenant = tenant.id === currentTenant?.id;
  const isHomeTenant = tenant.id === homeTenant?.id;
  const onboarding = buildTenantOnboardingChecklist(tenant);
  const operationalStatus = deriveTenantOperationalStatus(tenant);
  const hasOperationalContract = hasTenantOperationalContract(tenant);
  const auditNeedsContext = !isCurrentTenant;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to="/plataforma/tenants">
                <ArrowLeft className="mr-2 h-4 w-4" />
                {t('platformTenants.details.backToList')}
              </Link>
            </Button>
            <StatusBadge ativo={tenant.ativo} t={t} />
            <Badge variant={operationalStatus.tone}>{getOperationalStatusLabel(operationalStatus.key)}</Badge>
            {tenant.isRootTenant && <Badge variant="outline">{t('platformTenants.status.root')}</Badge>}
            {isCurrentTenant && <Badge variant="secondary">{t('platformTenants.details.badges.operatingNow')}</Badge>}
            {isHomeTenant && <Badge variant="outline">{t('platformTenants.details.badges.homeTenant')}</Badge>}
          </div>

          <div>
            <h1 className="text-3xl font-bold">{tenant.nomeExibicao || tenant.nome}</h1>
            <p className="mt-1 text-muted-foreground">
              {t('platformTenants.details.headerSubtitle', {
                slug: tenant.slug,
                domain: tenant.dominioPrimario || t('platformTenants.details.notConfigured'),
              })}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" onClick={() => loadTenant({ silent: true })} disabled={refreshing}>
            <RefreshCw className={`mr-2 h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
            {t('actions.refresh')}
          </Button>
          <Button variant="outline" onClick={handleOperateTenant}>
            <ArrowRightLeft className="mr-2 h-4 w-4" />
            {isCurrentTenant
              ? t('platformTenants.details.operate.goToCurrent')
              : isHomeTenant
                ? t('platformTenants.details.operate.backToHome')
                : t('platformTenants.details.operate.button')}
          </Button>
          {!isOperatingHomeTenant && (
            <Button variant="outline" onClick={handleBackToHomeTenant}>
              {t('platformTenants.details.backToOrigin')}
            </Button>
          )}
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.details.metrics.users')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{tenant.totalUsuarios || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.details.metrics.localAdmins')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{tenant.totalAdministradores || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.details.metrics.people')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{tenant.totalPessoas || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.details.metrics.onboarding')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="text-3xl font-bold">{onboarding.progress}%</div>
              <div className="text-sm text-muted-foreground">
                {t('platformTenants.details.metrics.onboardingSummary', {
                  completed: onboarding.completed,
                  total: onboarding.total,
                })}
              </div>
              {!hasOperationalContract && (
                <div className="text-xs text-amber-600">
                  {t('platformTenants.details.metrics.localSummary')}
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.2fr_0.8fr]">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Building2 className="h-4 w-4" />
              {t('platformTenants.details.sections.identity')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.details.fields.internalName')}</div>
              <div className="mt-1 font-medium">{tenant.nome || '-'}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.details.fields.displayName')}</div>
              <div className="mt-1 font-medium">{tenant.nomeExibicao || '-'}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.fields.slug')}</div>
              <div className="mt-1 font-medium">{tenant.slug || '-'}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.details.fields.createdAt')}</div>
              <div className="mt-1 font-medium">{tenant.dataCriacao ? formatDateTime(tenant.dataCriacao) : '-'}</div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Globe className="h-4 w-4" />
              {t('platformTenants.details.sections.domainStatus')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.fields.primaryDomain')}</div>
              <div className="mt-1 font-medium">{tenant.dominioPrimario || t('platformTenants.details.notConfigured')}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.details.fields.operationalStatus')}</div>
              <div className="mt-2 flex flex-wrap items-center gap-2">
                <StatusBadge ativo={tenant.ativo} t={t} />
                <Badge variant={operationalStatus.tone}>{getOperationalStatusLabel(operationalStatus.key)}</Badge>
                {isCurrentTenant && <Badge variant="secondary">{t('platformTenants.details.badges.operatingThisTenant')}</Badge>}
                {isHomeTenant && <Badge variant="outline">{t('platformTenants.details.badges.sessionOrigin')}</Badge>}
              </div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.table.lastActivity')}</div>
              <div className="mt-1 font-medium">
                {tenant.ultimaAtividadeEm ? formatDateTime(tenant.ultimaAtividadeEm) : t('platformTenants.details.noActivity')}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-[0.8fr_1.2fr]">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Palette className="h-4 w-4" />
              {t('platformTenants.provision.steps.branding.title')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-4">
              {tenant.logoUrl ? (
                <img
                  src={tenant.logoUrl}
                  alt={t('platformTenants.alt.logoWithName', { name: tenant.nome })}
                  className="h-20 w-20 rounded-xl border bg-white object-contain p-2"
                />
              ) : (
                <div className="flex h-20 w-20 items-center justify-center rounded-xl border border-dashed text-xs text-muted-foreground">
                  {t('platformTenants.logoUpload.noLogo')}
                </div>
              )}
              <div className="space-y-2">
                <div className="flex gap-2">
                  <span className="rounded-full px-3 py-1 text-xs font-medium text-white" style={{ background: tenant.corPrimaria || '#111827' }}>
                    {t('platformTenants.preview.primary')}
                  </span>
                  <span className="rounded-full px-3 py-1 text-xs font-medium text-white" style={{ background: tenant.corSecundaria || '#374151' }}>
                    {t('platformTenants.preview.secondary')}
                  </span>
                </div>
                <div className="text-sm text-muted-foreground">{t('platformTenants.details.faviconLabel', { value: tenant.faviconUrl || t('platformTenants.details.notConfigured') })}</div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="h-4 w-4" />
              {t('platformTenants.details.sections.onboardingRead')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            <div className="rounded-xl border bg-muted/30 p-4">
              <div className="font-medium">{t('platformTenants.details.checklist.title')}</div>
              <div className="mt-1 text-xs text-muted-foreground">
                {t('platformTenants.details.checklist.summary', {
                  status: getOperationalStatusLabel(operationalStatus.key),
                  completed: onboarding.completed,
                  total: onboarding.total,
                })}
              </div>
              <div className="mt-3 space-y-3">
                <Progress value={onboarding.progress} />
                <div className="space-y-2">
                  {onboarding.items.map((item) => (
                    <div key={item.id} className="flex items-center justify-between gap-3 rounded-lg border bg-background px-3 py-2">
                      <span className="text-muted-foreground">{getChecklistLabel(item.id)}</span>
                      <Badge variant={item.done ? 'default' : 'secondary'}>
                        {t(item.done ? 'platformTenants.details.checklist.completed' : 'platformTenants.details.checklist.pending')}
                      </Badge>
                    </div>
                  ))}
                </div>
              </div>
            </div>
            <div className="rounded-xl border bg-muted/30 p-4">
              <div className="font-medium">{t('platformTenants.details.sessionContext.title')}</div>
              <div className="mt-2 space-y-1 text-muted-foreground">
                <div>{t('platformTenants.details.sessionContext.homeTenant', { value: homeTenant?.nomeExibicao || homeTenant?.nome || homeTenant?.slug || t('platformTenants.notAvailable') })}</div>
                <div>{t('platformTenants.details.sessionContext.currentTenant', { value: currentTenant?.nomeExibicao || currentTenant?.nome || currentTenant?.slug || t('platformTenants.notAvailable') })}</div>
              </div>
            </div>
            <div className="rounded-xl border bg-muted/30 p-4">
              <div className="font-medium">{t('platformTenants.details.nextSteps.title')}</div>
              <ul className="mt-2 space-y-1 text-muted-foreground">
                <li>{t('platformTenants.details.nextSteps.items.branding')}</li>
                <li>{t('platformTenants.details.nextSteps.items.adminAccess')}</li>
                <li>{t('platformTenants.details.nextSteps.items.onboarding')}</li>
              </ul>
            </div>
            <div className="rounded-xl border bg-muted/30 p-4">
              <div className="flex items-center gap-2 font-medium">
                <Activity className="h-4 w-4" />
                {t('platformTenants.details.audit.title')}
              </div>
              {auditNeedsContext ? (
                <div className="mt-3 space-y-3">
                  <div className="rounded-lg border border-amber-500/20 bg-amber-500/5 p-3 text-sm text-muted-foreground">
                    {t('platformTenants.details.audit.needsContext')}
                  </div>
                  <Button variant="outline" onClick={handleOperateTenant}>
                    <ArrowRightLeft className="mr-2 h-4 w-4" />
                    {t('platformTenants.details.audit.operateToInvestigate')}
                  </Button>
                </div>
              ) : (
                <div className="mt-3 space-y-4">
                  <div className="grid gap-3 md:grid-cols-2">
                    <div className="rounded-lg border bg-background p-3">
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.details.audit.metrics.logs')}</div>
                      <div className="mt-2 text-2xl font-bold">{auditLoading ? '...' : auditMetrics.totalLogs}</div>
                    </div>
                    <div className="rounded-lg border bg-background p-3">
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('platformTenants.details.audit.metrics.distinctUsers')}</div>
                      <div className="mt-2 text-2xl font-bold">{auditLoading ? '...' : auditMetrics.distinctUsers}</div>
                    </div>
                    <div className="rounded-lg border bg-background p-3">
                      <div className="flex items-center gap-2 text-xs uppercase tracking-wide text-muted-foreground">
                        <TriangleAlert className="h-3.5 w-3.5" />
                        {t('platformTenants.details.audit.metrics.criticalActions')}
                      </div>
                      <div className="mt-2 text-2xl font-bold">{auditLoading ? '...' : auditMetrics.criticalActions}</div>
                    </div>
                    <div className="rounded-lg border bg-background p-3">
                      <div className="flex items-center gap-2 text-xs uppercase tracking-wide text-muted-foreground">
                        <Shield className="h-3.5 w-3.5" />
                        {t('platformTenants.details.audit.metrics.failures')}
                      </div>
                      <div className="mt-2 text-2xl font-bold">{auditLoading ? '...' : auditMetrics.failureActions}</div>
                    </div>
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {t('platformTenants.details.audit.summary', {
                      action: auditMetrics.topActionName,
                      entity: auditMetrics.topEntityName,
                    })}
                  </div>
                  {auditError && (
                    <div className="rounded-lg border border-destructive/20 bg-destructive/5 p-3 text-xs text-destructive">
                      {auditError}
                    </div>
                  )}
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" size="sm" asChild>
                      <Link to={buildAuditLink()}>
                        {t('platformTenants.details.audit.links.openAudit')}
                      </Link>
                    </Button>
                    <Button variant="outline" size="sm" asChild>
                      <Link to={buildAuditLink({ action: 'Login', entityName: 'Auth' })}>
                        {t('platformTenants.details.audit.links.logins')}
                      </Link>
                    </Button>
                    <Button variant="outline" size="sm" asChild>
                      <Link to={buildAuditLink({ action: 'AlterarSenha', entityName: 'Usuario' })}>
                        {t('platformTenants.details.audit.links.passwordChanges')}
                      </Link>
                    </Button>
                    <Button variant="outline" size="sm" asChild>
                      <Link to={buildAuditLink({ action: 'ErroEnvio', entityName: 'MensagemAgendada' })}>
                        {t('platformTenants.details.audit.links.deliveryErrors')}
                      </Link>
                    </Button>
                    <Button variant="outline" size="sm" asChild>
                      <Link to={buildAuditLink({ entityName: 'Tenant', entityId: tenant.id })}>
                        {t('platformTenants.details.audit.links.adminTrail')}
                      </Link>
                    </Button>
                  </div>
                </div>
              )}
            </div>
            <div className="rounded-xl border bg-muted/30 p-4">
              <div className="font-medium">Trilha administrativa recente</div>
              <div className="mt-1 text-xs text-muted-foreground">
                Eventos de governança registrados para este tenant pelo backoffice da plataforma.
              </div>
              <div className="mt-3 space-y-3">
                {adminTrailLoading ? (
                  <div className="text-sm text-muted-foreground">Carregando trilha administrativa...</div>
                ) : adminTrailError ? (
                  <div className="rounded-lg border border-destructive/20 bg-destructive/5 p-3 text-xs text-destructive">
                    {adminTrailError}
                  </div>
                ) : adminTrail.length === 0 ? (
                  <div className="text-sm text-muted-foreground">Nenhum evento administrativo encontrado para este tenant.</div>
                ) : (
                  adminTrail.map((item) => (
                    <div key={item.id} className="rounded-lg border bg-background px-3 py-3">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <div className="font-medium">{getAdminTrailActionLabel(item.action)}</div>
                        <div className="text-xs text-muted-foreground">{formatDateTime(item.createdAt)}</div>
                      </div>
                      <div className="mt-1 text-xs text-muted-foreground">
                        {item.userName || item.userEmail || 'Sistema'}
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
            <div className="rounded-xl border bg-muted/30 p-4">
              <div className="font-medium">{t('platformTenants.details.safeActions.title')}</div>
              <div className="mt-3 flex flex-wrap gap-2">
                <Button variant="outline" onClick={handleOperateTenant}>
                  <ArrowRightLeft className="mr-2 h-4 w-4" />
                  {isCurrentTenant ? t('platformTenants.details.operate.goToCurrent') : t('platformTenants.details.operate.button')}
                </Button>
                <Button
                  variant="outline"
                  onClick={handleToggleStatus}
                  disabled={tenant.canDeactivate === false && tenant.ativo}
                >
                  {tenant.ativo ? t('platformTenants.details.safeActions.deactivate') : t('platformTenants.details.safeActions.activate')}
                </Button>
                {tenant.canDelete !== false && (
                  <Button variant="outline" className="text-destructive" onClick={handleDeleteTenant}>
                    <Trash2 className="mr-2 h-4 w-4" />
                    {t('platformTenants.delete.confirm')}
                  </Button>
                )}
              </div>
              <div className="mt-3 text-xs text-muted-foreground">
                {t('platformTenants.details.safeActions.hint')}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={(open) => {
          if (!open) confirmDialog.hide();
        }}
        onConfirm={confirmDialog.handleConfirm}
        title={confirmDialog.config.title}
        description={confirmDialog.config.description}
        confirmText={confirmDialog.config.confirmText}
        cancelText={confirmDialog.config.cancelText}
        variant={confirmDialog.config.variant}
        loading={confirmDialog.loading}
      />
    </div>
  );
}
