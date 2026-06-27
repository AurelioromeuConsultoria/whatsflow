import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { TableRowActions, RowIconButtonAction, RowIconLinkAction } from '@/components/ui/list-actions';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { equipesApi, voluntariosApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function EquipesList() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const [area, setArea] = useState('');
  const confirmDialog = useConfirmDialog();
  const { can, isAdmin } = useAuth();
  const { t } = useTranslation();
  const areaLabel = {
    1: t('volunteer.teams.areas.green'),
    2: t('volunteer.teams.areas.red'),
    3: t('volunteer.teams.areas.orange'),
  };

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
      const [equipesRes, voluntariosRes] = await Promise.all([
        equipesApi.getAll(),
        voluntariosApi.getAll(),
      ]);
      const membrosPorEquipe = (voluntariosRes.data || []).reduce((acc, voluntario) => {
        const equipeId = voluntario.equipeId;
        if (!equipeId) return acc;

        const membros = acc.get(equipeId) || new Set();
        membros.add(voluntario.pessoaId || voluntario.id);
        acc.set(equipeId, membros);
        return acc;
      }, new Map());

      setItems((equipesRes.data || []).map((equipe) => ({
        ...equipe,
        quantidadeMembros: membrosPorEquipe.get(equipe.id)?.size ?? 0,
      })));
    } catch (err) {
      setError(t('volunteer.teams.errorLoad'));
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
    const equipe = items.find(e => e.id === id);
    confirmDialog.show({
      title: t('volunteer.teams.deleteTitle'),
      description: t('volunteer.teams.deleteDescription', { name: equipe?.nome || t('volunteer.teams.thisTeam') }),
      confirmText: t('volunteer.teams.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await equipesApi.delete(id);
          toast.success(t('volunteer.teams.deleteSuccess'));
          await load();
        } catch (err) {
          const errorMsg = err.response?.data?.message || t('volunteer.teams.deleteError');
          toast.error(errorMsg);
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((e) => {
    if (busca && !e.nome.toLowerCase().includes(busca.toLowerCase())) return false;
    if (area && String(e.area) !== String(area)) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('volunteer.teams.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = isAdmin && can(RESOURCES.EQUIPES, ACTIONS.EDIT);
  const canDelete = isAdmin && can(RESOURCES.EQUIPES, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.teams.title')}</h1>
          <p className="text-muted-foreground">{t('volunteer.teams.subtitle')}</p>
        </div>
        {canEdit && (
          <Button asChild>
            <Link to="/equipes/novo">
              <Plus className="h-4 w-4 mr-2" /> {t('volunteer.teams.new')}
            </Link>
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.teams.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('volunteer.teams.searchLabel')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('volunteer.teams.searchPlaceholder')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('volunteer.teams.areaLabel')}</label>
              <Select value={area || 'all'} onValueChange={(value) => setArea(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('volunteer.teams.allAreas')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.teams.allAreas')}</SelectItem>
                  <SelectItem value="1">{t('volunteer.teams.areas.green')}</SelectItem>
                  <SelectItem value="2">{t('volunteer.teams.areas.red')}</SelectItem>
                  <SelectItem value="3">{t('volunteer.teams.areas.orange')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('volunteer.teams.listTitle')} ({total})</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('volunteer.teams.emptyTitle')}
              description={t('volunteer.teams.emptyDescription')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/equipes/novo">
                    <Plus className="h-4 w-4 mr-2" /> {t('volunteer.teams.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('volunteer.teams.table.name')}</TableHead>
                  <TableHead>{t('volunteer.teams.table.area')}</TableHead>
                  <TableHead className="text-right">{t('volunteer.teams.table.members')}</TableHead>
                  <TableHead>{t('volunteer.teams.table.createdAt')}</TableHead>
                  <TableHead className="text-right">{t('volunteer.teams.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((equipe) => (
                  <TableRow key={equipe.id}>
                    <TableCell className="font-medium">{equipe.nome}</TableCell>
                    <TableCell>{areaLabel[equipe.area] || equipe.area}</TableCell>
                    <TableCell className="text-right">{equipe.quantidadeMembros ?? 0}</TableCell>
                    <TableCell>{formatDate(equipe.dataCriacao)}</TableCell>
                    <TableCell className="text-right">
                      <TableRowActions>
                        {canEdit && (
                          <RowIconLinkAction>
                            <Link to={`/equipes/${equipe.id}/editar`}>
                              <Edit className="h-4 w-4" />
                            </Link>
                          </RowIconLinkAction>
                        )}
                        {canDelete && (
                          <RowIconButtonAction onClick={() => handleDelete(equipe.id)}>
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
