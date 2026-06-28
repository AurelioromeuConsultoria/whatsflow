import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { TableRowActions, RowIconButtonAction, RowIconLinkAction } from '@/components/ui/list-actions';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { tagsApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { toast } from 'sonner';

export default function TagsList() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const confirmDialog = useConfirmDialog();

  const load = useCallback(async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const res = await tagsApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Erro ao carregar tags.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleDelete = (tag) => {
    confirmDialog.show({
      title: 'Remover tag',
      description: `Tem certeza que deseja remover a tag "${tag.nome}"?`,
      confirmText: 'Remover',
      cancelText: 'Cancelar',
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await tagsApi.delete(tag.id);
          toast.success('Tag removida.');
          await load({ silent: true });
        } catch (err) {
          toast.error(getApiErrorMessage(err, 'Erro ao remover tag.'));
          throw err;
        }
      },
    });
  };

  if (loading) return <LoadingPage text="Carregando tags..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Tags</h1>
          <p className="text-muted-foreground">Organize os contatos com etiquetas coloridas.</p>
        </div>
        <Button asChild>
          <Link to="/tags/novo"><Plus className="h-4 w-4 mr-2" /> Nova tag</Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{items.length} tag(s)</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {items.length === 0 ? (
            <PageEmptyState
              title="Nenhuma tag cadastrada"
              description="Crie tags para segmentar seus contatos."
              action={(
                <Button asChild>
                  <Link to="/tags/novo"><Plus className="h-4 w-4 mr-2" /> Nova tag</Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Cor</TableHead>
                  <TableHead>Nome</TableHead>
                  <TableHead>Contatos</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((tag) => (
                  <TableRow key={tag.id}>
                    <TableCell>
                      <span className="inline-block h-5 w-5 rounded-full border" style={{ backgroundColor: tag.cor || '#e5e7eb' }} />
                    </TableCell>
                    <TableCell className="font-medium">{tag.nome}</TableCell>
                    <TableCell>{tag.totalContatos ?? 0}</TableCell>
                    <TableCell className="text-right">
                      <TableRowActions>
                        <RowIconLinkAction>
                          <Link to={`/tags/${tag.id}/editar`}><Edit className="h-4 w-4" /></Link>
                        </RowIconLinkAction>
                        <RowIconButtonAction onClick={() => handleDelete(tag)}>
                          <Trash2 className="h-4 w-4" />
                        </RowIconButtonAction>
                      </TableRowActions>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
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
