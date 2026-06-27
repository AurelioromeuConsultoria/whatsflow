import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Filter, BarChart3, Calendar, CheckCircle, XCircle, Search } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { enquetesApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';

export default function EnquetesList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const confirmDialog = useConfirmDialog();

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
      const res = await enquetesApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('pollsManagement.errorLoad'));
      console.error(err);
      toast.error(t('pollsManagement.errorLoad'));
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
    const enquete = items.find((e) => e.id === id);
    confirmDialog.show({
      title: t('pollsManagement.deleteTitle'),
      description: t('pollsManagement.deleteDescription', {
        name: enquete?.titulo || t('pollsManagement.deleteFallbackName'),
      }),
      confirmText: t('pollsManagement.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await enquetesApi.delete(id);
          toast.success(t('pollsManagement.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('pollsManagement.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((e) => {
    if (busca && !e.titulo?.toLowerCase().includes(busca.toLowerCase()) && !e.descricao?.toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  const isAtiva = (enquete) => {
    const agora = new Date();
    const inicio = new Date(enquete.dataInicio);
    const fim = new Date(enquete.dataFim);
    return enquete.ativo && inicio <= agora && fim >= agora;
  };

  if (loading) return <LoadingPage text={t('pollsManagement.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-foreground">{t('pollsManagement.title')}</h1>
          <p className="text-muted-foreground">{t('pollsManagement.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button asChild>
              <Link to="/enquetes/novo">
              <Plus className="h-4 w-4 mr-2" /> {t('pollsManagement.new')}
            </Link>
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Filter className="h-5 w-5" />
            {t('pollsManagement.filtersTitle')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground flex items-center gap-2"><Search className="h-4 w-4" />{t('pollsManagement.searchLabel')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('pollsManagement.searchPlaceholder')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('pollsManagement.listTitle', { total })}</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('pollsManagement.emptyTitle')}
              description={items.length === 0 ? t('pollsManagement.emptyDescriptionFirst') : t('pollsManagement.emptyDescriptionFiltered')}
              action={items.length === 0 ? (
                <Button asChild>
                  <Link to="/enquetes/novo">
                    <Plus className="h-4 w-4 mr-2" />
                    {t('pollsManagement.createFirst')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('pollsManagement.table.title')}</TableHead>
                  <TableHead>{t('pollsManagement.table.description')}</TableHead>
                  <TableHead>{t('pollsManagement.table.period')}</TableHead>
                  <TableHead>{t('pollsManagement.table.options')}</TableHead>
                  <TableHead>{t('pollsManagement.table.votes')}</TableHead>
                  <TableHead>{t('pollsManagement.table.status')}</TableHead>
                  <TableHead className="text-right">{t('pollsManagement.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((enquete) => (
                  <TableRow key={enquete.id}>
                    <TableCell className="font-medium text-foreground">
                      {enquete.titulo}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {enquete.descricao ? (
                        <span className="line-clamp-2">{enquete.descricao}</span>
                      ) : (
                        '-'
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Calendar className="h-4 w-4" />
                        <span>{formatDate(enquete.dataInicio)} - {formatDate(enquete.dataFim)}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">{t('pollsManagement.optionsCount', { count: enquete.opcoes?.length || 0 })}</Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <BarChart3 className="h-4 w-4 text-muted-foreground" />
                        <span className="text-sm text-foreground">{enquete.totalVotos || 0}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      {isAtiva(enquete) ? (
                        <Badge variant="default" className="bg-green-500 hover:bg-green-600 dark:bg-green-600 dark:hover:bg-green-700">
                          <CheckCircle className="h-3 w-3 mr-1" />
                          {t('pollsManagement.status.active')}
                        </Badge>
                      ) : (
                        <Badge variant="secondary">
                          <XCircle className="h-3 w-3 mr-1" />
                          {enquete.ativo ? t('pollsManagement.status.waiting') : t('pollsManagement.status.inactive')}
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-2">
                        <Button variant="ghost" size="sm" asChild title={t('actions.edit')}>
                          <Link to={`/enquetes/${enquete.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(enquete.id)}
                          title={t('pollsManagement.deleteConfirm')}
                          className="text-destructive hover:text-destructive"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
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
