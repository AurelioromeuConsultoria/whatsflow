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
import { BooleanStatusBadge } from '@/components/ui/status-badge';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { contasBancariasApi } from '@/lib/api';
import { formatCurrency } from '@/lib/formatters';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function ContasBancariasList() {
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
      const res = await contasBancariasApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('finance.bankAccounts.errorLoad'));
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
    const conta = items.find((c) => c.id === id);
    confirmDialog.show({
      title: t('finance.bankAccounts.deleteTitle'),
      description: t('finance.bankAccounts.deleteDescription', { name: conta?.nome || t('finance.bankAccounts.emptyMessage') }),
      confirmText: t('finance.revenues.delete.confirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await contasBancariasApi.delete(id);
          toast.success(t('finance.bankAccounts.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('finance.bankAccounts.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((c) => {
    if (busca && !String(c.nome || '').toLowerCase().includes(busca.toLowerCase())) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('finance.bankAccounts.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = can(RESOURCES.FINANCEIRO, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.FINANCEIRO, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('finance.bankAccounts.title')}</h1>
          <p className="text-muted-foreground">{t('finance.bankAccounts.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/financeiro/contas-bancarias/novo">
                <Plus className="h-4 w-4 mr-2" /> {t('finance.bankAccounts.new')}
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
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('finance.common.searchByName')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('finance.common.searchByName')}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.bankAccounts.listTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title="Nenhuma conta bancaria encontrada"
              description={t('finance.bankAccounts.emptyMessage')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/financeiro/contas-bancarias/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('finance.bankAccounts.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('finance.common.name')}</TableHead>
                  <TableHead>{t('finance.bankAccounts.tableBank')}</TableHead>
                  <TableHead>{t('finance.bankAccounts.tableAgency')}</TableHead>
                  <TableHead>{t('finance.bankAccounts.tableAccount')}</TableHead>
                  <TableHead>{t('finance.bankAccounts.tableType')}</TableHead>
                  <TableHead>{t('finance.bankAccounts.tableInitialBalance')}</TableHead>
                  <TableHead>{t('finance.common.status')}</TableHead>
                  <TableHead className="text-right">{t('finance.revenues.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((c) => (
                  <TableRow key={c.id}>
                    <TableCell className="font-medium">{c.nome}</TableCell>
                    <TableCell>{c.banco || '-'}</TableCell>
                    <TableCell>{c.agencia || '-'}</TableCell>
                    <TableCell>{c.conta || '-'}</TableCell>
                    <TableCell>{c.tipoConta || '-'}</TableCell>
                    <TableCell>{formatCurrency(c.saldoInicial)}</TableCell>
                    <TableCell>
                      <BooleanStatusBadge
                        value={c.ativo}
                        trueLabel={t('finance.revenueCategories.statusActive')}
                        falseLabel={t('finance.revenueCategories.statusInactive')}
                      />
                    </TableCell>
                    <TableCell className="text-right">
                      <TableRowActions>
                        {canEdit && (
                          <RowIconLinkAction>
                            <Link to={`/financeiro/contas-bancarias/${c.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </RowIconLinkAction>
                        )}
                        {canDelete && (
                          <RowIconButtonAction onClick={() => handleDelete(c.id)}>
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
