import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { AdvancedSearch } from '@/components/ui/advanced-search';
import { SortableTableHeader } from '@/components/ui/sortable-table-header';
import { useTableSort } from '@/hooks/useTableSort';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { exportToCSV } from '@/utils/export';
import { noticiasApi, categoriasNoticiasApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';

export default function NoticiasList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [filters, setFilters] = useState({
    titulo: '',
    descricao: '',
    categoriaId: undefined,
  });
  const confirmDialog = useConfirmDialog();

  const load = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const [noticiasRes, categoriasRes] = await Promise.all([
        noticiasApi.getAll(),
        categoriasNoticiasApi.getAll(),
      ]);
      setItems(noticiasRes.data || []);
      setCategorias(categoriasRes.data || []);
    } catch (err) {
      setError(t('news.errorLoad'));
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
    const noticia = items.find(n => n.id === id);
    confirmDialog.show({
      title: t('news.deleteTitle'),
      description: t('news.deleteDescription', {
        name: noticia?.titulo || t('news.deleteFallbackName'),
      }),
      confirmText: t('news.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await noticiasApi.delete(id);
          toast.success(t('news.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('news.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const getCategoriaNome = (id) => {
    const categoria = categorias.find((c) => c.id === id);
    return categoria ? categoria.nome : '-';
  };

  const filteredRaw = items.filter((n) => {
    // Busca por título
    if (filters.titulo && !n.titulo?.toLowerCase().includes(filters.titulo.toLowerCase())) {
      return false;
    }

    // Busca por descrição
    if (filters.descricao && !n.descricao?.toLowerCase().includes(filters.descricao.toLowerCase())) {
      return false;
    }

    // Filtro por categoria
    if (filters.categoriaId && String(n.categoriaNoticiaId) !== String(filters.categoriaId)) {
      return false;
    }

    return true;
  });

  // Preparar dados com nome da categoria para ordenação
  const filteredComCategoria = filteredRaw.map(n => ({
    ...n,
    categoriaNome: getCategoriaNome(n.categoriaNoticiaId),
  }));

  // Ordenação
  const { sortedData: filtered, sortConfig, handleSort } = useTableSort(filteredComCategoria, {
    defaultSort: 'data',
    defaultDirection: 'desc',
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  // Exportação
  const handleExport = () => {
    const exportData = filtered.map(noticia => ({
      [t('news.table.title')]: noticia.titulo || '',
      [t('news.table.description')]: noticia.descricao || '',
      [t('news.table.category')]: getCategoriaNome(noticia.categoriaNoticiaId),
      [t('news.table.date')]: noticia.data ? formatDate(noticia.data) : '',
      URL: noticia.url || '',
    }));

    exportToCSV(exportData, 'noticias');
    toast.success(t('news.exportSuccess'));
  };

  if (loading) return <LoadingPage text={t('news.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('news.title')}</h1>
          <p className="text-muted-foreground">{t('news.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button asChild>
            <Link to="/noticias/novo">
              <Plus className="h-4 w-4 mr-2" /> {t('news.new')}
            </Link>
          </Button>
        </div>
      </div>

      <AdvancedSearch
        searchFields={[
          { key: 'titulo', label: t('news.table.title'), type: 'text', placeholder: t('news.filters.searchTitle') },
          { key: 'descricao', label: t('news.table.description'), type: 'text', placeholder: t('news.filters.searchDescription') },
        ]}
        filterFields={[
          {
            key: 'categoriaId',
            label: t('news.table.category'),
            type: 'select',
            options: categorias.map(c => ({ value: c.id, label: c.nome })),
          },
        ]}
        values={filters}
        onChange={setFilters}
        onReset={() => {
          setFilters({
            titulo: '',
            descricao: '',
            categoriaId: undefined,
          });
        }}
      />

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>{t('news.listTitle', { total })}</CardTitle>
            {filtered.length > 0 && (
              <Button variant="outline" size="sm" onClick={handleExport}>
                <Download className="h-4 w-4 mr-2" />
                {t('news.exportCsv')}
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('news.emptyTitle')}
              description={t('news.emptyDescription')}
              action={(
                <Button asChild>
                  <Link to="/noticias/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('news.new')}
                  </Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <SortableTableHeader field="titulo" onSort={handleSort} sortConfig={sortConfig}>
                    {t('news.table.title')}
                  </SortableTableHeader>
                  <SortableTableHeader field="descricao" onSort={handleSort} sortConfig={sortConfig}>
                    {t('news.table.description')}
                  </SortableTableHeader>
                  <SortableTableHeader field="categoriaNome" onSort={handleSort} sortConfig={sortConfig}>
                    {t('news.table.category')}
                  </SortableTableHeader>
                  <SortableTableHeader field="data" onSort={handleSort} sortConfig={sortConfig}>
                    {t('news.table.date')}
                  </SortableTableHeader>
                  <TableHead>URL</TableHead>
                  <TableHead className="text-right">{t('news.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((noticia) => (
                  <TableRow key={noticia.id}>
                    <TableCell className="font-medium">{noticia.titulo || '-'}</TableCell>
                    <TableCell>{noticia.descricao ? (noticia.descricao.length > 50 ? `${noticia.descricao.substring(0, 50)}...` : noticia.descricao) : '-'}</TableCell>
                    <TableCell>{getCategoriaNome(noticia.categoriaNoticiaId)}</TableCell>
                    <TableCell>{noticia.data ? formatDate(noticia.data) : '-'}</TableCell>
                    <TableCell>
                      {noticia.url ? (
                        <a href={noticia.url} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline">
                          {noticia.url.length > 30 ? `${noticia.url.substring(0, 30)}...` : noticia.url}
                        </a>
                      ) : '-'}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-2">
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={`/noticias/${noticia.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => handleDelete(noticia.id)}>
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
