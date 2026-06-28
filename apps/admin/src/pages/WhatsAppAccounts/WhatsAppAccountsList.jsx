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
import { StatusBadge } from '@/components/ui/status-badge';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { whatsappAccountsApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { toast } from 'sonner';

export const PROVIDER_LABEL = {
  0: 'Fake (testes)',
  1: 'Cloud API (oficial)',
  2: 'Evolution API',
  3: 'Twilio',
  4: 'Zenvia',
  99: 'Outro',
};

const STATUS = {
  1: { label: 'Ativa', tone: 'success' },
  2: { label: 'Inativa', tone: 'neutral' },
  3: { label: 'Erro de configuração', tone: 'danger' },
};

export default function WhatsAppAccountsList() {
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
      const res = await whatsappAccountsApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Erro ao carregar contas WhatsApp.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleDelete = (account) => {
    confirmDialog.show({
      title: 'Remover conta',
      description: `Tem certeza que deseja remover a conta "${account.nome}"?`,
      confirmText: 'Remover',
      cancelText: 'Cancelar',
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await whatsappAccountsApi.delete(account.id);
          toast.success('Conta removida.');
          await load({ silent: true });
        } catch (err) {
          toast.error(getApiErrorMessage(err, 'Erro ao remover conta.'));
          throw err;
        }
      },
    });
  };

  if (loading) return <LoadingPage text="Carregando contas..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Conta WhatsApp</h1>
          <p className="text-muted-foreground">Provedores de envio conectados ao seu workspace.</p>
        </div>
        <Button asChild>
          <Link to="/whatsapp/contas/novo"><Plus className="h-4 w-4 mr-2" /> Nova conta</Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{items.length} conta(s)</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {items.length === 0 ? (
            <PageEmptyState
              title="Nenhuma conta configurada"
              description="Conecte um provedor de WhatsApp para começar a enviar mensagens."
              action={(
                <Button asChild>
                  <Link to="/whatsapp/contas/novo"><Plus className="h-4 w-4 mr-2" /> Nova conta</Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Provedor</TableHead>
                  <TableHead>Phone Number ID</TableHead>
                  <TableHead>Token</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((acc) => {
                  const status = STATUS[Number(acc.status)] || STATUS[2];
                  return (
                    <TableRow key={acc.id}>
                      <TableCell className="font-medium">{acc.nome}</TableCell>
                      <TableCell>{PROVIDER_LABEL[Number(acc.provider)] ?? acc.provider}</TableCell>
                      <TableCell>{acc.phoneNumberId || '-'}</TableCell>
                      <TableCell>
                        <StatusBadge tone={acc.possuiAccessToken ? 'success' : 'neutral'}>
                          {acc.possuiAccessToken ? 'Configurado' : 'Não configurado'}
                        </StatusBadge>
                      </TableCell>
                      <TableCell><StatusBadge tone={status.tone}>{status.label}</StatusBadge></TableCell>
                      <TableCell className="text-right">
                        <TableRowActions>
                          <RowIconLinkAction>
                            <Link to={`/whatsapp/contas/${acc.id}/editar`}><Edit className="h-4 w-4" /></Link>
                          </RowIconLinkAction>
                          <RowIconButtonAction onClick={() => handleDelete(acc)}>
                            <Trash2 className="h-4 w-4" />
                          </RowIconButtonAction>
                        </TableRowActions>
                      </TableCell>
                    </TableRow>
                  );
                })}
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
