import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Plus, Edit, Trash2, UserCheck, UserX, Search } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { formatDate, formatDateTime } from '@/lib/formatters';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { usuariosApi } from '@/lib/api';
import { toast } from 'sonner';
import UsuarioForm from './UsuarioForm';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';

export default function UsuariosList() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const [tipoFilter, setTipoFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const confirmDialog = useConfirmDialog();
  const { can, isAdmin } = useAuth();
  const pessoaIdParam = searchParams.get('pessoaId');
  const pessoaIdInicial = pessoaIdParam ? Number(pessoaIdParam) : null;

  const load = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const res = await usuariosApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('usersManagement.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  useEffect(() => {
    if (!showForm && pessoaIdInicial) {
      setEditingId(null);
      setShowForm(true);
    }
  }, [pessoaIdInicial, showForm]);

  const handleDelete = async (id) => {
    const usuario = items.find((u) => u.id === id);
    confirmDialog.show({
      title: t('usersManagement.deleteTitle'),
      description: t('usersManagement.deleteDescription', {
        name: usuario?.nome || t('usersManagement.deleteFallbackName'),
      }),
      confirmText: t('usersManagement.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await usuariosApi.delete(id);
          toast.success(t('usersManagement.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(err.response?.data?.message || t('usersManagement.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const handleToggleAtivo = async (usuario) => {
    try {
      await usuariosApi.update(usuario.id, {
        nome: usuario.nome,
        email: usuario.email,
        tipoUsuario: usuario.tipoUsuario,
        ativo: !usuario.ativo,
        perfilAcessoId: usuario.perfilAcessoId,
      });
      toast.success(
        !usuario.ativo
          ? t('usersManagement.activateSuccess')
          : t('usersManagement.deactivateSuccess')
      );
      await load();
    } catch (err) {
      toast.error(t('usersManagement.statusError'));
      console.error(err);
    }
  };

  const handleEdit = (id) => {
    setEditingId(id);
    setShowForm(true);
  };

  const handleCloseForm = () => {
    setShowForm(false);
    setEditingId(null);
    if (pessoaIdParam) {
      setSearchParams({});
    }
  };

  const handleFormSuccess = () => {
    handleCloseForm();
    load();
  };

  const filtered = items.filter((u) => {
    if (busca && !u.nome?.toLowerCase().includes(busca.toLowerCase()) && !u.email?.toLowerCase().includes(busca.toLowerCase())) return false;
    if (tipoFilter && String(u.tipoUsuario) !== tipoFilter) return false;
    if (statusFilter !== '' && String(u.ativo) !== statusFilter) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('usersManagement.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  const canEdit = isAdmin && can(RESOURCES.USUARIOS, ACTIONS.EDIT);
  const canDelete = isAdmin && can(RESOURCES.USUARIOS, ACTIONS.DELETE);
  const userTypeLabels = {
    1: t('usersManagement.userTypes.administrator'),
    2: t('usersManagement.userTypes.portal'),
    3: t('usersManagement.userTypes.both'),
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('usersManagement.title')}</h1>
          <p className="text-muted-foreground">{t('usersManagement.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button
              onClick={() => {
                setSearchParams({});
                setEditingId(null);
                setShowForm(true);
              }}
            >
              <Plus className="h-4 w-4 mr-2" /> {t('usersManagement.new')}
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('usersManagement.filters.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('usersManagement.filters.search')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('usersManagement.filters.searchPlaceholder')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('usersManagement.filters.userType')}</label>
              <Select value={tipoFilter || 'all'} onValueChange={(value) => setTipoFilter(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('usersManagement.filters.allTypes')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('usersManagement.filters.allTypes')}</SelectItem>
                  <SelectItem value="1">{t('usersManagement.userTypes.administrator')}</SelectItem>
                  <SelectItem value="2">{t('usersManagement.userTypes.portal')}</SelectItem>
                  <SelectItem value="3">{t('usersManagement.userTypes.both')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('usersManagement.filters.status')}</label>
              <Select value={statusFilter || 'all'} onValueChange={(value) => setStatusFilter(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('usersManagement.filters.allStatuses')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('usersManagement.filters.allStatuses')}</SelectItem>
                  <SelectItem value="true">{t('usersManagement.status.active')}</SelectItem>
                  <SelectItem value="false">{t('usersManagement.status.inactive')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('usersManagement.listTitle', { total })}</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('usersManagement.emptyTitle')}
              description={t('usersManagement.emptyDescription')}
              action={canEdit ? (
                <Button
                  onClick={() => {
                    setSearchParams({});
                    setEditingId(null);
                    setShowForm(true);
                  }}
                >
                  <Plus className="mr-2 h-4 w-4" />
                  {t('usersManagement.new')}
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('usersManagement.table.name')}</TableHead>
                  <TableHead>{t('usersManagement.table.email')}</TableHead>
                  <TableHead>{t('usersManagement.table.type')}</TableHead>
                  <TableHead>{t('usersManagement.table.profile')}</TableHead>
                  <TableHead>{t('usersManagement.table.status')}</TableHead>
                  <TableHead>{t('usersManagement.table.createdAt')}</TableHead>
                  <TableHead>{t('usersManagement.table.lastAccess')}</TableHead>
                  <TableHead className="text-right">{t('usersManagement.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((usuario) => (
                  <TableRow key={usuario.id}>
                    <TableCell className="font-medium">{usuario.nome}</TableCell>
                    <TableCell>{usuario.email}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">
                        {userTypeLabels[usuario.tipoUsuario] || usuario.tipoUsuarioDescricao}
                      </Badge>
                    </TableCell>
                    <TableCell>{usuario.perfilAcessoNome || '-'}</TableCell>
                    <TableCell>
                      <Badge variant={usuario.ativo ? 'default' : 'secondary'}>
                        {usuario.ativo ? t('usersManagement.status.active') : t('usersManagement.status.inactive')}
                      </Badge>
                    </TableCell>
                    <TableCell>{formatDate(usuario.dataCriacao)}</TableCell>
                    <TableCell>{usuario.ultimoAcesso ? formatDateTime(usuario.ultimoAcesso) : t('usersManagement.never')}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end space-x-1">
                        {canEdit && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleToggleAtivo(usuario)}
                            title={usuario.ativo ? t('usersManagement.actions.deactivate') : t('usersManagement.actions.activate')}
                          >
                            {usuario.ativo ? <UserX className="h-4 w-4 text-red-600" /> : <UserCheck className="h-4 w-4 text-green-600" />}
                          </Button>
                        )}
                        {canEdit && (
                          <Button variant="ghost" size="sm" onClick={() => handleEdit(usuario.id)} title={t('actions.edit')}>
                            <Edit className="h-4 w-4" />
                          </Button>
                        )}
                        {canDelete && (
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(usuario.id)} title={t('usersManagement.deleteConfirm')}>
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

      {showForm && (
        <UsuarioForm
          id={editingId}
          pessoaIdInicial={editingId ? null : pessoaIdInicial}
          onClose={handleCloseForm}
          onSuccess={handleFormSuccess}
        />
      )}
    </div>
  );
}
