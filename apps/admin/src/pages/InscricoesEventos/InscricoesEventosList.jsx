import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Edit, Trash2, Filter, Eye, CheckCircle, XCircle, Phone, Mail, Users } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { RowIconButtonAction, RowIconLinkAction, TableRowActions } from '@/components/ui/list-actions';
import { StatusBadge } from '@/components/ui/status-badge';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { inscricoesEventosApi, eventosApi } from '@/lib/api';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';

const STATUS_LABELS = (t) => ({
  1: t('eventRegistrations.status.pending'),
  2: t('eventRegistrations.status.confirmed'),
  3: t('eventRegistrations.status.canceled'),
  4: t('eventRegistrations.status.present'),
});

const STATUS_TONES = {
  1: 'warning',
  2: 'success',
  3: 'danger',
  4: 'info',
};

export default function InscricoesEventosList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [eventos, setEventos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const [eventoFilter, setEventoFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [confirmState, setConfirmState] = useState({ open: false, action: null, id: null });
  const [confirmLoading, setConfirmLoading] = useState(false);

  const load = async ({ silent = false } = {}) => {
    try {
      if (silent) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }
      if (!silent) {
        setError(null);
      }
      const [inscricoesRes, eventosRes] = await Promise.all([
        inscricoesEventosApi.getAll(),
        eventosApi.getAll(),
      ]);
      setItems(inscricoesRes.data || []);
      setEventos(eventosRes.data || []);
    } catch (err) {
      setError(t('eventRegistrations.errorLoad'));
      console.error(err);
    } finally {
      if (silent) {
        setRefreshing(false);
      } else {
        setLoading(false);
      }
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (id) => {
    setConfirmState({ open: true, action: 'delete', id });
  };

  const handleConfirmar = async (id) => {
    try {
      await inscricoesEventosApi.confirmar(id);
      toast.success(t('eventRegistrations.confirmSuccess'));
      await load();
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('eventRegistrations.errorConfirm')));
    }
  };

  const handleCancelar = async (id) => {
    setConfirmState({ open: true, action: 'cancel', id });
  };

  const runConfirmedAction = async () => {
    const { action, id } = confirmState;
    if (!action || !id) return;
    try {
      setConfirmLoading(true);
      if (action === 'delete') {
        await inscricoesEventosApi.delete(id);
        toast.success(t('eventRegistrations.deleteSuccess'));
      } else if (action === 'cancel') {
        await inscricoesEventosApi.cancelar(id);
        toast.success(t('eventRegistrations.cancelSuccess'));
      }
      setConfirmState({ open: false, action: null, id: null });
      await load();
    } catch (err) {
      toast.error(
        getApiErrorMessage(
          err,
          action === 'delete' ? t('eventRegistrations.errorDelete') : t('eventRegistrations.errorCancel')
        )
      );
    } finally {
      setConfirmLoading(false);
    }
  };

  const filtered = items.filter((i) => {
    if (busca && !i.nome?.toLowerCase().includes(busca.toLowerCase()) && !i.whatsApp?.includes(busca)) return false;
    if (eventoFilter && String(i.eventoId) !== eventoFilter) return false;
    if (statusFilter && String(i.status) !== statusFilter) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('eventRegistrations.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold sm:text-3xl">{t('eventRegistrations.title')}</h1>
          <p className="text-muted-foreground">{t('eventRegistrations.subtitle')}</p>
        </div>
        <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('eventRegistrations.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <Filter className="h-4 w-4" />
                {t('eventRegistrations.searchLabel')}
              </label>
              <input
                className="w-full px-3 py-2 border rounded"
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('eventRegistrations.searchPlaceholder')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('eventRegistrations.eventLabel')}</label>
              <select className="w-full px-3 py-2 border rounded" value={eventoFilter} onChange={(e) => setEventoFilter(e.target.value)}>
                <option value="">{t('eventRegistrations.eventAllOption')}</option>
                {eventos.map((e) => (
                  <option key={e.id} value={e.id}>{e.titulo}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('eventRegistrations.statusLabel')}</label>
              <select className="w-full px-3 py-2 border rounded" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">{t('eventRegistrations.statusAllOption')}</option>
                <option value="1">{t('eventRegistrations.status.pending')}</option>
                <option value="2">{t('eventRegistrations.status.confirmed')}</option>
                <option value="3">{t('eventRegistrations.status.canceled')}</option>
                <option value="4">{t('eventRegistrations.status.present')}</option>
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('eventRegistrations.listTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('eventRegistrations.emptyMessage')}
              description={t('eventRegistrations.emptyDescription')}
            />
          ) : (
            <>
              <div className="space-y-3 md:hidden">
                {paginatedItems.map((inscricao) => (
                  <div key={inscricao.id} className="rounded-lg border bg-background p-3 shadow-xs">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0 space-y-1">
                        <div className="font-medium leading-snug">{inscricao.nome}</div>
                        <div className="truncate text-xs text-muted-foreground">{inscricao.eventoTitulo || '-'}</div>
                      </div>
                      <StatusBadge tone={STATUS_TONES[inscricao.status] || 'neutral'}>
                        {STATUS_LABELS(t)[inscricao.status] || inscricao.statusDescricao}
                      </StatusBadge>
                    </div>

                    <div className="mt-3 grid gap-2 text-sm">
                      <div className="flex min-w-0 items-center justify-between gap-2">
                        <span className="min-w-0 truncate text-muted-foreground">{inscricao.whatsApp || '-'}</span>
                        {inscricao.whatsApp && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => window.open(`https://wa.me/55${inscricao.whatsApp.replace(/\D/g, '')}`)}
                          >
                            <Phone className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                      <div className="flex min-w-0 items-center justify-between gap-2">
                        <span className="min-w-0 truncate text-muted-foreground">{inscricao.email || '-'}</span>
                        {inscricao.email && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => window.open(`mailto:${inscricao.email}`)}
                          >
                            <Mail className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                      <div className="flex items-center justify-between gap-2 text-muted-foreground">
                        <span>{formatDateTime(inscricao.dataInscricao)}</span>
                        <span className="inline-flex items-center gap-1">
                          <Users className="h-4 w-4" />
                          {inscricao.quantidadeAcompanhantes || 0}
                        </span>
                      </div>
                    </div>

                    <div className="mt-3 flex items-center justify-end gap-1 border-t pt-2">
                      <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                        <Link to={`/inscricoes-eventos/${inscricao.id}`}>
                          <Eye className="h-4 w-4" />
                        </Link>
                      </Button>
                      {inscricao.status === 1 && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => handleConfirmar(inscricao.id)} title={t('eventRegistrations.confirmAction')}>
                          <CheckCircle className="h-4 w-4 text-green-600" />
                        </Button>
                      )}
                      {(inscricao.status === 1 || inscricao.status === 2) && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => handleCancelar(inscricao.id)} title={t('eventRegistrations.cancelAction')}>
                          <XCircle className="h-4 w-4 text-red-600" />
                        </Button>
                      )}
                      <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                        <Link to={`/inscricoes-eventos/${inscricao.id}/editar`}>
                          <Edit className="h-4 w-4" />
                        </Link>
                      </Button>
                      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => handleDelete(inscricao.id)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>

              <div className="hidden md:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t('eventRegistrations.table.name')}</TableHead>
                      <TableHead>{t('eventRegistrations.table.whatsapp')}</TableHead>
                      <TableHead>{t('eventRegistrations.table.email')}</TableHead>
                      <TableHead>{t('eventRegistrations.table.event')}</TableHead>
                      <TableHead>{t('eventRegistrations.table.status')}</TableHead>
                      <TableHead>{t('eventRegistrations.table.companions')}</TableHead>
                      <TableHead>{t('eventRegistrations.table.registrationDate')}</TableHead>
                      <TableHead className="text-right">{t('eventRegistrations.table.actions')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {paginatedItems.map((inscricao) => (
                      <TableRow key={inscricao.id}>
                        <TableCell className="font-medium">{inscricao.nome}</TableCell>
                        <TableCell>
                          <div className="flex items-center space-x-2">
                            <span>{inscricao.whatsApp}</span>
                            {inscricao.whatsApp && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => window.open(`https://wa.me/55${inscricao.whatsApp.replace(/\D/g, '')}`)}
                              >
                                <Phone className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center space-x-2">
                            <span>{inscricao.email || '-'}</span>
                            {inscricao.email && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => window.open(`mailto:${inscricao.email}`)}
                              >
                                <Mail className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>{inscricao.eventoTitulo || '-'}</TableCell>
                        <TableCell>
                          <StatusBadge tone={STATUS_TONES[inscricao.status] || 'neutral'}>
                            {STATUS_LABELS(t)[inscricao.status] || inscricao.statusDescricao}
                          </StatusBadge>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <Users className="h-4 w-4" />
                            {inscricao.quantidadeAcompanhantes || 0}
                          </div>
                        </TableCell>
                        <TableCell>{formatDateTime(inscricao.dataInscricao)}</TableCell>
                        <TableCell className="text-right">
                          <TableRowActions className="space-x-1">
                            <RowIconLinkAction>
                              <Link to={`/inscricoes-eventos/${inscricao.id}`}>
                                <Eye className="h-4 w-4" />
                              </Link>
                            </RowIconLinkAction>
                            {inscricao.status === 1 && (
                              <RowIconButtonAction onClick={() => handleConfirmar(inscricao.id)} title={t('eventRegistrations.confirmAction')}>
                                <CheckCircle className="h-4 w-4 text-green-600" />
                              </RowIconButtonAction>
                            )}
                            {(inscricao.status === 1 || inscricao.status === 2) && (
                              <RowIconButtonAction onClick={() => handleCancelar(inscricao.id)} title={t('eventRegistrations.cancelAction')}>
                                <XCircle className="h-4 w-4 text-red-600" />
                              </RowIconButtonAction>
                            )}
                            <RowIconLinkAction>
                              <Link to={`/inscricoes-eventos/${inscricao.id}/editar`}>
                                <Edit className="h-4 w-4" />
                              </Link>
                            </RowIconLinkAction>
                            <RowIconButtonAction onClick={() => handleDelete(inscricao.id)}>
                              <Trash2 className="h-4 w-4" />
                            </RowIconButtonAction>
                          </TableRowActions>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </>
          )}
          {filtered.length > 0 && (
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
        open={confirmState.open}
        onOpenChange={(open) => {
          if (!open) setConfirmState({ open: false, action: null, id: null });
          else setConfirmState((s) => ({ ...s, open: true }));
        }}
        onConfirm={runConfirmedAction}
        loading={confirmLoading}
        variant={confirmState.action === 'delete' ? 'destructive' : 'default'}
        title={
          confirmState.action === 'delete'
            ? t('eventRegistrations.confirmDialog.deleteTitle')
            : t('eventRegistrations.confirmDialog.cancelTitle')
        }
        description={
          confirmState.action === 'delete'
            ? t('eventRegistrations.confirmDialog.deleteDescription')
            : t('eventRegistrations.confirmDialog.cancelDescription')
        }
        confirmText={
          confirmState.action === 'delete'
            ? t('eventRegistrations.confirmDialog.confirmDelete')
            : t('eventRegistrations.confirmDialog.confirmCancel')
        }
      />
    </div>
  );
}


