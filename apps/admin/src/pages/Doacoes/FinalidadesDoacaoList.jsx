import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Edit, Plus, Search, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { usePagination } from '@/hooks/usePagination';
import { finalidadesDoacaoApi } from '@/lib/api';
import { useAuth } from '@/context/AuthContext';
import { ACTIONS, RESOURCES } from '@/utils/permissions';

export default function FinalidadesDoacaoList() {
  const [items, setItems] = useState([]);
  const [busca, setBusca] = useState('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const confirmDialog = useConfirmDialog();
  const { can } = useAuth();

  const load = async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const res = await finalidadesDoacaoApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError('Não foi possível carregar as finalidades de doação.');
      console.error(err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = (id) => {
    const item = items.find((finalidade) => finalidade.id === id);
    confirmDialog.show({
      title: 'Excluir finalidade',
      description: `Deseja excluir "${item?.nome || 'esta finalidade'}"? Ela deixará de aparecer no Portal.`,
      confirmText: 'Excluir',
      cancelText: 'Cancelar',
      variant: 'destructive',
      onConfirm: async () => {
        await finalidadesDoacaoApi.delete(id);
        toast.success('Finalidade excluída.');
        await load();
      },
    });
  };

  const filtered = items.filter((item) => {
    const term = busca.toLowerCase();
    return !term || item.nome?.toLowerCase().includes(term) || item.slug?.toLowerCase().includes(term);
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);
  const canEdit = can(RESOURCES.FINANCEIRO, ACTIONS.EDIT);
  const canDelete = can(RESOURCES.FINANCEIRO, ACTIONS.DELETE);

  if (loading) return <LoadingPage text="Carregando finalidades de doação..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">Finalidades de doação</h1>
          <p className="text-muted-foreground">Controle o que aparece na página pública de generosidade.</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {canEdit && (
            <Button asChild>
              <Link to="/doacoes/finalidades/novo">
                <Plus className="mr-2 h-4 w-4" />
                Nova finalidade
              </Link>
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filtros</CardTitle>
        </CardHeader>
        <CardContent>
          <label className="flex max-w-md flex-col gap-2 text-sm font-medium">
            <span className="flex items-center gap-2">
              <Search className="h-4 w-4" />
              Buscar
            </span>
            <Input value={busca} onChange={(event) => setBusca(event.target.value)} placeholder="Nome ou slug" />
          </label>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Finalidades ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title="Nenhuma finalidade encontrada"
              description="Cadastre Dízimos, Ofertas, Missões ou outras opções para liberar a página pública de doações."
              action={canEdit ? (
                <Button asChild>
                  <Link to="/doacoes/finalidades/novo">
                    <Plus className="mr-2 h-4 w-4" />
                    Nova finalidade
                  </Link>
                </Button>
              ) : null}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Slug</TableHead>
                  <TableHead>Métodos</TableHead>
                  <TableHead>Vínculo financeiro</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className="font-medium">{item.nome}</TableCell>
                    <TableCell>{item.slug}</TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {item.permitePix && <Badge variant="secondary">Pix</Badge>}
                        {item.permiteCartaoCredito && <Badge variant="secondary">Cartão</Badge>}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="text-sm">
                        <div>{item.categoriaReceitaNome || 'Sem categoria'}</div>
                        <div className="text-muted-foreground">{item.projetoNome || item.centroCustoNome || 'Sem projeto/centro'}</div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        <Badge variant={item.ativo ? 'default' : 'outline'}>{item.ativo ? 'Ativa' : 'Inativa'}</Badge>
                        <Badge variant={item.visivelPortal ? 'secondary' : 'outline'}>{item.visivelPortal ? 'Portal' : 'Oculta'}</Badge>
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        {canEdit && (
                          <Button variant="ghost" size="sm" asChild>
                            <Link to={`/doacoes/finalidades/${item.id}/editar`}>
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
