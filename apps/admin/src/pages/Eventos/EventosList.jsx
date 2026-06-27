import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Users, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { AdvancedSearch } from '@/components/ui/advanced-search';
import { SortableTableHeader } from '@/components/ui/sortable-table-header';
import { useTableSort } from '@/hooks/useTableSort';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { exportToCSV } from '@/utils/export';
import { eventosApi, normalizeEvento } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';

export default function EventosList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [filters, setFilters] = useState({
    titulo: '',
    descricao: '',
    dataInicio_from: '',
    dataInicio_to: '',
  });
  const confirmDialog = useConfirmDialog();
  const { can } = useAuth();

  const load = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const res = await eventosApi.getAll();
      const raw = res.data || [];
      setItems(Array.isArray(raw) ? raw.map(normalizeEvento) : raw);
    } catch (err) {
      setError(t('events.errorLoad', 'Erro ao carregar eventos'));
      console.error(err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (id) => {
    const evento = items.find(e => e.id === id);
    confirmDialog.show({
      title: t('events.deleteTitle'),
      description: t('events.deleteDescription', {
        title: evento?.titulo || t('events.deleteFallbackTitle'),
      }),
      confirmText: t('events.deleteConfirm'),
      cancelText: t('common.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await eventosApi.delete(id);
          toast.success(t('events.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('events.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filteredRaw = items.filter((e) => {
    // Busca por título
    if (filters.titulo && !e.titulo?.toLowerCase().includes(filters.titulo.toLowerCase())) {
      return false;
    }

    // Busca por descrição
    if (filters.descricao && !e.descricao?.toLowerCase().includes(filters.descricao.toLowerCase())) {
      return false;
    }

    // Filtro por data de início
    if (filters.dataInicio_from) {
      const dataInicio = new Date(e.dataInicio);
      const dataFrom = new Date(filters.dataInicio_from + 'T00:00:00');
      if (dataInicio < dataFrom) return false;
    }

    if (filters.dataInicio_to) {
      const dataInicio = new Date(e.dataInicio);
      const dataTo = new Date(filters.dataInicio_to + 'T23:59:59');
      if (dataInicio > dataTo) return false;
    }

    return true;
  });

  // Ordenação
  const { sortedData: filtered, sortConfig, handleSort } = useTableSort(filteredRaw, {
    defaultSort: 'titulo',
    defaultDirection: 'asc',
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  // Exibe data formatada ou '-' se vazia ou data default (ex: 0001-01-01)
  const formatEventDate = (value) => {
    if (!value) return '-';
    const d = new Date(value);
    if (isNaN(d.getTime()) || d.getFullYear() < 1900) return '-';
    return formatDateTime(d);
  };

  const getTipoLabel = (tipo, tipoDescricao) => {
    if (tipoDescricao) return tipoDescricao;
    const map = {
      1: t('events.type.event'),
      2: t('events.type.service'),
      3: t('events.type.meeting'),
      4: t('events.type.other'),
    };
    return map[tipo] ?? t('events.type.event');
  };

  // Exportação
  const handleExport = () => {
    const exportData = filtered.map(evento => ({
      [t('events.export.title')]: evento.titulo || '',
      [t('events.export.description')]: evento.descricao || '',
      [t('events.export.startDate')]: formatEventDate(evento.dataInicio),
      [t('events.export.endDate')]: formatEventDate(evento.dataFim),
      [t('events.export.url')]: evento.url || '',
    }));

    exportToCSV(exportData, 'eventos');
    toast.success(t('events.export.success'));
  };

  if (loading) return <LoadingPage text={t('events.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = can(RESOURCES.EVENTOS, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.EVENTOS, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold sm:text-3xl">{t('events.title')}</h1>
          <p className="text-muted-foreground">{t('events.subtitle')}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/eventos/novo">
                <Plus className="h-4 w-4 mr-2" /> {t('events.new')}
              </Link>
            </Button>
          )}
        </div>
      </div>

      <AdvancedSearch
        searchFields={[
          { key: 'titulo', label: t('events.fields.title'), type: 'text', placeholder: t('events.search.titlePlaceholder') },
          { key: 'descricao', label: t('events.fields.description'), type: 'text', placeholder: t('events.search.descriptionPlaceholder') },
        ]}
        filterFields={[
          {
            key: 'dataInicio',
            label: t('events.fields.startDate'),
            type: 'date-range',
          },
        ]}
        values={filters}
        onChange={setFilters}
        onReset={() => {
          setFilters({
            titulo: '',
            descricao: '',
            dataInicio_from: '',
            dataInicio_to: '',
          });
        }}
      />

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle>{t('events.listTitle')} ({total})</CardTitle>
            {filtered.length > 0 && (
              <Button variant="outline" size="sm" onClick={handleExport} className="w-full sm:w-auto">
                <Download className="h-4 w-4 mr-2" />
                {t('events.export.button')}
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('events.emptyTitle')}
              description={t('events.emptyMessage')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/eventos/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('events.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <>
              <div className="space-y-3 md:hidden">
                {paginatedItems.map((evento) => (
                  <div key={evento.id} className="rounded-lg border bg-background p-3 shadow-xs">
                    <div className="space-y-1">
                      <div className="font-medium leading-snug">{evento.titulo || '-'}</div>
                      <div className="text-xs text-muted-foreground">
                        {getTipoLabel(evento.tipo, evento.tipoDescricao)}
                      </div>
                    </div>

                    <div className="mt-3 grid gap-2 text-sm">
                      <div>
                        <span className="text-muted-foreground">{t('events.fields.startDate')}: </span>
                        <span>{formatEventDate(evento.dataInicio)}</span>
                      </div>
                      <div>
                        <span className="text-muted-foreground">{t('events.fields.endDate')}: </span>
                        <span>{formatEventDate(evento.dataFim)}</span>
                      </div>
                      {evento.descricao && (
                        <p className="text-muted-foreground">
                          {evento.descricao.length > 120 ? `${evento.descricao.substring(0, 120)}...` : evento.descricao}
                        </p>
                      )}
                      {evento.url && (
                        <a href={evento.url} target="_blank" rel="noopener noreferrer" className="truncate text-blue-600 hover:underline">
                          {evento.url}
                        </a>
                      )}
                    </div>

                    <div className="mt-3 flex items-center justify-end gap-1 border-t pt-2">
                      <Button variant="ghost" size="icon" className="h-8 w-8" asChild title={t('events.actions.viewRegistrations')}>
                        <Link to={`/eventos/${evento.id}/inscricoes`}>
                          <Users className="h-4 w-4" />
                        </Link>
                      </Button>
                      {canEdit && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                          <Link to={`/eventos/${evento.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                      )}
                      {canDelete && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => handleDelete(evento.id)}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>

              <div className="hidden md:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <SortableTableHeader field="titulo" onSort={handleSort} sortConfig={sortConfig}>
                        {t('events.fields.title')}
                      </SortableTableHeader>
                      <TableHead>{t('events.fields.type')}</TableHead>
                      <SortableTableHeader field="descricao" onSort={handleSort} sortConfig={sortConfig}>
                        {t('events.fields.description')}
                      </SortableTableHeader>
                      <SortableTableHeader field="dataInicio" onSort={handleSort} sortConfig={sortConfig}>
                        {t('events.fields.startDate')}
                      </SortableTableHeader>
                      <SortableTableHeader field="dataFim" onSort={handleSort} sortConfig={sortConfig}>
                        {t('events.fields.endDate')}
                      </SortableTableHeader>
                      <TableHead>{t('events.fields.url')}</TableHead>
                      <TableHead className="text-right">{t('events.fields.actions')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {paginatedItems.map((evento) => (
                      <TableRow key={evento.id}>
                        <TableCell className="font-medium">{evento.titulo || '-'}</TableCell>
                        <TableCell>{getTipoLabel(evento.tipo, evento.tipoDescricao)}</TableCell>
                        <TableCell>{evento.descricao ? (evento.descricao.length > 50 ? `${evento.descricao.substring(0, 50)}...` : evento.descricao) : '-'}</TableCell>
                        <TableCell>{formatEventDate(evento.dataInicio)}</TableCell>
                        <TableCell>{formatEventDate(evento.dataFim)}</TableCell>
                        <TableCell>
                          {evento.url ? (
                            <a href={evento.url} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline">
                              {evento.url.length > 30 ? `${evento.url.substring(0, 30)}...` : evento.url}
                            </a>
                          ) : '-'}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end space-x-2">
                            <Button variant="ghost" size="sm" asChild title={t('events.actions.viewRegistrations')}>
                              <Link to={`/eventos/${evento.id}/inscricoes`}>
                                <Users className="h-4 w-4" />
                              </Link>
                            </Button>
                            {canEdit && (
                              <Button variant="ghost" size="sm" asChild>
                                <Link to={`/eventos/${evento.id}/editar`}>
                                  <Edit className="h-4 w-4" />
                                </Link>
                              </Button>
                            )}
                            {canDelete && (
                              <Button variant="ghost" size="sm" onClick={() => handleDelete(evento.id)}>
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
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
