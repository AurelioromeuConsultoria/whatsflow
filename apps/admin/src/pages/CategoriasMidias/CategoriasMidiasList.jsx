import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Filter } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { formatDate } from '@/lib/formatters';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { categoriasMidiasApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';

export default function CategoriasMidiasList() {
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
      const res = await categoriasMidiasApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('mediaCategories.errorLoad'));
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
    const categoria = items.find((c) => c.id === id);
    confirmDialog.show({
      title: t('mediaCategories.deleteTitle'),
      description: t('mediaCategories.deleteDescription', {
        name: categoria?.nome || t('mediaCategories.deleteFallbackName'),
      }),
      confirmText: t('mediaCategories.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await categoriasMidiasApi.delete(id);
          toast.success(t('mediaCategories.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('mediaCategories.deleteErrorInUse')));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((c) => {
    if (busca && !c.nome?.toLowerCase().includes(busca.toLowerCase()) && !c.descricao?.toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  if (loading) return <LoadingPage text={t('mediaCategories.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('mediaCategories.title')}</h1>
          <p className="text-muted-foreground">{t('mediaCategories.subtitle')}</p>
        </div>
        <Button asChild>
          <Link to="/categorias-midias/novo">
            <Plus className="h-4 w-4 mr-2" /> {t('mediaCategories.new')}
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('mediaCategories.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Filter className="h-4 w-4" />{t('mediaCategories.searchLabel')}</label>
              <input
                className="w-full px-3 py-2 border rounded"
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('mediaCategories.searchPlaceholder')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('mediaCategories.listTitle')}</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('mediaCategories.emptyTitle')}
              description={t('mediaCategories.emptyDescription')}
              action={(
                <Button asChild>
                  <Link to="/categorias-midias/novo">
                    <Plus className="h-4 w-4 mr-2" /> {t('mediaCategories.new')}
                  </Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('mediaCategories.table.name')}</TableHead>
                  <TableHead>{t('mediaCategories.table.description')}</TableHead>
                  <TableHead>{t('mediaCategories.table.createdAt')}</TableHead>
                  <TableHead className="text-right">{t('mediaCategories.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filtered.map((categoria) => (
                  <TableRow key={categoria.id}>
                    <TableCell className="font-medium">{categoria.nome}</TableCell>
                    <TableCell>{categoria.descricao || '-'}</TableCell>
                    <TableCell>{formatDate(categoria.dataCriacao)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-2">
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={`/categorias-midias/${categoria.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => handleDelete(categoria.id)}>
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

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


