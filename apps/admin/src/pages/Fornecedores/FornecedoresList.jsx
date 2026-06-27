import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Search } from 'lucide-react';
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
import { fornecedoresApi } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function FornecedoresList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const confirmDialog = useConfirmDialog();
  const { can } = useAuth();

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
      const res = await fornecedoresApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('suppliers.errorLoad'));
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
    const fornecedor = items.find((f) => f.id === id);
    confirmDialog.show({
      title: t('suppliers.deleteTitle'),
      description: t('suppliers.deleteDescription', { name: fornecedor?.nome || t('suppliers.emptyMessage') }),
      confirmText: t('accessProfiles.permissionDelete'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await fornecedoresApi.delete(id);
          toast.success(t('suppliers.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('suppliers.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((f) => {
    if (busca && !String(f.nome || '').toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('suppliers.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = can(RESOURCES.FORNECEDORES, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.FORNECEDORES, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('suppliers.title')}</h1>
          <p className="text-muted-foreground">{t('suppliers.subtitle')}</p>
        </div>
        {canEdit && (
          <Button asChild>
            <Link to="/financeiro/fornecedores/novo">
              <Plus className="h-4 w-4 mr-2" /> {t('suppliers.new')}
            </Link>
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('suppliers.filters')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('suppliers.searchByName')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('suppliers.placeholderName')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('suppliers.listTitle')} ({total})</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('suppliers.emptyMessage')}
              description="Tente ajustar os filtros ou cadastre um novo fornecedor."
              action={canEdit ? (
                <Button asChild>
                  <Link to="/financeiro/fornecedores/novo">
                    <Plus className="h-4 w-4 mr-2" /> {t('suppliers.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('suppliers.name')}</TableHead>
                  <TableHead>{t('suppliers.cnpjCpf')}</TableHead>
                  <TableHead>{t('suppliers.phone')}</TableHead>
                  <TableHead>{t('suppliers.contact')}</TableHead>
                  <TableHead className="text-right">{t('suppliers.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((f) => (
                  <TableRow key={f.id}>
                    <TableCell className="font-medium">{f.nome}</TableCell>
                    <TableCell>{f.cnpjCpf || '-'}</TableCell>
                    <TableCell>{f.telefone || '-'}</TableCell>
                    <TableCell>{f.contatoNome || '-'}</TableCell>
                    <TableCell className="text-right">
                      <TableRowActions>
                        {canEdit && (
                          <RowIconLinkAction>
                            <Link to={`/financeiro/fornecedores/${f.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </RowIconLinkAction>
                        )}
                        {canDelete && (
                          <RowIconButtonAction onClick={() => handleDelete(f.id)}>
                            <Trash2 className="h-4 w-4" />
                          </RowIconButtonAction>
                        )}
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
