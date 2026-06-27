import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { hubCasasApi, usuariosApi } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function CasasList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [usuarios, setUsuarios] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const confirmDialog = useConfirmDialog();
  const { can } = useAuth();

  const load = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const [c, u] = await Promise.all([
        hubCasasApi.getAll(),
        usuariosApi.getAll(),
      ]);
      setItems(c.data || []);
      setUsuarios(u.data || []);
    } catch (err) {
      setError(t('housesList.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const usuariosById = useMemo(() => {
    return new Map((usuarios || []).map((u) => [String(u.id), u.nome || u.email || String(u.id)]));
  }, [usuarios]);

  const getUsuarioNome = (casa, key) => {
    const nomeDireto = casa?.[`${key}Nome`] || casa?.[`${key}nome`];
    if (nomeDireto) return nomeDireto;

    const obj = casa?.[key];
    if (obj?.nome) return obj.nome;

    const id = casa?.[`${key}Id`] ?? obj?.id;
    if (id !== undefined && id !== null) {
      return usuariosById.get(String(id)) || `#${id}`;
    }

    return '-';
  };

  const handleDelete = async (id) => {
    const casa = items.find((c) => c.id === id);
    confirmDialog.show({
      title: t('housesList.deleteTitle'),
      description: t('housesList.deleteDescription', {
        name: casa?.nome || t('housesList.deleteFallbackName'),
      }),
      confirmText: t('housesList.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await hubCasasApi.delete(id);
          toast.success(t('housesList.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('housesList.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((casa) => {
    if (busca && !String(casa.nome || '').toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('housesList.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = can(RESOURCES.HUB, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.HUB, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('housesList.title')}</h1>
          <p className="text-muted-foreground">{t('housesList.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/hub/casas/novo">
                <Plus className="h-4 w-4 mr-2" /> {t('housesList.new')}
              </Link>
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('housesList.filters.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('housesList.filters.search')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('housesList.filters.searchPlaceholder')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('housesList.listTitle', { total })}</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('housesList.emptyTitle')}
              description={busca ? t('housesList.emptyFiltered') : t('housesList.emptyDescription')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/hub/casas/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('housesList.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('housesList.table.name')}</TableHead>
                  <TableHead>{t('housesList.table.openedBy')}</TableHead>
                  <TableHead>{t('housesList.table.leader')}</TableHead>
                  <TableHead>{t('housesList.table.timothy')}</TableHead>
                  <TableHead>{t('housesList.table.host')}</TableHead>
                  <TableHead>{t('housesList.table.address')}</TableHead>
                  <TableHead className="text-right">{t('housesList.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((casa) => (
                  <TableRow key={casa.id}>
                    <TableCell className="font-medium">{casa.nome}</TableCell>
                    <TableCell>{getUsuarioNome(casa, 'abertoPor')}</TableCell>
                    <TableCell>{getUsuarioNome(casa, 'lider')}</TableCell>
                    <TableCell>{getUsuarioNome(casa, 'timoteo')}</TableCell>
                    <TableCell>{casa.anfitriao || '-'}</TableCell>
                    <TableCell className="max-w-[260px] truncate" title={casa.enderecoCompleto || casa.endereco || ''}>
                      {casa.enderecoCompleto || casa.endereco || '-'}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-2">
                        {canEdit && (
                          <Button variant="ghost" size="sm" asChild>
                            <Link to={`/hub/casas/${casa.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </Button>
                        )}
                        {canDelete && (
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(casa.id)}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
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
