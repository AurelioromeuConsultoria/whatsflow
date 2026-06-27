import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Phone, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { voluntariosApi, equipesApi, cargosApi } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

export default function VoluntariosList() {
  const [items, setItems] = useState([]);
  const [equipes, setEquipes] = useState([]);
  const [cargos, setCargos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const [equipeId, setEquipeId] = useState('');
  const [cargoId, setCargoId] = useState('');
  const confirmDialog = useConfirmDialog();
  const { can, isAdmin } = useAuth();
  const { t } = useTranslation();

  const load = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const [v, e, c] = await Promise.all([
        voluntariosApi.getAll(),
        equipesApi.getAll(),
        cargosApi.getAll(),
      ]);
      const voluntariosAgrupados = Array.from(
        (v.data || []).reduce((acc, vinculo) => {
          const key = vinculo.pessoaId || vinculo.id;
          const atual = acc.get(key) || {
            ...vinculo,
            id: vinculo.id,
            vinculos: [],
            quantidadeEquipes: 0,
            cargosResumo: '',
          };

          atual.vinculos.push(vinculo);
          atual.quantidadeEquipes = new Set(atual.vinculos.map((item) => item.equipeId).filter(Boolean)).size;
          atual.cargosResumo = Array.from(new Set(atual.vinculos.map((item) => item.nomeCargo).filter(Boolean))).join(', ');
          acc.set(key, atual);
          return acc;
        }, new Map()).values()
      ).sort((a, b) => (a.nome || '').localeCompare(b.nome || ''));

      setItems(voluntariosAgrupados);
      setEquipes(e.data || []);
      setCargos(c.data || []);
    } catch (err) {
      setError(t('volunteer.volunteers.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (voluntario) => {
    confirmDialog.show({
      title: t('volunteer.volunteers.deleteTitle'),
      description: t('volunteer.volunteers.deleteDescription', { name: voluntario?.nome || t('volunteer.volunteers.thisVolunteer') }),
      confirmText: t('volunteer.volunteers.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await Promise.all((voluntario.vinculos || [voluntario]).map((vinculo) => voluntariosApi.delete(vinculo.id)));
          toast.success(t('volunteer.volunteers.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('volunteer.volunteers.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((v) => {
    if (busca && !v.nome.toLowerCase().includes(busca.toLowerCase())) return false;
    if (equipeId && !(v.vinculos || []).some((item) => String(item.equipeId) === String(equipeId))) return false;
    if (cargoId && !(v.vinculos || []).some((item) => String(item.cargoId) === String(cargoId))) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('volunteer.volunteers.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = isAdmin && can(RESOURCES.VOLUNTARIOS, ACTIONS.EDIT);
  const canDelete = isAdmin && can(RESOURCES.VOLUNTARIOS, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold sm:text-3xl">{t('volunteer.volunteers.title')}</h1>
          <p className="text-muted-foreground">{t('volunteer.volunteers.subtitle')}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/voluntarios/novo">
                <Plus className="h-4 w-4 mr-2" /> {t('volunteer.volunteers.new')}
              </Link>
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.volunteers.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('volunteer.volunteers.searchLabel')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('volunteer.volunteers.searchPlaceholder')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('volunteer.volunteers.teamLabel')}</label>
              <Select value={equipeId || 'all'} onValueChange={(value) => setEquipeId(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('volunteer.volunteers.allTeams')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.volunteers.allTeams')}</SelectItem>
                  {equipes.map((e) => (
                    <SelectItem key={e.id} value={String(e.id)}>{e.nome}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('volunteer.volunteers.roleLabel')}</label>
              <Select value={cargoId || 'all'} onValueChange={(value) => setCargoId(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('volunteer.volunteers.allRoles')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.volunteers.allRoles')}</SelectItem>
                  {cargos.map((c) => (
                    <SelectItem key={c.id} value={String(c.id)}>{c.nome}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.volunteers.listTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('volunteer.volunteers.emptyTitle')}
              description={t('volunteer.volunteers.emptyDescription')}
              action={canEdit ? (
                <Button asChild>
                  <Link to="/voluntarios/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('volunteer.volunteers.new')}
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <>
              <div className="space-y-3 md:hidden">
                {paginatedItems.map((vol) => (
                  <div key={vol.id} className="rounded-lg border bg-background p-3 shadow-xs">
                    <div className="space-y-1">
                      <div className="font-medium leading-snug">{vol.nome}</div>
                      <div className="text-xs text-muted-foreground">
                        {t('volunteer.volunteers.table.teamsCount')}: {vol.quantidadeEquipes || 0}
                      </div>
                    </div>

                    <div className="mt-3 space-y-2 text-sm">
                      <div className="flex min-w-0 items-center justify-between gap-2">
                        <span className="min-w-0 truncate text-muted-foreground">{vol.whatsApp || '-'}</span>
                        {vol.whatsApp && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => window.open(`https://wa.me/55${String(vol.whatsApp).replace(/\D/g, '')}`)}
                          >
                            <Phone className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                      <div className="text-muted-foreground">
                        {vol.cargosResumo || '-'}
                      </div>
                    </div>

                    <div className="mt-3 flex items-center justify-end gap-1 border-t pt-2">
                      {canEdit && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                          <Link to={`/voluntarios/${vol.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                      )}
                      {canDelete && (
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => handleDelete(vol)}>
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
                      <TableHead>{t('volunteer.volunteers.table.name')}</TableHead>
                      <TableHead>{t('volunteer.volunteers.table.whatsApp')}</TableHead>
                      <TableHead className="text-right">{t('volunteer.volunteers.table.teamsCount')}</TableHead>
                      <TableHead>{t('volunteer.volunteers.table.roles')}</TableHead>
                      <TableHead className="text-right">{t('volunteer.volunteers.table.actions')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {paginatedItems.map((vol) => (
                      <TableRow key={vol.id}>
                        <TableCell className="font-medium">{vol.nome}</TableCell>
                        <TableCell>
                          <div className="flex items-center space-x-2">
                            <span>{vol.whatsApp}</span>
                            {vol.whatsApp && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => window.open(`https://wa.me/55${String(vol.whatsApp).replace(/\D/g, '')}`)}
                              >
                                <Phone className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </TableCell>
                        <TableCell className="text-right">{vol.quantidadeEquipes}</TableCell>
                        <TableCell>{vol.cargosResumo || '-'}</TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end space-x-2">
                            {canEdit && (
                              <Button variant="ghost" size="sm" asChild>
                                <Link to={`/voluntarios/${vol.id}/editar`}>
                                  <Edit className="h-4 w-4" />
                                </Link>
                              </Button>
                            )}
                            {canDelete && (
                              <Button variant="ghost" size="sm" onClick={() => handleDelete(vol)}>
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
