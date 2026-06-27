import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, CalendarDays, CheckCircle2, Clock3, Settings, XCircle } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { usePagination } from '@/hooks/usePagination';
import { eventosApi, eventosOcorrenciasApi } from '@/lib/api';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';

function getStatusOcorrenciaLabel(status, t) {
  const value = Number(status);
  if (value === 1) return t('events.occurrencesStatus.confirmed');
  if (value === 2) return t('events.occurrencesStatus.canceled');
  if (value === 3) return t('events.occurrencesStatus.done');
  return t('events.occurrencesStatus.unknown');
}

function getRiskClassName(nivelRisco) {
  if (nivelRisco === 'high') return 'bg-red-100 text-red-800 hover:bg-red-100';
  if (nivelRisco === 'attention') return 'bg-amber-100 text-amber-800 hover:bg-amber-100';
  if (nivelRisco === 'none') return 'bg-gray-100 text-gray-800 hover:bg-gray-100';
  return 'bg-green-100 text-green-800 hover:bg-green-100';
}

export default function EscalasList() {
  const [initialLoad, setInitialLoad] = useState(true);
  const [loadingOcorrencias, setLoadingOcorrencias] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [eventos, setEventos] = useState([]);
  const [ocorrencias, setOcorrencias] = useState([]);
  const [coverageByOcorrencia, setCoverageByOcorrencia] = useState({});

  const [filtroEventoId, setFiltroEventoId] = useState('all');
  const [dataInicio, setDataInicio] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 10);
  });
  const [dataFim, setDataFim] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() + 30);
    return d.toISOString().slice(0, 10);
  });

  const { t } = useTranslation();

  const loadBase = async () => {
    try {
      const eventosRes = await eventosApi.getAll();
      setEventos(eventosRes.data || []);
    } catch (err) {
      console.error(err);
      setError(t('volunteer.schedules.errorLoadEvents'));
    }
  };

  const loadOcorrencias = async ({ silent = false } = {}) => {
    try {
      if (silent) {
        setRefreshing(true);
      } else {
        setLoadingOcorrencias(true);
      }
      if (!silent) {
        setError(null);
      }
      const eventoId = filtroEventoId === 'all' ? undefined : Number(filtroEventoId);
      const res = await eventosOcorrenciasApi.getByPeriodo(
        `${dataInicio}T00:00:00`,
        `${dataFim}T23:59:59`,
        eventoId
      );
      const ocorrenciasData = res.data || [];
      setOcorrencias(ocorrenciasData);

      if (ocorrenciasData.length === 0) {
        setCoverageByOcorrencia({});
        return;
      }

      const coberturaRes = await eventosOcorrenciasApi.getCoberturaVoluntariado({
        dataInicio: `${dataInicio}T00:00:00`,
        dataFim: `${dataFim}T23:59:59`,
        eventoId,
      });
      const cobertura = coberturaRes.data || [];
      setCoverageByOcorrencia(
        Object.fromEntries(
          cobertura.map((item) => [item.ocorrenciaId, item])
        )
      );
    } catch (err) {
      console.error(err);
      setError(t('volunteer.schedules.errorLoadOccurrences'));
    } finally {
      if (silent) {
        setRefreshing(false);
      } else {
        setLoadingOcorrencias(false);
      }
      setInitialLoad(false);
    }
  };

  useEffect(() => {
    loadBase();
  }, []);

  useEffect(() => {
    loadOcorrencias();
  }, [filtroEventoId, dataInicio, dataFim]);

  const sorted = [...ocorrencias].sort((a, b) => new Date(a.dataHoraInicio) - new Date(b.dataHoraInicio));
  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(sorted, 20);

  if (initialLoad) return <LoadingPage text={t('volunteer.schedules.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={loadOcorrencias} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.schedules.title')}</h1>
          <p className="text-muted-foreground">
            {t('volunteer.schedules.subtitle')}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" asChild>
            <Link to="/eventos/ocorrencias">{t('volunteer.schedules.goToOccurrences')}</Link>
          </Button>
          <PageRefreshButton onClick={() => loadOcorrencias({ silent: true })} refreshing={refreshing} />
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.schedules.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label>{t('volunteer.schedules.eventLabel')}</Label>
              <Select value={filtroEventoId} onValueChange={setFiltroEventoId}>
                <SelectTrigger>
                  <SelectValue placeholder={t('volunteer.schedules.allEventsOption')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.schedules.allEventsOption')}</SelectItem>
                  {eventos.map((evento) => (
                    <SelectItem key={evento.id} value={String(evento.id)}>
                      {evento.titulo}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.schedules.startDateLabel')}</Label>
              <Input type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.schedules.endDateLabel')}</Label>
              <Input type="date" value={dataFim} onChange={(e) => setDataFim(e.target.value)} />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('volunteer.schedules.listTitle')} ({total})</CardTitle>
            <PageRefreshButton onClick={() => loadOcorrencias({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {loadingOcorrencias ? (
            <div className="text-center py-8 text-muted-foreground">{t('volunteer.schedules.loadingOccurrences')}</div>
          ) : sorted.length === 0 ? (
            <PageEmptyState
              title={t('volunteer.schedules.emptyTitle')}
              description={t('volunteer.schedules.emptyDescription')}
              action={(
                <Button variant="outline" asChild>
                  <Link to="/eventos/ocorrencias">{t('volunteer.schedules.goToOccurrences')}</Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('volunteer.schedules.table.event')}</TableHead>
                  <TableHead>{t('volunteer.schedules.table.dateTime')}</TableHead>
                  <TableHead>{t('volunteer.schedules.table.occurrenceStatus')}</TableHead>
                  <TableHead>{t('volunteer.schedules.table.coverage')}</TableHead>
                  <TableHead className="text-right">{t('volunteer.schedules.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((item) => {
                  const coverage = coverageByOcorrencia[item.id] || {
                    rotuloRisco: t('volunteer.schedules.coverage.noSchedule'),
                    nivelRisco: 'none',
                    confirmados: 0,
                    pendentes: 0,
                    recusados: 0,
                    substituidos: 0,
                  };

                  return (
                    <TableRow key={item.id}>
                      <TableCell className="font-medium">{item.eventoTitulo}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <CalendarDays className="h-4 w-4 text-muted-foreground" />
                          {formatDateTime(item.dataHoraInicio)}
                        </div>
                      </TableCell>
                      <TableCell>{getStatusOcorrenciaLabel(item.status, t)}</TableCell>
                      <TableCell>
                        <div className="space-y-2">
                          <Badge className={getRiskClassName(coverage.nivelRisco)}>
                            {coverage.rotuloRisco}
                          </Badge>
                          <div className="flex flex-wrap gap-3 text-xs text-muted-foreground">
                            <span className="inline-flex items-center gap-1">
                              <CheckCircle2 className="h-3.5 w-3.5 text-green-600" />
                              {coverage.confirmados} {t('volunteer.schedules.coverage.confirmed')}
                            </span>
                            <span className="inline-flex items-center gap-1">
                              <Clock3 className="h-3.5 w-3.5 text-amber-600" />
                              {coverage.pendentes} {t('volunteer.schedules.coverage.pending')}
                            </span>
                            <span className="inline-flex items-center gap-1">
                              <XCircle className="h-3.5 w-3.5 text-red-600" />
                              {coverage.recusados} {t('volunteer.schedules.coverage.declined')}
                            </span>
                            <span className="inline-flex items-center gap-1">
                              <AlertTriangle className="h-3.5 w-3.5 text-slate-600" />
                              {coverage.substituidos} {t('volunteer.schedules.coverage.replaced')}
                            </span>
                          </div>
                        </div>
                      </TableCell>
                      <TableCell className="text-right">
                        <Button variant="outline" size="sm" asChild>
                          <Link
                            to={`/voluntariado/escalas/ocorrencia/${item.id}`}
                            state={{
                              breadcrumbLabels: {
                                [`/voluntariado/escalas/ocorrencia/${item.id}`]: item.eventoTitulo,
                              },
                            }}
                          >
                            <Settings className="h-4 w-4 mr-2" />
                            {item.possuiEscala ? t('volunteer.schedules.editAction') : t('volunteer.schedules.buildAction')}
                          </Link>
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
          {sorted.length > 0 && (
            <DataTablePagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
