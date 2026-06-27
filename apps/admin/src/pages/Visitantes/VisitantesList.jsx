import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Eye, Edit, Trash2, Phone, Mail, Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { AdvancedSearch } from '@/components/ui/advanced-search';
import { SortableTableHeader } from '@/components/ui/sortable-table-header';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { exportToCSV } from '@/utils/export';
import { visitantesApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { useTranslation } from 'react-i18next';

export default function VisitantesList() {
  const [visitantes, setVisitantes] = useState([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [filters, setFilters] = useState({
    nome: '',
    email: '',
    telefone: '',
    whatsApp: '',
    dataVisita_from: '',
    dataVisita_to: '',
  });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [sortConfig, setSortConfig] = useState({ field: 'dataVisita', direction: 'desc' });
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [bulkDeleteDialogOpen, setBulkDeleteDialogOpen] = useState(false);
  const [bulkDeleting, setBulkDeleting] = useState(false);
  const confirmDialog = useConfirmDialog();
  const { isAdmin } = useAuth();
  const { t } = useTranslation();

  const loadVisitantes = useCallback(async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const response = await visitantesApi.getPaged({
        page,
        pageSize,
        sort: sortConfig.field,
        direction: sortConfig.direction,
        nome: filters.nome || undefined,
        email: filters.email || undefined,
        telefone: filters.telefone || undefined,
        whatsApp: filters.whatsApp || undefined,
        dataVisitaFrom: filters.dataVisita_from || undefined,
        dataVisitaTo: filters.dataVisita_to || undefined,
      });

      const data = response.data || {};
      setVisitantes(data.items || []);
      setTotal(Number(data.total || 0));
    } catch (err) {
      setError(t('visitors.errorLoad'));
      console.error('Erro ao carregar visitantes:', err);
      toast.error(t('visitors.errorLoad'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [filters, page, pageSize, sortConfig.direction, sortConfig.field]);

  const handleDelete = async (id) => {
    const visitante = visitantes.find(v => v.id === id);
    const pessoaNome = visitante?.nome || t('visitors.visitFallback');
    const currentPageCount = visitantes.length;
    confirmDialog.show({
      title: t('visitors.deleteTitle'),
      description: t('visitors.deleteDescription', { name: pessoaNome }),
      confirmText: t('visitors.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await visitantesApi.delete(id);
          toast.success(t('visitors.deleteSuccess'));
          await loadVisitantes();
          if (page > 1 && currentPageCount === 1) {
            setPage((p) => Math.max(1, p - 1));
          }
        } catch (err) {
          toast.error(t('visitors.deleteError'));
          console.error('Erro ao excluir visita:', err);
          throw err;
        }
      },
    });
  };

  useEffect(() => {
    loadVisitantes();
  }, [loadVisitantes]);

  useEffect(() => {
    setSelectedIds(new Set());
  }, [page, filters]);

  const pageIds = visitantes.map((v) => v.id);
  const allPageSelected = pageIds.length > 0 && pageIds.every((id) => selectedIds.has(id));

  const toggleSelectAll = () => {
    if (allPageSelected) {
      setSelectedIds((prev) => {
        const next = new Set(prev);
        pageIds.forEach((id) => next.delete(id));
        return next;
      });
    } else {
      setSelectedIds((prev) => {
        const next = new Set(prev);
        pageIds.forEach((id) => next.add(id));
        return next;
      });
    }
  };

  const toggleSelect = (id) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleBulkDeleteClick = () => {
    if (selectedIds.size === 0) return;
    setBulkDeleteDialogOpen(true);
  };

  const handleBulkDeleteConfirm = async () => {
    const ids = Array.from(selectedIds);
    if (ids.length === 0) return;

    try {
      setBulkDeleting(true);
      let ok = 0;
      let fail = 0;
      for (const id of ids) {
        try {
          await visitantesApi.delete(id);
          ok += 1;
        } catch {
          fail += 1;
        }
      }
      setSelectedIds(new Set());
      setBulkDeleteDialogOpen(false);
      await loadVisitantes();
      if (page > 1 && visitantes.length === ids.length) setPage((p) => Math.max(1, p - 1));
      if (fail > 0) {
        toast.warning(t('visitors.bulkDeletePartial', { ok, fail }));
      } else {
        toast.success(t('visitors.bulkDeleteSuccess', { count: ok }));
      }
    } catch {
      toast.error(t('visitors.bulkDeleteError'));
    } finally {
      setBulkDeleting(false);
    }
  };

  const handleSort = (field) => {
    setSortConfig((prev) => {
      if (prev.field === field) {
        return { field, direction: prev.direction === 'asc' ? 'desc' : 'asc' };
      }
      return { field, direction: 'asc' };
    });
    setPage(1);
  };

  // Exportação
  const handleExport = async () => {
    try {
      const all = [];
      let p = 1;
      let totalItems = Infinity;
      const exportPageSize = 200;

      while (all.length < totalItems) {
        const resp = await visitantesApi.getPaged({
          page: p,
          pageSize: exportPageSize,
          sort: sortConfig.field,
          direction: sortConfig.direction,
          nome: filters.nome || undefined,
          email: filters.email || undefined,
          telefone: filters.telefone || undefined,
          whatsApp: filters.whatsApp || undefined,
          dataVisitaFrom: filters.dataVisita_from || undefined,
          dataVisitaTo: filters.dataVisita_to || undefined,
        });

        const data = resp.data || {};
        const items = data.items || [];
        totalItems = Number(data.total || 0);
        all.push(...items);
        if (items.length === 0) break;
        p += 1;
        if (p > 200) break;
      }

      const exportData = all.map(v => ({
        [t('visitors.export.name')]: v.nome || '',
        [t('visitors.export.email')]: v.email || '',
        [t('visitors.export.phone')]: v.telefone || '',
        [t('visitors.export.whatsapp')]: v.whatsApp || '',
        [t('visitors.export.visitDate')]: v.dataVisita ? formatDate(v.dataVisita) : '',
        [t('visitors.export.notes')]: v.observacoes || '',
      }));

      exportToCSV(exportData, 'visitantes');
      toast.success(t('visitors.export.success'));
    } catch (err) {
      console.error('Erro ao exportar visitantes:', err);
      toast.error(t('visitors.export.error'));
    }
  };

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [filters]);

  if (loading) {
    return <LoadingPage text={t('visitors.loading')} />;
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadVisitantes} />;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold sm:text-3xl">{t('visitors.title')}</h1>
          <p className="text-muted-foreground">
            {t('visitors.subtitle')}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <PageRefreshButton onClick={() => loadVisitantes({ silent: true })} refreshing={refreshing} />
          {isAdmin && (
            <Button asChild>
              <Link to="/visitantes/novo">
                <Plus className="h-4 w-4 mr-2" />
                {t('visitors.new')}
              </Link>
            </Button>
          )}
        </div>
      </div>

      <AdvancedSearch
        searchFields={[
          { key: 'nome', label: t('visitors.search.name'), type: 'text', placeholder: t('visitors.search.namePlaceholder') },
          { key: 'email', label: t('visitors.search.email'), type: 'text', placeholder: t('visitors.search.emailPlaceholder') },
          { key: 'telefone', label: t('visitors.search.phone'), type: 'text', placeholder: t('visitors.search.phonePlaceholder') },
          { key: 'whatsApp', label: t('visitors.search.whatsapp'), type: 'text', placeholder: t('visitors.search.whatsappPlaceholder') },
        ]}
        filterFields={[
          {
            key: 'dataVisita',
            label: t('visitors.search.visitDate'),
            type: 'date-range',
          },
        ]}
        values={filters}
        onChange={setFilters}
        onReset={() => {
          setFilters({
            nome: '',
            email: '',
            telefone: '',
            whatsApp: '',
            dataVisita_from: '',
            dataVisita_to: '',
          });
        }}
      />

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle>{t('visitors.listTitle')} ({total})</CardTitle>
            {total > 0 && (
              <Button variant="outline" size="sm" onClick={handleExport} className="w-full sm:w-auto">
                <Download className="h-4 w-4 mr-2" />
                {t('visitors.export.button')}
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {selectedIds.size > 0 && (
            <div className="mb-4 flex flex-col gap-3 rounded-md border bg-muted/50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:py-2">
              <span className="text-sm font-medium">
                {t('visitors.selectedCount', { count: selectedIds.size })}
              </span>
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" size="sm" onClick={() => setSelectedIds(new Set())}>
                  {t('visitors.clearSelection')}
                </Button>
                <Button variant="destructive" size="sm" onClick={handleBulkDeleteClick}>
                  <Trash2 className="h-4 w-4 mr-2" />
                  {t('visitors.deleteSelected')}
                </Button>
              </div>
            </div>
          )}
          {visitantes.length === 0 ? (
            <PageEmptyState
              title={t('visitors.emptyTitle')}
              description={total === 0 ? t('visitors.emptyMessage') : t('visitors.emptyPageMessage')}
              action={total === 0 && isAdmin ? (
                <Button asChild>
                  <Link to="/visitantes/novo">
                    <Plus className="h-4 w-4 mr-2" />
                    {t('visitors.emptyCta')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <>
              <div className="space-y-3 md:hidden">
                <div className="flex items-center justify-between rounded-md border bg-muted/30 px-3 py-2">
                  <span className="text-sm font-medium">{t('visitors.selectAll')}</span>
                  <Checkbox
                    checked={allPageSelected}
                    onCheckedChange={toggleSelectAll}
                    aria-label={t('visitors.selectAll')}
                  />
                </div>

                {visitantes.map((visitante) => {
                  const contato = visitante.email || visitante.whatsApp || visitante.telefone || '-';
                  const perfisAtivos = visitante.perfis || [];

                  return (
                    <div key={visitante.id} className="rounded-lg border bg-background p-3 shadow-xs">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0 space-y-1">
                          <div className="font-medium leading-snug">{visitante.nome || '-'}</div>
                          <div className="text-xs text-muted-foreground">
                            {visitante.dataVisita ? formatDate(visitante.dataVisita) : '-'}
                          </div>
                        </div>
                        <Checkbox
                          checked={selectedIds.has(visitante.id)}
                          onCheckedChange={() => toggleSelect(visitante.id)}
                          aria-label={t('visitors.selectOne', { name: visitante.nome || t('visitors.visitFallback') })}
                        />
                      </div>

                      <div className="mt-3 space-y-2 text-sm">
                        <div className="flex min-w-0 items-center justify-between gap-2">
                          <span className="min-w-0 truncate text-muted-foreground">{contato}</span>
                          <div className="flex shrink-0 items-center gap-1">
                            {visitante.email && (
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8"
                                onClick={() => window.open(`mailto:${visitante.email}`)}
                              >
                                <Mail className="h-4 w-4" />
                              </Button>
                            )}
                            {visitante.whatsApp && (
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8"
                                onClick={() => window.open(`https://wa.me/55${visitante.whatsApp.replace(/\D/g, '')}`)}
                              >
                                <Phone className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </div>
                        {visitante.observacoes && (
                          <p className="line-clamp-2 text-muted-foreground">
                            {visitante.observacoes}
                          </p>
                        )}
                      </div>

                      <div className="mt-3 flex flex-wrap gap-1">
                        {perfisAtivos.length > 0 ? (
                          perfisAtivos.map((perfil, idx) => (
                            <Badge key={idx} variant="secondary" className="text-xs">
                              {perfil}
                            </Badge>
                          ))
                        ) : (
                          <span className="text-sm text-muted-foreground">-</span>
                        )}
                      </div>

                      <div className="mt-3 flex items-center justify-end gap-1 border-t pt-2">
                        <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                          <Link to={`/visitantes/${visitante.id}`}>
                            <Eye className="h-4 w-4" />
                          </Link>
                        </Button>
                        {isAdmin && (
                          <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                            <Link to={`/visitantes/${visitante.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </Button>
                        )}
                        {isAdmin && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => handleDelete(visitante.id)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>

              <div className="hidden md:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-12">
                        <Checkbox
                          checked={allPageSelected}
                          onCheckedChange={toggleSelectAll}
                          aria-label={t('visitors.selectAll')}
                        />
                      </TableHead>
                      <SortableTableHeader field="dataVisita" onSort={handleSort} sortConfig={sortConfig}>
                        {t('visitors.table.visitDate')}
                      </SortableTableHeader>
                      <SortableTableHeader field="nome" onSort={handleSort} sortConfig={sortConfig}>
                        {t('visitors.table.person')}
                      </SortableTableHeader>
                      <TableHead>{t('visitors.table.contact')}</TableHead>
                      <TableHead>{t('visitors.table.notes')}</TableHead>
                      <TableHead>{t('visitors.table.profiles')}</TableHead>
                      <TableHead className="text-right">{t('visitors.table.actions')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {visitantes.map((visitante) => {
                      const contato = visitante.email || visitante.whatsApp || visitante.telefone || '-';
                      const perfisAtivos = visitante.perfis || [];

                      return (
                        <TableRow key={visitante.id}>
                          <TableCell>
                            <Checkbox
                              checked={selectedIds.has(visitante.id)}
                              onCheckedChange={() => toggleSelect(visitante.id)}
                              aria-label={t('visitors.selectOne', { name: visitante.nome || t('visitors.visitFallback') })}
                            />
                          </TableCell>
                          <TableCell>
                            {visitante.dataVisita ? formatDate(visitante.dataVisita) : '-'}
                          </TableCell>
                          <TableCell className="font-medium">
                            {visitante.nome || '-'}
                          </TableCell>
                          <TableCell>
                            <div className="flex items-center space-x-2">
                              <span className="text-sm">{contato}</span>
                              {visitante.email && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => window.open(`mailto:${visitante.email}`)}
                                >
                                  <Mail className="h-4 w-4" />
                                </Button>
                              )}
                              {visitante.whatsApp && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => window.open(`https://wa.me/55${visitante.whatsApp.replace(/\D/g, '')}`)}
                                >
                                  <Phone className="h-4 w-4" />
                                </Button>
                              )}
                            </div>
                          </TableCell>
                          <TableCell>
                            <span className="text-sm text-muted-foreground">
                              {visitante.observacoes
                                ? (visitante.observacoes.length > 50
                                    ? visitante.observacoes.substring(0, 50) + '...'
                                    : visitante.observacoes)
                                : '-'}
                            </span>
                          </TableCell>
                          <TableCell>
                            <div className="flex flex-wrap gap-1">
                              {perfisAtivos.length > 0 ? (
                                perfisAtivos.map((perfil, idx) => (
                                  <Badge key={idx} variant="secondary" className="text-xs">
                                    {perfil}
                                  </Badge>
                                ))
                              ) : (
                                <span className="text-muted-foreground text-sm">-</span>
                              )}
                            </div>
                          </TableCell>
                          <TableCell className="text-right">
                            <div className="flex items-center justify-end space-x-2">
                              <Button variant="ghost" size="sm" asChild>
                                <Link to={`/visitantes/${visitante.id}`}>
                                  <Eye className="h-4 w-4" />
                                </Link>
                              </Button>
                              {isAdmin && (
                                <Button variant="ghost" size="sm" asChild>
                                  <Link to={`/visitantes/${visitante.id}/editar`}>
                                    <Edit className="h-4 w-4" />
                                  </Link>
                                </Button>
                              )}
                              {isAdmin && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleDelete(visitante.id)}
                                >
                                  <Trash2 className="h-4 w-4" />
                                </Button>
                              )}
                            </div>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </div>
            </>
          )}
          {total > 0 && (
            <DataTablePagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              onPageSizeChange={(newSize) => {
                setPageSize(newSize);
                setPage(1);
              }}
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

      <ConfirmDialog
        open={bulkDeleteDialogOpen}
        onOpenChange={setBulkDeleteDialogOpen}
        onConfirm={handleBulkDeleteConfirm}
        title={t('visitors.bulkDeleteTitle')}
        description={t('visitors.bulkDeleteDescription', { count: selectedIds.size })}
        confirmText={t('visitors.deleteConfirm')}
        cancelText={t('actions.cancel')}
        variant="destructive"
        loading={bulkDeleting}
      />
    </div>
  );
}
