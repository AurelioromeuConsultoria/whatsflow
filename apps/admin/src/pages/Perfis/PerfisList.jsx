import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Filter, X, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { pessoasPerfisApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

const perfilLabels = {
  1: 'Visitante',
  2: 'Membro',
  3: 'Voluntário',
  4: 'Líder',
  5: 'Kids',
  6: 'Admin',
  Visitante: 'Visitante',
  Membro: 'Membro',
  Voluntario: 'Voluntário',
  Lider: 'Líder',
  Kids: 'Kids',
  Admin: 'Admin',
};

function getPerfilLabel(value, t) {
  if (value == null || value === '') return t('peopleProfiles.notInformed');
  return perfilLabels[value] || String(value);
}

export default function PerfisList() {
  const { t } = useTranslation();
  const [perfis, setPerfis] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [filtroPerfil, setFiltroPerfil] = useState('');
  const [filtroStatus, setFiltroStatus] = useState('');
  const confirmDialog = useConfirmDialog();

  const loadPerfis = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const response = await pessoasPerfisApi.getAll();
      setPerfis(response.data || []);
    } catch (err) {
      setError(t('peopleProfiles.errorLoad'));
      console.error('Erro ao carregar perfis:', err);
      toast.error(t('peopleProfiles.errorLoad'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleEncerrarPerfil = async (id) => {
    const perfil = perfis.find(p => p.id === id);
    confirmDialog.show({
      title: t('peopleProfiles.endTitle'),
      description: t('peopleProfiles.endDescription', {
        profile: perfil?.perfil || t('peopleProfiles.fallbackProfile'),
        person: perfil?.pessoa?.nome || t('peopleProfiles.fallbackPerson'),
      }),
      confirmText: t('peopleProfiles.endConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'default',
      onConfirm: async () => {
        try {
          await pessoasPerfisApi.update(id, {
            dataFim: new Date().toISOString(),
          });
          toast.success(t('peopleProfiles.endSuccess'));
          await loadPerfis();
        } catch (err) {
          toast.error(t('peopleProfiles.endError'));
          console.error('Erro ao encerrar perfil:', err);
          throw err;
        }
      },
    });
  };

  const handleDelete = async (id) => {
    const perfil = perfis.find(p => p.id === id);
    confirmDialog.show({
      title: t('peopleProfiles.deleteTitle'),
      description: t('peopleProfiles.deleteDescription', {
        profile: perfil?.perfil || t('peopleProfiles.fallbackProfile'),
        person: perfil?.pessoa?.nome || t('peopleProfiles.fallbackPerson'),
      }),
      confirmText: t('peopleProfiles.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await pessoasPerfisApi.delete(id);
          toast.success(t('peopleProfiles.deleteSuccess'));
          await loadPerfis();
        } catch (err) {
          toast.error(t('peopleProfiles.deleteError'));
          console.error('Erro ao excluir perfil:', err);
          throw err;
        }
      },
    });
  };

  useEffect(() => {
    loadPerfis();
  }, []);

  // Obter lista única de perfis para filtro
  const perfisUnicos = [...new Set(
    perfis
      .map(p => getPerfilLabel(p.perfil, t))
      .filter(perfil => typeof perfil === 'string')
      .map(perfil => perfil.trim())
      .filter(perfil => perfil !== '')
  )];

  // Filtrar perfis
  const perfisFiltrados = perfis.filter((perfil) => {
    const matchPerfil = !filtroPerfil || getPerfilLabel(perfil.perfil, t) === filtroPerfil;
    
    const isAtivo = !perfil.dataFim;
    const matchStatus = !filtroStatus || 
      (filtroStatus === 'ativo' && isAtivo) ||
      (filtroStatus === 'inativo' && !isAtivo);

    return matchPerfil && matchStatus;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(perfisFiltrados, 20);

  if (loading) {
    return <LoadingPage text={t('peopleProfiles.loading')} />;
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadPerfis} />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('peopleProfiles.title')}</h1>
          <p className="text-muted-foreground">
            {t('peopleProfiles.subtitle')}
          </p>
        </div>
        <PageRefreshButton onClick={() => loadPerfis({ silent: true })} refreshing={refreshing} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Filter className="h-5 w-5" />
            {t('peopleProfiles.filters.title')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('peopleProfiles.filters.profile')}</label>
              <Select value={filtroPerfil || "all"} onValueChange={(value) => setFiltroPerfil(value === "all" ? "" : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('peopleProfiles.filters.allProfiles')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('peopleProfiles.filters.allProfiles')}</SelectItem>
                  {perfisUnicos.map((perfil) => (
                    <SelectItem key={perfil} value={perfil}>
                      {perfil}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('peopleProfiles.filters.status')}</label>
              <Select value={filtroStatus || "all"} onValueChange={(value) => setFiltroStatus(value === "all" ? "" : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('peopleProfiles.filters.allStatuses')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('peopleProfiles.filters.allStatuses')}</SelectItem>
                  <SelectItem value="ativo">{t('peopleProfiles.status.active')}</SelectItem>
                  <SelectItem value="inativo">{t('peopleProfiles.status.inactive')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('peopleProfiles.listTitle', { total })}</CardTitle>
        </CardHeader>
        <CardContent>
          {perfisFiltrados.length === 0 ? (
            <PageEmptyState
              title={perfis.length === 0 ? t('peopleProfiles.emptyNoneTitle') : t('peopleProfiles.emptyFilteredTitle')}
              description={perfis.length === 0
                ? t('peopleProfiles.emptyNoneDescription')
                : t('peopleProfiles.emptyFilteredDescription')}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('peopleProfiles.table.person')}</TableHead>
                  <TableHead>{t('peopleProfiles.table.profile')}</TableHead>
                  <TableHead>{t('peopleProfiles.table.startDate')}</TableHead>
                  <TableHead>{t('peopleProfiles.table.endDate')}</TableHead>
                  <TableHead>{t('peopleProfiles.table.status')}</TableHead>
                  <TableHead className="text-right">{t('peopleProfiles.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((perfil) => {
                  const isAtivo = !perfil.dataFim;
                  return (
                    <TableRow key={perfil.id}>
                      <TableCell className="font-medium">
                        <Link 
                          to={`/pessoas/${perfil.pessoaId}`}
                          className="text-primary hover:underline"
                        >
                          {perfil.nomePessoa || `Pessoa #${perfil.pessoaId}`}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">{getPerfilLabel(perfil.perfil, t)}</Badge>
                      </TableCell>
                      <TableCell>
                        {formatDate(perfil.dataInicio)}
                      </TableCell>
                      <TableCell>
                        {perfil.dataFim 
                          ? formatDate(perfil.dataFim)
                          : '-'}
                      </TableCell>
                      <TableCell>
                        <Badge variant={isAtivo ? 'default' : 'secondary'}>
                          {isAtivo ? t('peopleProfiles.status.active') : t('peopleProfiles.status.inactive')}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end space-x-2">
                          {isAtivo && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleEncerrarPerfil(perfil.id)}
                              title={t('peopleProfiles.endTitle')}
                            >
                              <X className="h-4 w-4" />
                            </Button>
                          )}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(perfil.id)}
                            title={t('peopleProfiles.deleteTitle')}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
          {perfisFiltrados.length > 0 && (
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
