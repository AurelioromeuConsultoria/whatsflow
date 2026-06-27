import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { CalendarDays, PlusCircle, Settings, Trash2 } from 'lucide-react';
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
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { usePagination } from '@/hooks/usePagination';
import { eventosApi, eventosOcorrenciasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';

function getStatusOcorrenciaLabel(status, t) {
  const value = Number(status);
  if (value === 1) return t('events.occurrencesStatus.confirmed');
  if (value === 2) return t('events.occurrencesStatus.canceled');
  if (value === 3) return t('events.occurrencesStatus.done');
  return t('events.occurrencesStatus.unknown');
}

export default function OcorrenciasList() {
  const { t } = useTranslation();
  const { can } = useAuth();
  const confirmDialog = useConfirmDialog();
  const [initialLoad, setInitialLoad] = useState(true);
  const [loadingOcorrencias, setLoadingOcorrencias] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [eventos, setEventos] = useState([]);
  const [ocorrencias, setOcorrencias] = useState([]);

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

  const canEdit = can(RESOURCES.EVENTOS, ACTIONS.EDIT);

  const loadBase = async () => {
    try {
      const eventosRes = await eventosApi.getAll();
      setEventos(eventosRes.data || []);
    } catch (err) {
      console.error(err);
      setError(t('events.errorLoad'));
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
      setOcorrencias(res.data || []);
    } catch (err) {
      console.error(err);
      setError(t('events.occurrencesErrorLoad'));
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

  const handleGerarOcorrencias = async () => {
    if (filtroEventoId === 'all') {
      toast.error(t('events.occurrencesGenerateSelectEvent'));
      return;
    }

    try {
      const res = await eventosOcorrenciasApi.gerarRecorrencia(
        Number(filtroEventoId),
        `${dataInicio}T00:00:00`,
        `${dataFim}T23:59:59`
      );
      const total = res.data?.totalCriadas ?? 0;
      if (total > 0) {
        toast.success(t('events.occurrencesGenerateCreated', { count: total }));
        await loadOcorrencias();
      } else {
        toast.warning(t('events.occurrencesGenerateNone'));
        await loadOcorrencias();
      }
    } catch (err) {
      console.error(err);
      const msg = err.response?.data?.message ?? err.response?.data;
      toast.error(typeof msg === 'string' ? msg : t('events.occurrencesGenerateError'));
    }
  };

  const handleDeleteOcorrencia = (item) => {
    if (item.possuiEscala) {
      toast.error(t('events.occurrencesDeleteHasScales'));
      return;
    }

    confirmDialog.show({
      title: t('events.occurrencesDeleteTitle'),
      description: t('events.occurrencesDeleteConfirm'),
      confirmText: t('actions.remove'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await eventosOcorrenciasApi.delete(item.id);
          toast.success(t('events.occurrencesDeleteSuccess'));
          await loadOcorrencias();
        } catch (err) {
          console.error(err);
          const msg = err.response?.data?.message ?? err.response?.data;
          toast.error(typeof msg === 'string' ? msg : t('events.occurrencesDeleteError'));
          throw err;
        }
      },
    });
  };

  const sorted = [...ocorrencias].sort((a, b) => new Date(a.dataHoraInicio) - new Date(b.dataHoraInicio));
  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(sorted, 20);

  if (initialLoad) return <LoadingPage text={t('events.occurrencesLoading')} />;
  if (error) return <ErrorPage message={error} onRetry={loadOcorrencias} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('events.occurrencesTitle')}</h1>
          <p className="text-muted-foreground">{t('events.occurrencesSubtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => loadOcorrencias({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button onClick={handleGerarOcorrencias}>
              <PlusCircle className="h-4 w-4 mr-2" />
              {t('events.occurrencesActions.generate')}
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('events.occurrencesFiltersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label>{t('events.occurrencesEventLabel')}</Label>
              <Select value={filtroEventoId} onValueChange={setFiltroEventoId}>
                <SelectTrigger>
                  <SelectValue placeholder={t('events.occurrencesAllEvents')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('events.occurrencesAllEvents')}</SelectItem>
                  {eventos.map((evento) => (
                    <SelectItem key={evento.id} value={String(evento.id)}>
                      {evento.titulo}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t('events.occurrencesStartDateLabel')}</Label>
              <Input type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>{t('events.occurrencesEndDateLabel')}</Label>
              <Input type="date" value={dataFim} onChange={(e) => setDataFim(e.target.value)} />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('events.occurrencesListTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {loadingOcorrencias ? (
            <div className="text-center py-8 text-muted-foreground">
              {t('events.occurrencesLoading')}
            </div>
          ) : sorted.length === 0 ? (
            <PageEmptyState
              title={t('events.occurrencesEmptyMessage')}
              description={t('events.occurrencesEmptyDescription')}
              action={canEdit ? (
                <Button onClick={handleGerarOcorrencias}>
                  <PlusCircle className="h-4 w-4 mr-2" />
                  {t('events.occurrencesActions.generate')}
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('events.occurrencesTable.event')}</TableHead>
                  <TableHead>{t('events.occurrencesTable.dateTime')}</TableHead>
                  <TableHead>{t('events.occurrencesTable.status')}</TableHead>
                  <TableHead>{t('events.occurrencesTable.scales')}</TableHead>
                  <TableHead className="text-right">{t('events.occurrencesTable.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((item) => (
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
                      {item.possuiEscala ? (
                        <span className="px-2 py-1 rounded text-xs bg-green-100 text-green-800">
                          {t('events.occurrencesTable.hasScales')}
                        </span>
                      ) : (
                        <span className="px-2 py-1 rounded text-xs bg-gray-100 text-gray-800">
                          {t('events.occurrencesTable.noScales')}
                        </span>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button variant="outline" size="sm" asChild>
                          <Link to={`/voluntariado/escalas/ocorrencia/${item.id}`}>
                            <Settings className="h-4 w-4 mr-2" />
                            {t('events.occurrencesTable.buildScales')}
                          </Link>
                        </Button>
                        {canEdit && (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => handleDeleteOcorrencia(item)}
                            disabled={item.possuiEscala}
                            title={item.possuiEscala ? t('events.occurrencesDeleteHasScales') : t('events.occurrencesTable.delete')}
                          >
                            <Trash2 className="h-4 w-4 mr-2 text-destructive" />
                            {t('events.occurrencesTable.delete')}
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
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

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={confirmDialog.hide}
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
