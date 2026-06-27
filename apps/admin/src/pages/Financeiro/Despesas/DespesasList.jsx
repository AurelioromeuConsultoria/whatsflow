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
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { despesasApi, fornecedoresApi, categoriasDespesasApi, contasBancariasApi, centrosCustosApi, projetosApi } from '@/lib/api';
import { formatCurrency, formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function DespesasList() {
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
      const res = await despesasApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('finance.expenses.errorLoad'));
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
    const despesa = items.find((d) => d.id === id);
    confirmDialog.show({
      title: t('finance.expenses.delete.title'),
      description: t('finance.expenses.delete.description', { name: despesa?.descricao || t('finance.expenses.emptyMessage') }),
      confirmText: t('finance.expenses.delete.confirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await despesasApi.delete(id);
          toast.success(t('finance.expenses.delete.success'));
          await load();
        } catch (err) {
          toast.error(t('finance.expenses.delete.error'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const getStatusLabel = (status) => {
    const statusMap = {
      'Pendente': t('finance.expenses.status.pending'),
      'Pago': t('finance.expenses.status.paid'),
      'Cancelado': t('finance.expenses.status.canceled'),
    };
    return statusMap[status] || status;
  };

  const getStatusColor = (status) => {
    const colorMap = {
      'Pendente': 'bg-yellow-100 text-yellow-800',
      'Pago': 'bg-green-100 text-green-800',
      'Cancelado': 'bg-gray-100 text-gray-800',
    };
    return colorMap[status] || 'bg-gray-100 text-gray-800';
  };

  const filtered = items.filter((d) => {
    if (busca && !String(d.descricao || '').toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('finance.expenses.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = can(RESOURCES.FINANCEIRO, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.FINANCEIRO, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('finance.expenses.title')}</h1>
          <p className="text-muted-foreground">{t('finance.expenses.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/financeiro/despesas/novo">
                <Plus className="h-4 w-4 mr-2" /> {t('finance.expenses.new')}
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
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('finance.expenses.filters.searchLabel')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('finance.expenses.filters.searchPlaceholder')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.expenses.listTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('finance.expenses.emptyTitle', 'Nenhuma despesa encontrada')}
              description={t('finance.expenses.emptyMessage')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/financeiro/despesas/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('finance.expenses.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('finance.expenses.table.description')}</TableHead>
                  <TableHead>{t('finance.expenses.table.value')}</TableHead>
                  <TableHead>{t('finance.expenses.table.dueDate')}</TableHead>
                  <TableHead>{t('finance.expenses.table.supplier')}</TableHead>
                  <TableHead>{t('finance.expenses.table.category')}</TableHead>
                  <TableHead>{t('finance.expenses.table.status')}</TableHead>
                  <TableHead className="text-right">{t('finance.expenses.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell className="font-medium">{d.descricao}</TableCell>
                    <TableCell>{formatCurrency(d.valor)}</TableCell>
                    <TableCell>{formatDate(d.dataVencimento)}</TableCell>
                    <TableCell>{d.fornecedor?.nome || '-'}</TableCell>
                    <TableCell>{d.categoriaDespesa?.nome || '-'}</TableCell>
                    <TableCell>
                      <span className={`px-2 py-1 rounded text-xs ${getStatusColor(d.status)}`}>
                        {getStatusLabel(d.status)}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-2">
                        {canEdit && (
                          <Button variant="ghost" size="sm" asChild>
                            <Link to={`/financeiro/despesas/${d.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </Button>
                        )}
                        {canDelete && (
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(d.id)}>
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
