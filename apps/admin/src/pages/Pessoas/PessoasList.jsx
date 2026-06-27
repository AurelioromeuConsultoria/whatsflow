import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Eye, Edit, Trash2, Phone, Mail, Download, UserPlus } from 'lucide-react';
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
import { exportToCSV } from '@/utils/export';
import { pessoasApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

const PERFIS_OPTIONS = [
  { value: 'Visitante', labelKey: 'visitor' },
  { value: 'Membro', labelKey: 'member' },
  { value: 'Voluntario', labelKey: 'volunteer' },
  { value: 'Lider', labelKey: 'leader' },
  { value: 'Kids', label: 'Kids' },
  { value: 'Admin', labelKey: 'admin' },
];

export default function PessoasList() {
  const [pessoas, setPessoas] = useState([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [filters, setFilters] = useState({
    nome: '',
    email: '',
    telefone: '',
    whatsApp: '',
    perfil: undefined,
    tipoPessoa: undefined,
    ativo: undefined,
  });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [sortConfig, setSortConfig] = useState({ field: 'nome', direction: 'asc' });
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [pessoaToDelete, setPessoaToDelete] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [bulkDeleteDialogOpen, setBulkDeleteDialogOpen] = useState(false);
  const [bulkDeleting, setBulkDeleting] = useState(false);
  const { can, isAdmin } = useAuth();
  const { t } = useTranslation();

  const loadPessoas = useCallback(async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const ativoParam =
        filters.ativo === undefined ? undefined : (filters.ativo === true || filters.ativo === 'true');
      const response = await pessoasApi.getPaged({
        page,
        pageSize,
        sort: sortConfig.field,
        direction: sortConfig.direction,
        nome: filters.nome || undefined,
        email: filters.email || undefined,
        telefone: filters.telefone || undefined,
        whatsApp: filters.whatsApp || undefined,
        perfil: filters.perfil || undefined,
        tipoPessoa: filters.tipoPessoa || undefined,
        ativo: ativoParam,
      });

      const data = response.data || {};
      setPessoas(data.items || []);
      setTotal(Number(data.total || 0));
    } catch (err) {
      const errorMessage = err.response?.data?.message || err.message || t('people.errorLoad');
      setError(errorMessage);
      console.error('Erro ao carregar pessoas:', err);
      toast.error(t('people.errorLoadToast', { message: errorMessage }));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [filters, page, pageSize, sortConfig.direction, sortConfig.field]);

  const handleDeleteClick = (pessoa) => {
    setPessoaToDelete(pessoa);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!pessoaToDelete) return;
    const currentPageCount = pessoas.length;

    try {
      setDeleting(true);
      await pessoasApi.delete(pessoaToDelete.id);
      toast.success(t('people.deleteSuccess'));
      setDeleteDialogOpen(false);
      setPessoaToDelete(null);
      // Recarrega a página atual; se ficar vazia, volta uma página.
      await loadPessoas();
      if (page > 1 && currentPageCount === 1) {
        setPage((p) => Math.max(1, p - 1));
      }
    } catch (err) {
      toast.error(t('people.deleteError'));
      console.error('Erro ao excluir pessoa:', err);
    } finally {
      setDeleting(false);
    }
  };

  useEffect(() => {
    loadPessoas();
  }, [loadPessoas]);

  useEffect(() => {
    setSelectedIds(new Set());
  }, [page, filters]);

  const pageIds = pessoas.map((p) => p.id);
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
          await pessoasApi.delete(id);
          ok += 1;
        } catch {
          fail += 1;
        }
      }
      setSelectedIds(new Set());
      setBulkDeleteDialogOpen(false);
      await loadPessoas();
      if (page > 1 && pessoas.length === ids.length) setPage((p) => Math.max(1, p - 1));
      if (fail > 0) {
        toast.warning(t('people.bulkDeletePartial', { ok, fail }));
      } else {
        toast.success(t('people.bulkDeleteSuccess', { count: ok }));
      }
    } catch (err) {
      toast.error(t('people.bulkDeleteError'));
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
      // Exporta TODOS os itens do filtro atual buscando páginas sequencialmente.
      const all = [];
      let p = 1;
      let totalItems = Infinity;
      const exportPageSize = 200;
      const ativoParam =
        filters.ativo === undefined ? undefined : (filters.ativo === true || filters.ativo === 'true');

      while (all.length < totalItems) {
        const resp = await pessoasApi.getPaged({
          page: p,
          pageSize: exportPageSize,
          sort: sortConfig.field,
          direction: sortConfig.direction,
          nome: filters.nome || undefined,
          email: filters.email || undefined,
          telefone: filters.telefone || undefined,
          whatsApp: filters.whatsApp || undefined,
          perfil: filters.perfil || undefined,
          tipoPessoa: filters.tipoPessoa || undefined,
          ativo: ativoParam,
        });

        const data = resp.data || {};
        const items = data.items || [];
        totalItems = Number(data.total || 0);
        all.push(...items);
        if (items.length === 0) break;
        p += 1;
        if (p > 200) break; // trava de segurança
      }

      const exportData = all.map(pessoa => ({
      [t('people.export.name')]: pessoa.nome || '',
      [t('people.export.email')]: pessoa.email || '',
      [t('people.export.phone')]: pessoa.telefone || '',
      [t('people.export.whatsapp')]: pessoa.whatsApp || '',
      [t('people.export.personType')]: pessoa.tipoPessoa || '',
      [t('people.export.profiles')]: pessoa.perfis?.filter(p => !p.dataFim).map(p => p.perfil).join('; ') || '',
      [t('people.export.active')]: pessoa.ativo ? t('people.boolean.yes') : t('people.boolean.no'),
      [t('people.export.createdAt')]: pessoa.dataCriacao ? formatDate(pessoa.dataCriacao) : '',
    }));

    exportToCSV(exportData, 'pessoas', [
      { key: t('people.export.name'), label: t('people.export.name') },
      { key: t('people.export.email'), label: t('people.export.email') },
      { key: t('people.export.phone'), label: t('people.export.phone') },
      { key: t('people.export.whatsapp'), label: t('people.export.whatsapp') },
      { key: t('people.export.personType'), label: t('people.export.personType') },
      { key: t('people.export.profiles'), label: t('people.export.profiles') },
      { key: t('people.export.active'), label: t('people.export.active') },
      { key: t('people.export.createdAt'), label: t('people.export.createdAt') },
    ]);

    toast.success(t('people.export.success'));
    } catch (err) {
      console.error('Erro ao exportar pessoas:', err);
      toast.error(t('people.export.error'));
    }
  };

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [filters]);

  if (loading) {
    return <LoadingPage text={t('people.loading')} />;
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadPessoas} />;
  }

  const perfisOptions = PERFIS_OPTIONS.map((option) => ({
    value: option.value,
    label: option.label ?? t(`people.filters.profileOptions.${option.labelKey}`),
  }));

  const canEdit = isAdmin && can(RESOURCES.PESSOAS, ACTIONS.EDIT);
  const canDelete = isAdmin && can(RESOURCES.PESSOAS, ACTIONS.DELETE);
  const canCreateUsuario = isAdmin && can(RESOURCES.USUARIOS, ACTIONS.EDIT);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold sm:text-3xl">{t('people.title')}</h1>
          <p className="text-muted-foreground">
            {t('people.subtitle')}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <PageRefreshButton onClick={() => loadPessoas({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/pessoas/novo">
                <Plus className="h-4 w-4 mr-2" />
                {t('people.new')}
              </Link>
            </Button>
          )}
        </div>
      </div>

      <AdvancedSearch
        searchFields={[
          { key: 'nome', label: t('people.fields.name'), type: 'text', placeholder: t('people.search.namePlaceholder') },
          { key: 'email', label: t('people.fields.email'), type: 'text', placeholder: t('people.search.emailPlaceholder') },
          { key: 'telefone', label: t('people.fields.phone'), type: 'text', placeholder: t('people.search.phonePlaceholder') },
          { key: 'whatsApp', label: t('people.fields.whatsapp'), type: 'text', placeholder: t('people.search.whatsappPlaceholder') },
        ]}
        filterFields={[
          {
            key: 'perfil',
            label: t('people.filters.profile'),
            type: 'select',
            options: perfisOptions,
          },
          {
            key: 'tipoPessoa',
            label: t('people.filters.personType'),
            type: 'select',
            options: [
              { value: 'Adulto', label: t('people.form.personType.adult') },
              { value: 'Crianca', label: t('people.form.personType.child') },
            ],
          },
          {
            key: 'ativo',
            label: t('people.filters.status'),
            type: 'boolean',
            trueLabel: t('people.status.active'),
            falseLabel: t('people.status.inactive'),
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
            perfil: undefined,
            tipoPessoa: undefined,
            ativo: undefined,
          });
        }}
      />

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle>{t('people.listTitle')} ({total})</CardTitle>
            {total > 0 && (
              <Button variant="outline" size="sm" onClick={handleExport} className="w-full sm:w-auto">
                <Download className="h-4 w-4 mr-2" />
                {t('people.export.button')}
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {selectedIds.size > 0 && canDelete && (
            <div className="mb-4 flex flex-col gap-3 rounded-md border bg-muted/50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:py-2">
              <span className="text-sm font-medium">
                {t('people.selectedCount', { count: selectedIds.size })}
              </span>
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" size="sm" onClick={() => setSelectedIds(new Set())}>
                  {t('people.clearSelection')}
                </Button>
                <Button variant="destructive" size="sm" onClick={handleBulkDeleteClick}>
                  <Trash2 className="h-4 w-4 mr-2" />
                  {t('people.deleteSelected')}
                </Button>
              </div>
            </div>
          )}
          {pessoas.length === 0 ? (
            <PageEmptyState
              title={t('people.emptyTitle')}
              description={total === 0 ? t('people.emptyMessage') : t('people.emptyPageMessage')}
              action={total === 0 && canEdit ? (
                <Button asChild>
                  <Link to="/pessoas/novo">
                    <Plus className="h-4 w-4 mr-2" />
                    {t('people.emptyCta')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <>
              <div className="space-y-3 md:hidden">
                {canDelete && (
                  <div className="flex items-center justify-between rounded-md border bg-muted/30 px-3 py-2">
                    <span className="text-sm font-medium">{t('people.selectAll')}</span>
                    <Checkbox
                      checked={allPageSelected}
                      onCheckedChange={toggleSelectAll}
                      aria-label={t('people.selectAll')}
                    />
                  </div>
                )}

                {pessoas.map((pessoa) => (
                  <div key={pessoa.id} className="rounded-lg border bg-background p-3 shadow-xs">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0 space-y-1">
                        <div className="font-medium leading-snug">{pessoa.nome}</div>
                        <div className="flex flex-wrap gap-1.5">
                          <Badge variant="outline">{pessoa.tipoPessoa || '-'}</Badge>
                          <Badge variant={pessoa.ativo ? 'default' : 'secondary'}>
                            {pessoa.ativo ? t('people.boolean.yes') : t('people.boolean.no')}
                          </Badge>
                        </div>
                      </div>
                      {canDelete && (
                        <Checkbox
                          checked={selectedIds.has(pessoa.id)}
                          onCheckedChange={() => toggleSelect(pessoa.id)}
                          aria-label={t('people.selectOne', { name: pessoa.nome })}
                        />
                      )}
                    </div>

                    <div className="mt-3 space-y-2 text-sm">
                      <div className="flex min-w-0 items-center justify-between gap-2">
                        <span className="min-w-0 truncate text-muted-foreground">{pessoa.email || '-'}</span>
                        {pessoa.email && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => window.open(`mailto:${pessoa.email}`)}
                          >
                            <Mail className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                      <div className="flex min-w-0 items-center justify-between gap-2">
                        <span className="text-muted-foreground">{pessoa.whatsApp || pessoa.telefone || '-'}</span>
                        {pessoa.whatsApp && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => window.open(`https://wa.me/55${pessoa.whatsApp.replace(/\D/g, '')}`)}
                          >
                            <Phone className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </div>

                    <div className="mt-3 flex flex-wrap gap-1">
                      {pessoa.perfis && pessoa.perfis.length > 0 ? (
                        pessoa.perfis
                          .filter(p => !p.dataFim)
                          .map((perfil, idx) => (
                            <Badge key={idx} variant="secondary">
                              {perfil.perfil}
                            </Badge>
                          ))
                      ) : (
                        <span className="text-sm text-muted-foreground">-</span>
                      )}
                    </div>

                    <div className="mt-3 flex items-center justify-end gap-1 border-t pt-2">
                      <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                        <Link to={`/pessoas/${pessoa.id}`}>
                          <Eye className="h-4 w-4" />
                        </Link>
                      </Button>
                      {canEdit && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                          <Link to={`/pessoas/${pessoa.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                      )}
                      {canCreateUsuario && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" asChild title={t('people.actions.createAccess')}>
                          <Link to={`/usuarios?pessoaId=${pessoa.id}`}>
                            <UserPlus className="h-4 w-4" />
                          </Link>
                        </Button>
                      )}
                      {canDelete && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => handleDeleteClick(pessoa)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>

              <div className="hidden md:block">
                <Table>
                  <TableHeader>
                    <TableRow>
                      {canDelete && (
                        <TableHead className="w-12">
                          <Checkbox
                            checked={allPageSelected}
                            onCheckedChange={toggleSelectAll}
                            aria-label={t('people.selectAll')}
                          />
                        </TableHead>
                      )}
                      <SortableTableHeader field="nome" onSort={handleSort} sortConfig={sortConfig}>
                        {t('people.fields.name')}
                      </SortableTableHeader>
                      <SortableTableHeader field="email" onSort={handleSort} sortConfig={sortConfig}>
                        {t('people.fields.email')}
                      </SortableTableHeader>
                      <SortableTableHeader field="telefone" onSort={handleSort} sortConfig={sortConfig}>
                        {t('people.fields.phone')}
                      </SortableTableHeader>
                      <SortableTableHeader field="whatsApp" onSort={handleSort} sortConfig={sortConfig}>
                        {t('people.fields.whatsapp')}
                      </SortableTableHeader>
                      <SortableTableHeader field="tipoPessoa" onSort={handleSort} sortConfig={sortConfig}>
                        {t('people.table.type')}
                      </SortableTableHeader>
                      <TableHead>{t('people.table.profiles')}</TableHead>
                      <SortableTableHeader field="ativo" onSort={handleSort} sortConfig={sortConfig}>
                        {t('people.table.active')}
                      </SortableTableHeader>
                      <TableHead className="text-right">{t('people.table.actions')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {pessoas.map((pessoa) => (
                      <TableRow key={pessoa.id}>
                        {canDelete && (
                          <TableCell>
                            <Checkbox
                              checked={selectedIds.has(pessoa.id)}
                              onCheckedChange={() => toggleSelect(pessoa.id)}
                              aria-label={t('people.selectOne', { name: pessoa.nome })}
                            />
                          </TableCell>
                        )}
                        <TableCell className="font-medium">
                          {pessoa.nome}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center space-x-2">
                            <span>{pessoa.email || '-'}</span>
                            {pessoa.email && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => window.open(`mailto:${pessoa.email}`)}
                              >
                                <Mail className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          {pessoa.telefone || '-'}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center space-x-2">
                            <span>{pessoa.whatsApp || '-'}</span>
                            {pessoa.whatsApp && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => window.open(`https://wa.me/55${pessoa.whatsApp.replace(/\D/g, '')}`)}
                              >
                                <Phone className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline">
                            {pessoa.tipoPessoa || '-'}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-1">
                            {pessoa.perfis && pessoa.perfis.length > 0 ? (
                              pessoa.perfis
                                .filter(p => !p.dataFim) // Apenas perfis ativos
                                .map((perfil, idx) => (
                                  <Badge key={idx} variant="secondary">
                                    {perfil.perfil}
                                  </Badge>
                                ))
                            ) : (
                              <span className="text-muted-foreground text-sm">-</span>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant={pessoa.ativo ? 'default' : 'secondary'}>
                            {pessoa.ativo ? t('people.boolean.yes') : t('people.boolean.no')}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end space-x-2">
                            <Button variant="ghost" size="sm" asChild>
                              <Link to={`/pessoas/${pessoa.id}`}>
                                <Eye className="h-4 w-4" />
                              </Link>
                            </Button>
                            {canEdit && (
                              <Button variant="ghost" size="sm" asChild>
                                <Link to={`/pessoas/${pessoa.id}/editar`}>
                                  <Edit className="h-4 w-4" />
                                </Link>
                              </Button>
                            )}
                            {canCreateUsuario && (
                              <Button variant="ghost" size="sm" asChild title={t('people.actions.createAccess')}>
                                <Link to={`/usuarios?pessoaId=${pessoa.id}`}>
                                  <UserPlus className="h-4 w-4" />
                                </Link>
                              </Button>
                            )}
                            {canDelete && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleDeleteClick(pessoa)}
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
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
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        onConfirm={handleDeleteConfirm}
        title={t('people.deleteTitle')}
        description={t('people.deleteDescription', { name: pessoaToDelete?.nome || t('people.deleteFallbackName') })}
        confirmText={t('people.deleteConfirm')}
        cancelText={t('actions.cancel')}
        variant="destructive"
        loading={deleting}
      />

      <ConfirmDialog
        open={bulkDeleteDialogOpen}
        onOpenChange={setBulkDeleteDialogOpen}
        onConfirm={handleBulkDeleteConfirm}
        title={t('people.bulkDeleteTitle')}
        description={t('people.bulkDeleteDescription', { count: selectedIds.size })}
        confirmText={t('people.deleteConfirm')}
        cancelText={t('actions.cancel')}
        variant="destructive"
        loading={bulkDeleting}
      />
    </div>
  );
}
