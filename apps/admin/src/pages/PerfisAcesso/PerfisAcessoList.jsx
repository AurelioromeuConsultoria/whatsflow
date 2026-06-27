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
import { perfisAcessoApi } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function PerfisAcessoList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const confirmDialog = useConfirmDialog();
  const { can, isAdmin } = useAuth();

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
      const res = await perfisAcessoApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('accessProfiles.errorLoad'));
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
    const perfil = items.find((p) => p.id === id);
    confirmDialog.show({
      title: t('accessProfiles.deleteTitle'),
      description: t('accessProfiles.deleteDescription', { name: perfil?.nome || t('accessProfiles.emptyMessage') }),
      confirmText: t('accessProfiles.permissionDelete'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await perfisAcessoApi.delete(id);
          toast.success(t('accessProfiles.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('accessProfiles.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((p) => {
    if (busca && !String(p.nome || '').toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('accessProfiles.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = isAdmin && can(RESOURCES.PERFIS_ACESSO, ACTIONS.EDIT);
  const canDelete = isAdmin && can(RESOURCES.PERFIS_ACESSO, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('accessProfiles.title')}</h1>
          <p className="text-muted-foreground">{t('accessProfiles.subtitle')}</p>
        </div>
        {canEdit && (
          <Button asChild>
            <Link to="/perfis-acesso/novo">
              <Plus className="h-4 w-4 mr-2" /> {t('accessProfiles.new')}
            </Link>
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('accessProfiles.filters')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('accessProfiles.searchByName')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('accessProfiles.placeholderName')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('accessProfiles.listTitle')} ({total})</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('accessProfiles.emptyMessage')}
              description="Ajuste os filtros ou crie um novo perfil de acesso."
              action={canEdit ? (
                <Button asChild>
                  <Link to="/perfis-acesso/novo">
                    <Plus className="h-4 w-4 mr-2" /> {t('accessProfiles.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('accessProfiles.name')}</TableHead>
                  <TableHead>{t('accessProfiles.description')}</TableHead>
                  <TableHead>{t('accessProfiles.permissions')}</TableHead>
                  <TableHead className="text-right">{t('accessProfiles.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell className="font-medium">{p.nome}</TableCell>
                    <TableCell>{p.descricao || '-'}</TableCell>
                    <TableCell>{p.permissoes?.length || 0}</TableCell>
                    <TableCell className="text-right">
                      <TableRowActions>
                        {canEdit && (
                          <RowIconLinkAction>
                            <Link to={`/perfis-acesso/${p.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </RowIconLinkAction>
                        )}
                        {canDelete && (
                          <RowIconButtonAction onClick={() => handleDelete(p.id)}>
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
