import { useCallback, useEffect, useMemo, useState } from 'react';
import { Shield, Eye, Search, ArrowUpRight, TriangleAlert } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { AdvancedSearch } from '@/components/ui/advanced-search';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { auditLogsApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';

function getEntityOptions(t) {
  return [
    { value: 'Auth', label: t('audit.entities.Auth') },
    { value: 'CampanhaAniversario', label: t('audit.entities.CampanhaAniversario') },
    { value: 'CampanhaAniversarioEnvio', label: t('audit.entities.CampanhaAniversarioEnvio') },
    { value: 'Escala', label: t('audit.entities.Escala') },
    { value: 'EscalaItem', label: t('audit.entities.EscalaItem') },
    { value: 'SolicitacaoTrocaEscala', label: t('audit.entities.SolicitacaoTrocaEscala') },
    { value: 'MensagemAgendada', label: t('audit.entities.MensagemAgendada') },
    { value: 'Pessoa', label: t('audit.entities.Pessoa') },
    { value: 'PessoaPerfil', label: t('audit.entities.PessoaPerfil') },
    { value: 'Visitante', label: t('audit.entities.Visitante') },
    { value: 'Evento', label: t('audit.entities.Evento') },
    { value: 'Noticia', label: t('audit.entities.Noticia') },
    { value: 'Usuario', label: t('audit.entities.Usuario') },
  ];
}

function getActionOptions(t) {
  return [
    { value: 'Create', label: t('audit.actionsMap.Create') },
    { value: 'Update', label: t('audit.actionsMap.Update') },
    { value: 'Delete', label: t('audit.actionsMap.Delete') },
    { value: 'Login', label: t('audit.actionsMap.Login') },
    { value: 'RefreshToken', label: t('audit.actionsMap.RefreshToken') },
    { value: 'AlterarSenha', label: t('audit.actionsMap.AlterarSenha') },
    { value: 'Publicar', label: t('audit.actionsMap.Publicar') },
    { value: 'GerarAutomatico', label: t('audit.actionsMap.GerarAutomatico') },
    { value: 'Confirmar', label: t('audit.actionsMap.Confirmar') },
    { value: 'Recusar', label: t('audit.actionsMap.Recusar') },
    { value: 'RegistrarPresenca', label: t('audit.actionsMap.RegistrarPresenca') },
    { value: 'Aprovar', label: t('audit.actionsMap.Aprovar') },
    { value: 'Rejeitar', label: t('audit.actionsMap.Rejeitar') },
    { value: 'Regerar', label: t('audit.actionsMap.Regerar') },
    { value: 'ProntaParaEnvio', label: t('audit.actionsMap.ProntaParaEnvio') },
    { value: 'Enviada', label: t('audit.actionsMap.Enviada') },
    { value: 'ErroEnvio', label: t('audit.actionsMap.ErroEnvio') },
    { value: 'AtualizarConfiguracao', label: t('audit.actionsMap.AtualizarConfiguracao') },
    { value: 'EnviarTeste', label: t('audit.actionsMap.EnviarTeste') },
    { value: 'Reenviar', label: t('audit.actionsMap.Reenviar') },
    { value: 'ProcessarDia', label: t('audit.actionsMap.ProcessarDia') },
  ];
}

const QUICK_ACTIONS = ['Login', 'AlterarSenha', 'Publicar', 'Confirmar', 'Recusar', 'Aprovar', 'Rejeitar', 'ErroEnvio', 'ProcessarDia'];
const DEFAULT_FILTERS = {
  search: '',
  entityName: undefined,
  entityId: '',
  action: undefined,
  userName: '',
  userEmail: '',
  createdAt_from: '',
  createdAt_to: '',
};

function normalizeFilterValue(value) {
  return value == null ? '' : String(value);
}

function parseFiltersFromSearchParams(searchParams) {
  return {
    search: normalizeFilterValue(searchParams.get('search')),
    entityName: searchParams.get('entityName') || undefined,
    entityId: normalizeFilterValue(searchParams.get('entityId')),
    action: searchParams.get('action') || undefined,
    userName: normalizeFilterValue(searchParams.get('userName')),
    userEmail: normalizeFilterValue(searchParams.get('userEmail')),
    createdAt_from: normalizeFilterValue(searchParams.get('createdAt_from') || searchParams.get('from')),
    createdAt_to: normalizeFilterValue(searchParams.get('createdAt_to') || searchParams.get('to')),
  };
}

function areFiltersEqual(left, right) {
  return Object.keys(DEFAULT_FILTERS).every((key) => (left?.[key] ?? '') === (right?.[key] ?? ''));
}

function buildSearchParamsFromFilters(filters) {
  const nextParams = new URLSearchParams();

  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      nextParams.set(key, value);
    }
  });

  return nextParams;
}

function getActionLabel(action, t) {
  return t(`audit.actionsMap.${action}`, { defaultValue: action });
}

function getActionVariant(action) {
  if (action === 'Delete' || action === 'ErroEnvio' || action === 'Recusar' || action === 'Rejeitar') {
    return 'destructive';
  }

  if (action === 'Create' || action === 'Login' || action === 'Confirmar' || action === 'Aprovar' || action === 'Enviada') {
    return 'default';
  }

  return 'secondary';
}

function formatAuditJson(value) {
  if (!value) return null;

  try {
    const obj = JSON.parse(value);
    return JSON.stringify(obj, null, 2);
  } catch {
    return value;
  }
}

function parseAuditJson(value) {
  if (!value) return null;

  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

function toLocalDateInput(date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function buildPeriodFilters(days) {
  const now = new Date();
  const from = new Date(now);
  from.setDate(now.getDate() - days);

  return {
    createdAt_from: toLocalDateInput(from),
    createdAt_to: toLocalDateInput(now),
  };
}

function formatTimeBucket(date) {
  const source = new Date(date);
  const day = `${source.getDate()}`.padStart(2, '0');
  const month = `${source.getMonth() + 1}`.padStart(2, '0');
  const hour = `${source.getHours()}`.padStart(2, '0');
  return `${day}/${month} ${hour}:00`;
}

function getAuditDestination(item, t) {
  if (!item) return null;

  const entityId = item.entityId;
  const parsed = parseAuditJson(item.changesJson);

  if (item.entityName === 'Pessoa' && entityId) {
    return { to: `/pessoas/${entityId}`, label: t('audit.destinations.openPerson') };
  }

  if (item.entityName === 'Visitante' && entityId) {
    return { to: `/visitantes/${entityId}`, label: t('audit.destinations.openVisitor') };
  }

  if (item.entityName === 'Usuario') {
    return { to: '/usuarios', label: t('audit.destinations.openUsers') };
  }

  if (item.entityName === 'PessoaPerfil') {
    const pessoaId = parsed?.newValues?.PessoaId
      ?? parsed?.changes?.PessoaId?.newValue
      ?? parsed?.changes?.PessoaId?.oldValue;

    if (pessoaId) {
      return { to: `/pessoas/${pessoaId}`, label: t('audit.destinations.openPerson') };
    }
  }

  if (item.entityName === 'MensagemAgendada') {
    const visitanteId = parsed?.VisitanteId
      ?? parsed?.newValues?.VisitanteId
      ?? parsed?.changes?.VisitanteId?.newValue
      ?? parsed?.changes?.VisitanteId?.oldValue;

    if (visitanteId) {
      return { to: `/visitantes/${visitanteId}`, label: t('audit.destinations.openVisitor') };
    }

    return { to: '/mensagens-agendadas', label: t('audit.destinations.openMessages') };
  }

  if (item.entityName === 'Escala') {
    const ocorrenciaId = parsed?.EventoOcorrenciaId
      ?? parsed?.newValues?.EventoOcorrenciaId
      ?? parsed?.changes?.EventoOcorrenciaId?.newValue
      ?? parsed?.changes?.EventoOcorrenciaId?.oldValue;
    const equipeId = parsed?.EquipeId
      ?? parsed?.newValues?.EquipeId
      ?? parsed?.changes?.EquipeId?.newValue
      ?? parsed?.changes?.EquipeId?.oldValue;

    if (ocorrenciaId && equipeId) {
      return { to: `/voluntariado/escalas/ocorrencia/${ocorrenciaId}/equipe/${equipeId}`, label: t('audit.destinations.openSchedule') };
    }

    if (ocorrenciaId) {
      return { to: `/voluntariado/escalas/ocorrencia/${ocorrenciaId}`, label: t('audit.destinations.openOccurrence') };
    }

    return { to: '/voluntariado/escalas', label: t('audit.destinations.openSchedules') };
  }

  if (item.entityName === 'EscalaItem' || item.entityName === 'SolicitacaoTrocaEscala') {
    return { to: '/voluntariado/solicitacoes-troca', label: t('audit.destinations.openSwaps') };
  }

  if (item.entityName === 'CampanhaAniversario' || item.entityName === 'CampanhaAniversarioEnvio') {
    return { to: '/pessoas/aniversariantes/campanha', label: t('audit.destinations.openCampaign') };
  }

  return null;
}

export default function AuditoriaList() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [metrics, setMetrics] = useState({
    totalLogs: 0,
    criticalActions: 0,
    failureActions: 0,
    distinctUsers: 0,
    topUserLabel: '-',
    topUserCount: 0,
    topEntityName: '-',
    topEntityCount: 0,
    topActionName: '-',
    topActionCount: 0,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [filters, setFilters] = useState(() => ({
    ...DEFAULT_FILTERS,
    ...parseFiltersFromSearchParams(searchParams),
  }));
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [selected, setSelected] = useState(null);

  const entityOptions = useMemo(() => getEntityOptions(t), [t]);
  const actionOptions = useMemo(() => getActionOptions(t), [t]);

  const applyAlertFilters = useCallback((patch) => {
    setFilters((current) => ({ ...current, ...patch }));
    setPage(1);
  }, []);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const params = {
        page,
        pageSize,
        search: filters.search || undefined,
        entityName: filters.entityName || undefined,
        entityId: filters.entityId || undefined,
        action: filters.action || undefined,
        userName: filters.userName || undefined,
        userEmail: filters.userEmail || undefined,
        from: filters.createdAt_from || undefined,
        to: filters.createdAt_to || undefined,
      };

      const [resp, metricsResp] = await Promise.all([
        auditLogsApi.getPaged(params),
        auditLogsApi.getMetrics(params),
      ]);

      const data = resp.data || {};
      setItems(data.items || []);
      setTotal(Number(data.total || 0));
      setMetrics({
        totalLogs: Number(metricsResp.data?.totalLogs || 0),
        criticalActions: Number(metricsResp.data?.criticalActions || 0),
        failureActions: Number(metricsResp.data?.failureActions || 0),
        distinctUsers: Number(metricsResp.data?.distinctUsers || 0),
        topUserLabel: metricsResp.data?.topUserLabel || '-',
        topUserCount: Number(metricsResp.data?.topUserCount || 0),
        topEntityName: metricsResp.data?.topEntityName || '-',
        topEntityCount: Number(metricsResp.data?.topEntityCount || 0),
        topActionName: metricsResp.data?.topActionName || '-',
        topActionCount: Number(metricsResp.data?.topActionCount || 0),
      });
    } catch (err) {
      const msg = getApiErrorMessage(err, t('audit.errorLoad'));
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  }, [filters.action, filters.createdAt_from, filters.createdAt_to, filters.entityId, filters.entityName, filters.search, filters.userEmail, filters.userName, page, pageSize, t]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    setPage(1);
  }, [filters]);

  useEffect(() => {
    const nextFilters = {
      ...DEFAULT_FILTERS,
      ...parseFiltersFromSearchParams(searchParams),
    };

    setFilters((current) => (areFiltersEqual(current, nextFilters) ? current : nextFilters));
  }, [searchParams]);

  useEffect(() => {
    const nextParams = buildSearchParamsFromFilters(filters);
    const currentParams = searchParams.toString();
    const targetParams = nextParams.toString();

    if (currentParams !== targetParams) {
      setSearchParams(nextParams, { replace: true });
    }
  }, [filters, searchParams, setSearchParams]);

  const prettyJson = useMemo(() => formatAuditJson(selected?.changesJson), [selected]);
  const selectedDestination = useMemo(() => getAuditDestination(selected, t), [selected, t]);

  const activeFilterCount = useMemo(() => {
    return Object.values(filters).filter((value) => value !== undefined && value !== '').length;
  }, [filters]);

  const periodLabel = useMemo(() => {
    if (filters.createdAt_from && filters.createdAt_to) {
      return t('audit.periodLabel', { from: filters.createdAt_from, to: filters.createdAt_to });
    }

    if (filters.createdAt_from) {
      return t('audit.period.from', { value: filters.createdAt_from });
    }

    if (filters.createdAt_to) {
      return t('audit.period.to', { value: filters.createdAt_to });
    }

    return t('audit.period.all');
  }, [filters.createdAt_from, filters.createdAt_to, t]);

  const currentActionLabel = filters.action ? getActionLabel(filters.action, t) : t('audit.allActions');

  const recentCriticalItems = useMemo(() => {
    return items
      .filter((item) => QUICK_ACTIONS.includes(item.action) || ['Delete', 'ErroEnvio', 'Recusar', 'Rejeitar'].includes(item.action))
      .slice(0, 6);
  }, [items]);

  const trendBuckets = useMemo(() => {
    const map = new Map();

    for (const item of items) {
      if (!item.createdAt) continue;
      const bucket = formatTimeBucket(item.createdAt);
      map.set(bucket, (map.get(bucket) || 0) + 1);
    }

    return [...map.entries()]
      .slice(0, 8)
      .map(([label, count]) => ({ label, count }))
      .reverse();
  }, [items]);

  const maxTrendCount = useMemo(() => {
    return trendBuckets.reduce((max, item) => Math.max(max, item.count), 1);
  }, [trendBuckets]);

  const alerts = useMemo(() => {
    const nextAlerts = [];

    if (metrics.failureActions >= 5) {
      nextAlerts.push({
        id: 'failure-spike',
        title: t('audit.alerts.failureSpike.title'),
        description: t('audit.alerts.failureSpike.description', { count: metrics.failureActions }),
        variant: 'destructive',
        actionLabel: t('audit.alerts.failureSpike.action'),
        action: () => applyAlertFilters({ action: 'ErroEnvio' }),
      });
    }

    if (metrics.topActionName === 'ErroEnvio' && metrics.topActionCount >= 3) {
      nextAlerts.push({
        id: 'send-errors',
        title: t('audit.alerts.sendErrors.title'),
        description: t('audit.alerts.sendErrors.description', { count: metrics.topActionCount }),
        variant: 'destructive',
        actionLabel: t('audit.alerts.sendErrors.action'),
        action: () => applyAlertFilters({ action: 'ErroEnvio', entityName: 'MensagemAgendada' }),
      });
    }

    if (metrics.topActionName === 'AlterarSenha' && metrics.topActionCount >= 3) {
      nextAlerts.push({
        id: 'password-changes',
        title: t('audit.alerts.passwordChanges.title'),
        description: t('audit.alerts.passwordChanges.description', { count: metrics.topActionCount }),
        variant: 'secondary',
        actionLabel: t('audit.alerts.passwordChanges.action'),
        action: () => applyAlertFilters({ action: 'AlterarSenha', entityName: 'Usuario' }),
      });
    }

    if (metrics.topActionName === 'Login' && metrics.topActionCount >= 10) {
      nextAlerts.push({
        id: 'login-activity',
        title: t('audit.alerts.loginActivity.title'),
        description: t('audit.alerts.loginActivity.description', { count: metrics.topActionCount }),
        variant: 'secondary',
        actionLabel: t('audit.alerts.loginActivity.action'),
        action: () => applyAlertFilters({ action: 'Login', entityName: 'Auth' }),
      });
    }

    if (metrics.criticalActions >= 12) {
      nextAlerts.push({
        id: 'critical-actions',
        title: t('audit.alerts.criticalActions.title'),
        description: t('audit.alerts.criticalActions.description', { count: metrics.criticalActions }),
        variant: 'default',
        actionLabel: t('audit.alerts.criticalActions.action'),
        action: () => applyAlertFilters({ action: undefined }),
      });
    }

    return nextAlerts;
  }, [applyAlertFilters, metrics.criticalActions, metrics.failureActions, metrics.topActionCount, metrics.topActionName, t]);

  if (loading) return <LoadingPage text={t('audit.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('audit.title')}</h1>
          <p className="mt-1 text-muted-foreground">{t('audit.subtitle')}</p>
        </div>
      </div>

      <AdvancedSearch
        searchFields={[
          { key: 'search', label: t('audit.search.general'), type: 'text', placeholder: t('audit.search.generalPlaceholder') },
          { key: 'entityId', label: t('audit.search.entityId'), type: 'text', placeholder: t('audit.search.entityIdPlaceholder') },
          { key: 'userName', label: t('audit.search.userName'), type: 'text', placeholder: t('audit.search.userNamePlaceholder') },
          { key: 'userEmail', label: t('audit.search.userEmail'), type: 'text', placeholder: t('audit.search.userEmailPlaceholder') },
        ]}
        filterFields={[
          { key: 'entityName', label: t('audit.search.entity'), type: 'select', options: entityOptions },
          { key: 'action', label: t('audit.search.action'), type: 'select', options: actionOptions },
          { key: 'createdAt', label: t('audit.search.date'), type: 'date-range' },
        ]}
        values={filters}
        onChange={setFilters}
        onReset={() => setFilters(DEFAULT_FILTERS)}
      />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('audit.cards.total.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{metrics.totalLogs}</div>
            <p className="mt-1 text-xs text-muted-foreground">{t('audit.cards.total.description')}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('audit.cards.critical.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{metrics.criticalActions}</div>
            <p className="mt-1 text-xs text-muted-foreground">{t('audit.cards.critical.description')}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('audit.cards.failures.title')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{metrics.failureActions}</div>
            <p className="mt-1 text-xs text-muted-foreground">{t('audit.cards.failures.description')}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('audit.cards.highestVolume.title')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('audit.cards.highestVolume.user')}</div>
              <div className="font-semibold">{metrics.topUserLabel}</div>
              <div className="text-xs text-muted-foreground">{t('audit.records', { count: metrics.topUserCount })}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('audit.cards.highestVolume.entity')}</div>
              <div className="font-semibold">{metrics.topEntityName}</div>
              <div className="text-xs text-muted-foreground">{t('audit.records', { count: metrics.topEntityCount })}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('audit.cards.highestVolume.action')}</div>
              <div className="font-semibold">{getActionLabel(metrics.topActionName, t)}</div>
              <div className="text-xs text-muted-foreground">{t('audit.records', { count: metrics.topActionCount })}</div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('audit.operationalAlerts')}</CardTitle>
        </CardHeader>
        <CardContent>
          {alerts.length === 0 ? (
            <div className="flex items-center gap-3 rounded-lg border border-dashed p-4 text-sm text-muted-foreground">
              <Shield className="h-4 w-4" />
              {t('audit.noAlerts')}
            </div>
          ) : (
            <div className="space-y-3">
              {alerts.map((alert) => (
                <div key={alert.id} className="flex items-start gap-3 rounded-lg border p-4">
                  <TriangleAlert className="mt-0.5 h-4 w-4 text-amber-600" />
                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="font-medium">{alert.title}</span>
                      <Badge variant={alert.variant}>
                        {alert.variant === 'destructive'
                          ? t('audit.alertPriority.high')
                          : alert.variant === 'default'
                            ? t('audit.alertPriority.medium')
                            : t('audit.alertPriority.attention')}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground">{alert.description}</p>
                    {alert.actionLabel ? (
                      <div className="pt-1">
                        <Button type="button" variant="outline" size="sm" onClick={alert.action}>
                          {alert.actionLabel}
                        </Button>
                      </div>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 xl:grid-cols-[1.3fr_0.7fr]">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('audit.recentCriticalEvents')}</CardTitle>
          </CardHeader>
          <CardContent>
            {recentCriticalItems.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t('audit.noCriticalEvents')}</p>
            ) : (
              <div className="space-y-3">
                {recentCriticalItems.map((item) => {
                  const destination = getAuditDestination(item, t);
                  return (
                    <div key={item.id} className="flex items-start justify-between gap-3 rounded-lg border p-3">
                      <div className="space-y-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <Badge variant={getActionVariant(item.action)}>{getActionLabel(item.action, t)}</Badge>
                          <span className="text-sm font-medium">{t(`audit.entities.${item.entityName}`, { defaultValue: item.entityName })}</span>
                          <span className="text-xs text-muted-foreground">#{item.entityId}</span>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {item.userEmail || item.userName || (item.userId ? t('audit.userFallback', { id: item.userId }) : t('audit.system'))}
                        </div>
                        <div className="text-xs text-muted-foreground">{item.createdAt ? formatDateTime(item.createdAt) : '-'}</div>
                      </div>
                      <div className="flex items-center gap-1">
                        {destination ? (
                          <Button type="button" variant="ghost" size="sm" asChild>
                            <Link to={destination.to} title={destination.label}>
                              <ArrowUpRight className="h-4 w-4" />
                            </Link>
                          </Button>
                        ) : null}
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setSelected(item);
                            setDetailsOpen(true);
                          }}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('audit.recentTrend')}</CardTitle>
          </CardHeader>
          <CardContent>
            {trendBuckets.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t('audit.noTrendData')}</p>
            ) : (
              <div className="space-y-3">
                {trendBuckets.map((bucket) => (
                  <div key={bucket.label} className="space-y-1">
                    <div className="flex items-center justify-between text-xs text-muted-foreground">
                      <span>{bucket.label}</span>
                      <span>{bucket.count}</span>
                    </div>
                    <div className="h-2 rounded-full bg-muted">
                      <div
                        className="h-2 rounded-full bg-primary"
                        style={{ width: `${Math.max((bucket.count / maxTrendCount) * 100, 8)}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('audit.investigationShortcuts')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant={!filters.createdAt_from && !filters.createdAt_to ? 'default' : 'outline'}
              size="sm"
              onClick={() => setFilters((current) => ({ ...current, createdAt_from: '', createdAt_to: '' }))}
            >
              {t('audit.shortcuts.allPeriod')}
            </Button>
            <Button
              type="button"
              variant={filters.createdAt_from === toLocalDateInput(new Date()) && filters.createdAt_to === toLocalDateInput(new Date()) ? 'default' : 'outline'}
              size="sm"
              onClick={() => {
                const today = toLocalDateInput(new Date());
                setFilters((current) => ({ ...current, createdAt_from: today, createdAt_to: today }));
              }}
            >
              {t('audit.shortcuts.today')}
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setFilters((current) => ({ ...current, ...buildPeriodFilters(1) }))}
            >
              {t('audit.shortcuts.last24h')}
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setFilters((current) => ({ ...current, ...buildPeriodFilters(7) }))}
            >
              {t('audit.shortcuts.last7d')}
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setFilters((current) => ({ ...current, ...buildPeriodFilters(30) }))}
            >
              {t('audit.shortcuts.last30d')}
            </Button>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant={!filters.action ? 'default' : 'outline'}
              size="sm"
              onClick={() => setFilters((current) => ({ ...current, action: undefined }))}
            >
              {t('audit.allActions')}
            </Button>
            {QUICK_ACTIONS.map((action) => (
              <Button
                key={action}
                type="button"
                variant={filters.action === action ? 'default' : 'outline'}
                size="sm"
                onClick={() => setFilters((current) => ({ ...current, action }))}
              >
                {getActionLabel(action, t)}
              </Button>
            ))}
          </div>

          <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
            <Badge variant="secondary">{t('audit.activeFilters', { count: activeFilterCount })}</Badge>
            <Badge variant="outline">{t('audit.periodLabelBadge', { value: periodLabel })}</Badge>
            <Badge variant="outline">{t('audit.actionLabel', { value: currentActionLabel })}</Badge>
            {filters.entityName ? (
              <Badge variant="outline">
                {t('audit.entityLabel', { value: t(`audit.entities.${filters.entityName}`, { defaultValue: filters.entityName }) })}
              </Badge>
            ) : null}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('audit.logsTitle', { total })}</CardTitle>
        </CardHeader>
        <CardContent>
          {items.length === 0 ? (
            <div className="py-12 text-center">
              <Shield className="mx-auto mb-4 h-12 w-12 text-muted-foreground" />
              <h3 className="mb-2 text-lg font-medium text-foreground">{t('audit.noLogsTitle')}</h3>
              <p className="text-muted-foreground">{t('audit.noLogsDescription')}</p>
            </div>
          ) : (
            <div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('audit.table.date')}</TableHead>
                    <TableHead>{t('audit.table.action')}</TableHead>
                    <TableHead>{t('audit.table.entity')}</TableHead>
                    <TableHead>{t('audit.table.entityId')}</TableHead>
                    <TableHead>{t('audit.table.user')}</TableHead>
                    <TableHead>{t('audit.table.ip')}</TableHead>
                    <TableHead className="text-right">{t('audit.table.details')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((it) => {
                    const destination = getAuditDestination(it, t);
                    return (
                      <TableRow key={it.id}>
                        <TableCell>{it.createdAt ? formatDateTime(it.createdAt) : '-'}</TableCell>
                        <TableCell>
                          <Badge variant={getActionVariant(it.action)}>
                            {getActionLabel(it.action, t)}
                          </Badge>
                        </TableCell>
                        <TableCell>{t(`audit.entities.${it.entityName}`, { defaultValue: it.entityName })}</TableCell>
                        <TableCell>{it.entityId}</TableCell>
                        <TableCell className="max-w-[260px] truncate">
                          {it.userEmail || it.userName || (it.userId ? t('audit.userFallback', { id: it.userId }) : '-')}
                        </TableCell>
                        <TableCell>{it.ipAddress || '-'}</TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-1">
                            {destination ? (
                              <Button type="button" variant="ghost" size="sm" asChild>
                                <Link to={destination.to} title={destination.label}>
                                  <ArrowUpRight className="h-4 w-4" />
                                </Link>
                              </Button>
                            ) : null}
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                setSelected(it);
                                setDetailsOpen(true);
                              }}
                            >
                              <Eye className="h-4 w-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}

          {total > 0 && (
            <DataTablePagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              onPageSizeChange={(newSize) => {
                setPageSize(newSize);
                setPage(1);
              }}
            />
          )}
        </CardContent>
      </Card>

      <Dialog open={detailsOpen} onOpenChange={setDetailsOpen}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t('audit.dialog.title')}</DialogTitle>
            <DialogDescription>
              {selected ? `${t(`audit.entities.${selected.entityName}`, { defaultValue: selected.entityName })} ${selected.entityId} - ${getActionLabel(selected.action, t)}` : ''}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="grid grid-cols-1 gap-2 text-sm">
              <div><span className="text-muted-foreground">{t('audit.dialog.entity')}</span> {selected?.entityName ? t(`audit.entities.${selected.entityName}`, { defaultValue: selected.entityName }) : '-'}</div>
              <div><span className="text-muted-foreground">{t('audit.dialog.entityId')}</span> {selected?.entityId || '-'}</div>
              <div><span className="text-muted-foreground">{t('audit.dialog.action')}</span> {selected?.action ? getActionLabel(selected.action, t) : '-'}</div>
              <div><span className="text-muted-foreground">{t('audit.dialog.user')}</span> {selected?.userEmail || selected?.userName || selected?.userId || '-'}</div>
              <div><span className="text-muted-foreground">{t('audit.dialog.ip')}</span> {selected?.ipAddress || '-'}</div>
              <div><span className="text-muted-foreground">{t('audit.dialog.when')}</span> {selected?.createdAt ? formatDateTime(selected.createdAt) : '-'}</div>
            </div>
            {selectedDestination ? (
              <div className="flex justify-end">
                <Button type="button" variant="outline" asChild>
                  <Link to={selectedDestination.to} onClick={() => setDetailsOpen(false)}>
                    <ArrowUpRight className="mr-2 h-4 w-4" />
                    {selectedDestination.label}
                  </Link>
                </Button>
              </div>
            ) : null}
            <div className="rounded-md border bg-muted/30 p-3">
              <div className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                <Search className="h-3.5 w-3.5" />
                {t('audit.dialog.recordedChanges')}
              </div>
              <pre className="max-h-[380px] overflow-auto whitespace-pre-wrap text-xs">
                {prettyJson || t('audit.dialog.noChangeDetails')}
              </pre>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
