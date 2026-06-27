import { useCallback, useEffect, useState } from 'react';
import { ShieldCheck, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { platformBillingApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDate } from '@/lib/formatters';
import { useAuth } from '@/context/AuthContext';
import { toast } from 'sonner';

const STATUS_META = {
  Trial: { label: 'Teste', variant: 'secondary' },
  Ativa: { label: 'Ativa', variant: 'default' },
  Inadimplente: { label: 'Inadimplente', variant: 'destructive' },
  Suspensa: { label: 'Suspensa', variant: 'destructive' },
  Cancelada: { label: 'Cancelada', variant: 'outline' },
};

const moeda = (v) => (v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

export default function AdminAssinaturas() {
  const { isPlatformAdmin } = useAuth();
  const [assinaturas, setAssinaturas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [processando, setProcessando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    try {
      const resp = await platformBillingApi.assinaturas();
      setAssinaturas(resp.data ?? []);
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao carregar assinaturas.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isPlatformAdmin) carregar();
    else setLoading(false);
  }, [isPlatformAdmin, carregar]);

  const alterar = async (tenantId, acao) => {
    try {
      await (acao === 'suspender' ? platformBillingApi.suspender(tenantId) : platformBillingApi.reativar(tenantId));
      toast.success(acao === 'suspender' ? 'Assinatura suspensa.' : 'Assinatura reativada.');
      await carregar();
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao atualizar a assinatura.'));
    }
  };

  const processarCiclo = async () => {
    try {
      setProcessando(true);
      const resp = await platformBillingApi.processarCiclo();
      const r = resp.data ?? {};
      toast.success(`Ciclo processado: ${r.avisosTrialEnviados ?? 0} avisos, ${r.trialsExpirados ?? 0} expirados, ${r.suspensos ?? 0} suspensos.`);
      await carregar();
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao processar o ciclo.'));
    } finally {
      setProcessando(false);
    }
  };

  if (!isPlatformAdmin) {
    return <ErrorPage message="Acesso restrito ao administrador da plataforma." />;
  }

  if (loading) return <LoadingPage />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ShieldCheck className="h-7 w-7 text-primary" />
          <div>
            <h1 className="text-3xl font-bold">Assinaturas (plataforma)</h1>
            <p className="text-muted-foreground">Visão de todas as organizações e gestão de cobrança.</p>
          </div>
        </div>
        <Button variant="outline" onClick={processarCiclo} disabled={processando}>
          <RefreshCw className="h-4 w-4 mr-2" />
          {processando ? 'Processando...' : 'Processar ciclo agora'}
        </Button>
      </div>

      <Card>
        <CardHeader><CardTitle>Assinaturas</CardTitle></CardHeader>
        <CardContent>
          {assinaturas.length === 0 ? (
            <p className="text-muted-foreground py-8 text-center">Nenhuma assinatura.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Organização</TableHead>
                  <TableHead>Plano</TableHead>
                  <TableHead>Valor</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Próxima cobrança</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {assinaturas.map((a) => {
                  const meta = STATUS_META[a.status] ?? { label: a.status, variant: 'secondary' };
                  return (
                    <TableRow key={a.id}>
                      <TableCell>{a.tenantNome || `Tenant #${a.tenantId}`}</TableCell>
                      <TableCell>{a.planoNome}</TableCell>
                      <TableCell>{moeda(a.valor)}</TableCell>
                      <TableCell><Badge variant={meta.variant}>{meta.label}</Badge></TableCell>
                      <TableCell>{a.proximaCobranca ? formatDate(a.proximaCobranca) : '—'}</TableCell>
                      <TableCell className="text-right space-x-2 whitespace-nowrap">
                        {a.status !== 'Suspensa' && a.status !== 'Cancelada' && (
                          <Button variant="outline" size="sm" className="text-destructive hover:text-destructive" onClick={() => alterar(a.tenantId, 'suspender')}>Suspender</Button>
                        )}
                        {a.status === 'Suspensa' && (
                          <Button variant="outline" size="sm" onClick={() => alterar(a.tenantId, 'reativar')}>Reativar</Button>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
