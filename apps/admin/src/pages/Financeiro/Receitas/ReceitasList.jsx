import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Search, RefreshCw, User } from 'lucide-react';
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
import { receitasApi } from '@/lib/api';
import { formatCurrency, formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function ReceitasList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
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
      const res = await receitasApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('finance.revenues.errorLoad'));
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
    const receita = items.find((r) => r.id === id);
    confirmDialog.show({
      title: t('finance.revenues.delete.title'),
      description: t('finance.revenues.delete.description', { name: receita?.descricao || t('finance.revenues.emptyMessage') }),
      confirmText: t('finance.revenues.delete.confirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await receitasApi.delete(id);
          toast.success(t('finance.revenues.delete.success'));
          await load();
        } catch (err) {
          toast.error(t('finance.revenues.delete.error'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const getStatusLabel = (status) => {
    const statusMap = {
      'Pendente': t('finance.revenues.status.pending'),
      'Recebido': t('finance.revenues.status.received'),
      'Cancelado': t('finance.revenues.status.canceled'),
    };
    return statusMap[status] || status;
  };

  const getStatusColor = (status) => {
    const colorMap = {
      'Pendente': 'bg-yellow-100 text-yellow-800',
      'Recebido': 'bg-green-100 text-green-800',
      'Cancelado': 'bg-gray-100 text-gray-800',
    };
    return colorMap[status] || 'bg-gray-100 text-gray-800';
  };

  const filtered = items.filter((r) => {
    if (!busca) return true;
    const termo = busca.toLowerCase();
    return (
      String(r.descricao || '').toLowerCase().includes(termo) ||
      String(r.pessoaNome || '').toLowerCase().includes(termo) ||
      String(r.categoriaReceitaNome || '').toLowerCase().includes(termo)
    );
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('finance.revenues.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = can(RESOURCES.FINANCEIRO, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.FINANCEIRO, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('finance.revenues.title')}</h1>
          <p className="text-muted-foreground">{t('finance.revenues.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/financeiro/receitas/novo">
                <Plus className="h-4 w-4 mr-2" /> {t('finance.revenues.new')}
              </Link>
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.common.filters')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('finance.revenues.filters.searchLabel')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('finance.revenues.filters.searchPlaceholder')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.revenues.listTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('finance.revenues.emptyTitle', 'Nenhuma receita encontrada')}
              description={t('finance.revenues.emptyMessage')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/financeiro/receitas/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('finance.revenues.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('finance.revenues.table.description')}</TableHead>
                  <TableHead>Membro</TableHead>
                  <TableHead>{t('finance.revenues.table.value')}</TableHead>
                  <TableHead>{t('finance.revenues.table.date')}</TableHead>
                  <TableHead>Categoria</TableHead>
                  <TableHead>{t('finance.revenues.table.status')}</TableHead>
                  <TableHead className="text-right">{t('finance.revenues.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell className="font-medium">
                      <div>{r.descricao}</div>
                      {r.recorrente && (
                        <span className="text-xs text-muted-foreground flex items-center gap-1">
                          <RefreshCw className="h-3 w-3" /> {r.tipoRecorrenciaDescricao}
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      {r.pessoaNome ? (
                        <span className="flex items-center gap-1 text-sm">
                          <User className="h-3 w-3 text-muted-foreground" />
                          {r.pessoaNome}
                        </span>
                      ) : (
                        <span className="text-muted-foreground text-sm">-</span>
                      )}
                    </TableCell>
                    <TableCell className="font-semibold">{formatCurrency(r.valor)}</TableCell>
                    <TableCell>{formatDate(r.dataRecebimento)}</TableCell>
                    <TableCell>{r.categoriaReceitaNome || '-'}</TableCell>
                    <TableCell>
                      <span className={`px-2 py-1 rounded text-xs ${getStatusColor(r.status)}`}>
                        {getStatusLabel(r.status)}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-2">
                        {canEdit && (
                          <Button variant="ghost" size="sm" asChild>
                            <Link to={`/financeiro/receitas/${r.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </Button>
                        )}
                        {canDelete && (
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(r.id)}>
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
