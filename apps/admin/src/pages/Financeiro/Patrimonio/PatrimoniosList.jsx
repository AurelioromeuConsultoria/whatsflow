import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Search, BarChart3 } from 'lucide-react';
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
import { patrimonioApi, categoriasPatrimonioApi } from '@/lib/api';
import { formatCurrency } from '@/lib/formatters';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

const statusKeyMap = {
  EmUso: 'inUse',
  EmManutencao: 'inMaintenance',
  Emprestado: 'loaned',
  Ocioso: 'idle',
  Baixado: 'disposed',
};

export default function PatrimoniosList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const [categoriaId, setCategoriaId] = useState('');
  const [status, setStatus] = useState('');
  const confirmDialog = useConfirmDialog();
  const { can } = useAuth();

  const load = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const [itemsRes, categoriasRes] = await Promise.all([
        patrimonioApi.getAll(),
        categoriasPatrimonioApi.getAll(),
      ]);
      setItems(itemsRes.data || []);
      setCategorias(categoriasRes.data || []);
    } catch (err) {
      setError(t('finance.patrimony.errorLoad'));
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
    const item = items.find((registro) => registro.id === id);
    confirmDialog.show({
      title: t('finance.patrimony.deleteTitle'),
      description: t('finance.patrimony.deleteDescription', { name: item?.nome || t('finance.patrimony.emptyMessage') }),
      confirmText: t('finance.expenses.delete.confirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await patrimonioApi.delete(id);
          toast.success(t('finance.patrimony.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(err.response?.data?.message || t('finance.patrimony.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = useMemo(() => items.filter((item) => {
    const termo = busca.trim().toLowerCase();

    if (termo) {
      const matches = [item.nome, item.codigo, item.numeroSerie, item.localizacao]
        .some((value) => String(value || '').toLowerCase().includes(termo));
      if (!matches) return false;
    }

    if (categoriaId && String(item.categoriaPatrimonioId) !== categoriaId) return false;
    if (status && item.status !== status) return false;

    return true;
  }), [items, busca, categoriaId, status]);

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  const resumo = useMemo(() => ({
    totalItens: items.length,
    totalAtivos: items.filter((item) => item.ativo).length,
    emManutencao: items.filter((item) => item.status === 'EmManutencao').length,
    valorTotal: items.reduce((acc, item) => acc + Number(item.valorAquisicao || 0), 0),
  }), [items]);

  if (loading) return <LoadingPage text={t('finance.patrimony.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = can(RESOURCES.FINANCEIRO, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.FINANCEIRO, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('finance.patrimony.title')}</h1>
          <p className="text-muted-foreground">{t('finance.patrimony.subtitle')}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button variant="outline" asChild>
            <Link to="/financeiro/patrimonio/relatorio-geral">
              <BarChart3 className="mr-2 h-4 w-4" /> {t('finance.patrimony.report')}
            </Link>
          </Button>
          {canEdit && (
            <Button asChild>
              <Link to="/financeiro/patrimonio/novo">
                <Plus className="h-4 w-4 mr-2" /> {t('finance.patrimony.new')}
              </Link>
            </Button>
          )}
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimony.summary.totalItems')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.totalItens}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimony.summary.activeItems')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.totalAtivos}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimony.summary.inMaintenance')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.emManutencao}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimony.summary.totalValue')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{formatCurrency(resumo.valorTotal)}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.common.filters')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <Search className="h-4 w-4" />
                {t('finance.patrimony.filters.searchLabel')}
              </label>
              <Input value={busca} onChange={(e) => setBusca(e.target.value)} placeholder={t('finance.patrimony.filters.searchPlaceholder')} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('finance.patrimony.fields.category')}</label>
              <select value={categoriaId} onChange={(e) => setCategoriaId(e.target.value)} className="w-full px-3 py-2 border rounded">
                <option value="">{t('finance.patrimony.filters.allCategories')}</option>
                {categorias.map((categoria) => (
                  <option key={categoria.id} value={categoria.id}>{categoria.nome}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('finance.patrimony.fields.status')}</label>
              <select value={status} onChange={(e) => setStatus(e.target.value)} className="w-full px-3 py-2 border rounded">
                <option value="">{t('finance.patrimony.filters.allStatus')}</option>
                {Object.entries(statusKeyMap).map(([value, key]) => (
                  <option key={value} value={value}>{t(`finance.patrimony.status.${key}`)}</option>
                ))}
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.patrimony.listTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('finance.patrimony.emptyTitle', 'Nenhum patrimônio encontrado')}
              description={t('finance.patrimony.emptyMessage')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/financeiro/patrimonio/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('finance.patrimony.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('finance.patrimony.table.code')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.name')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.category')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.location')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.responsible')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.status')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.value')}</TableHead>
                  <TableHead className="text-right">{t('finance.expenses.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className="font-medium">{item.codigo}</TableCell>
                    <TableCell>
                      <Link to={`/financeiro/patrimonio/${item.id}`} className="font-medium hover:underline">
                        {item.nome}
                      </Link>
                    </TableCell>
                    <TableCell>{item.categoriaNome || '-'}</TableCell>
                    <TableCell>{item.localizacao || '-'}</TableCell>
                    <TableCell>{item.responsavelNome || '-'}</TableCell>
                    <TableCell>{t(`finance.patrimony.status.${statusKeyMap[item.status] || 'inUse'}`)}</TableCell>
                    <TableCell>{item.valorAquisicao ? formatCurrency(item.valorAquisicao) : '-'}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-2">
                        {canEdit && (
                          <Button variant="ghost" size="sm" asChild>
                            <Link to={`/financeiro/patrimonio/${item.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </Button>
                        )}
                        {canDelete && (
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(item.id)}>
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
