import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Filter, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { TableRowActions, RowIconButtonAction, RowIconLinkAction } from '@/components/ui/list-actions';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { destaquesSiteApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function DestaquesSiteList() {
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
      const res = await destaquesSiteApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('siteHighlightsManagement.errorLoad'));
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
    const destaque = items.find(d => d.id === id);
    confirmDialog.show({
      title: t('siteHighlightsManagement.deleteTitle'),
      description: t('siteHighlightsManagement.deleteDescription', {
        name: destaque?.texto || t('siteHighlightsManagement.fallbackName'),
      }),
      confirmText: t('actions.remove'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await destaquesSiteApi.delete(id);
          toast.success(t('siteHighlightsManagement.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('siteHighlightsManagement.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((d) => {
    if (busca && !d.texto?.toLowerCase().includes(busca.toLowerCase()) && !d.descricao?.toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('siteHighlightsManagement.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('siteHighlightsManagement.title')}</h1>
          <p className="text-muted-foreground">{t('siteHighlightsManagement.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button asChild>
            <Link to="/destaques-site/novo">
              <Plus className="h-4 w-4 mr-2" /> {t('siteHighlightsManagement.actions.new')}
            </Link>
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('siteHighlightsManagement.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('siteHighlightsManagement.searchLabel')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('siteHighlightsManagement.searchPlaceholder')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('siteHighlightsManagement.listTitle', { total })}</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('siteHighlightsManagement.emptyTitle')}
              description={t('siteHighlightsManagement.emptyDescription')}
              action={(
                <Button asChild>
                  <Link to="/destaques-site/novo">
                    <Plus className="h-4 w-4 mr-2" /> {t('siteHighlightsManagement.actions.new')}
                  </Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('siteHighlightsManagement.table.text')}</TableHead>
                  <TableHead>{t('siteHighlightsManagement.table.description')}</TableHead>
                  <TableHead>URL</TableHead>
                  <TableHead>{t('siteHighlightsManagement.table.createdAt')}</TableHead>
                  <TableHead className="text-right">{t('siteHighlightsManagement.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((destaque) => (
                  <TableRow key={destaque.id}>
                    <TableCell className="font-medium">{destaque.texto || t('common.notInformed')}</TableCell>
                    <TableCell>{destaque.descricao || t('common.notInformed')}</TableCell>
                    <TableCell>
                      {destaque.url ? (
                        <a href={destaque.url} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline">
                          {destaque.url.length > 30 ? `${destaque.url.substring(0, 30)}...` : destaque.url}
                        </a>
                      ) : t('common.notInformed')}
                    </TableCell>
                    <TableCell>{formatDate(destaque.dataCriacao)}</TableCell>
                    <TableCell className="text-right">
                      <TableRowActions>
                        <RowIconLinkAction>
                          <Link to={`/destaques-site/${destaque.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </RowIconLinkAction>
                        <RowIconButtonAction onClick={() => handleDelete(destaque.id)}>
                          <Trash2 className="h-4 w-4" />
                        </RowIconButtonAction>
                      </TableRowActions>
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
