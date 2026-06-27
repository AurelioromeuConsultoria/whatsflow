import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Filter, Phone, Mail, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { TableRowActions, RowIconButtonAction, RowIconLinkAction } from '@/components/ui/list-actions';
import { BooleanStatusBadge } from '@/components/ui/status-badge';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { contatosApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function ContatosList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const [membroFilter, setMembroFilter] = useState('');
  const confirmDialog = useConfirmDialog();

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
      const res = await contatosApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(t('contactsList.errorLoad'));
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
    const contato = items.find(c => c.id === id);
    confirmDialog.show({
      title: t('contactsList.deleteTitle'),
      description: t('contactsList.deleteDescription', {
        name: contato?.nome || t('contactsList.deleteFallbackName'),
      }),
      confirmText: t('contactsList.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await contatosApi.delete(id);
          toast.success(t('contactsList.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('contactsList.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((c) => {
    if (busca && !c.nome?.toLowerCase().includes(busca.toLowerCase()) && !c.email?.toLowerCase().includes(busca.toLowerCase())) return false;
    if (membroFilter !== '' && String(c.membro) !== membroFilter) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text={t('contactsList.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('contactsList.title')}</h1>
          <p className="text-muted-foreground">{t('contactsList.subtitle')}</p>
        </div>
        <Button asChild>
          <Link to="/contatos/novo">
            <Plus className="h-4 w-4 mr-2" /> {t('contactsList.new')}
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('contactsList.filters.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('contactsList.filters.search')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('contactsList.filters.searchPlaceholder')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('contactsList.filters.member')}</label>
              <Select value={membroFilter || 'all'} onValueChange={(value) => setMembroFilter(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('contactsList.filters.all')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('contactsList.filters.all')}</SelectItem>
                  <SelectItem value="true">{t('contactsList.filters.memberOnly')}</SelectItem>
                  <SelectItem value="false">{t('contactsList.filters.nonMember')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('contactsList.listTitle', { total })}</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('contactsList.emptyTitle')}
              description={t('contactsList.emptyDescription')}
              action={(
                <Button asChild>
                  <Link to="/contatos/novo">
                    <Plus className="h-4 w-4 mr-2" /> {t('contactsList.new')}
                  </Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('contactsList.table.name')}</TableHead>
                  <TableHead>{t('contactsList.table.whatsapp')}</TableHead>
                  <TableHead>{t('contactsList.table.email')}</TableHead>
                  <TableHead>{t('contactsList.table.member')}</TableHead>
                  <TableHead>{t('contactsList.table.message')}</TableHead>
                  <TableHead>{t('contactsList.table.createdAt')}</TableHead>
                  <TableHead className="text-right">{t('contactsList.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((contato) => (
                  <TableRow key={contato.id}>
                    <TableCell className="font-medium">{contato.nome || '-'}</TableCell>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        <span>{contato.whatsApp || '-'}</span>
                        {contato.whatsApp && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => window.open(`https://wa.me/55${contato.whatsApp.replace(/\D/g, '')}`)}
                          >
                            <Phone className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        <span>{contato.email || '-'}</span>
                        {contato.email && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => window.open(`mailto:${contato.email}`)}
                          >
                            <Mail className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <BooleanStatusBadge value={contato.membro} trueLabel={t('contactsList.boolean.yes')} falseLabel={t('contactsList.boolean.no')} trueTone="info" />
                    </TableCell>
                    <TableCell>{contato.mensagem ? (contato.mensagem.length > 50 ? `${contato.mensagem.substring(0, 50)}...` : contato.mensagem) : '-'}</TableCell>
                    <TableCell>{formatDate(contato.dataCriacao)}</TableCell>
                    <TableCell className="text-right">
                      <TableRowActions>
                        <RowIconLinkAction>
                          <Link to={`/contatos/${contato.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </RowIconLinkAction>
                        <RowIconButtonAction onClick={() => handleDelete(contato.id)}>
                          <Trash2 className="h-4 w-4" />
                        </RowIconButtonAction>
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




