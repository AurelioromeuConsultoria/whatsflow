import { useCallback, useEffect, useState } from 'react';
import { RotateCcw, Search, Send } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { StatusBadge } from '@/components/ui/status-badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { useDebouncedCallback } from '@/hooks/useDebouncedCallback';
import { comunicacaoEntregasApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';
import { toast } from 'sonner';

const CANAL_LABEL = { 1: 'WhatsApp', 2: 'E-mail', 3: 'Push', 4: 'Notificação interna' };

const STATUS = {
  1: { label: 'Pendente', tone: 'warning' },
  2: { label: 'Reservada', tone: 'info' },
  3: { label: 'Enviada', tone: 'info' },
  4: { label: 'Entregue', tone: 'success' },
  5: { label: 'Falha', tone: 'danger' },
  6: { label: 'Cancelada', tone: 'neutral' },
  7: { label: 'Ignorada', tone: 'neutral' },
};

export default function ComunicacaoEntregasList() {
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [texto, setTexto] = useState('');
  const [canalFilter, setCanalFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState(null);

  const load = useCallback(async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const params = { page, pageSize };
      if (texto.trim()) params.texto = texto.trim();
      if (canalFilter !== 'all') params.canal = Number(canalFilter);
      if (statusFilter !== 'all') params.status = Number(statusFilter);
      const res = await comunicacaoEntregasApi.getPaged(params);
      setItems(res.data?.items || []);
      setTotal(res.data?.total || 0);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Erro ao carregar logs de mensagens.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [page, pageSize, texto, canalFilter, statusFilter]);

  useEffect(() => { load(); }, [load]);

  const debouncedSearch = useDebouncedCallback(() => setPage(1), 400);
  const handleTextoChange = (value) => { setTexto(value); debouncedSearch(); };

  const processarPendentes = async () => {
    try {
      setProcessing(true);
      const res = await comunicacaoEntregasApi.processarPendentes(100);
      toast.success(`${res.data?.processadas ?? 0} entrega(s) processada(s).`);
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao processar pendentes.'));
    } finally {
      setProcessing(false);
    }
  };

  const reprocessar = async (entregaId) => {
    try {
      await comunicacaoEntregasApi.reprocessar(entregaId);
      toast.success('Entrega reprocessada.');
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao reprocessar entrega.'));
    }
  };

  if (loading) return <LoadingPage text="Carregando logs..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">Logs de mensagens</h1>
          <p className="text-muted-foreground">Entregas processadas por canal, com status e erros.</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button onClick={processarPendentes} disabled={processing}>
            <Send className="h-4 w-4 mr-2" /> {processing ? 'Processando...' : 'Processar pendentes'}
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filtros</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />Buscar destino</label>
              <Input value={texto} onChange={(e) => handleTextoChange(e.target.value)} placeholder="Telefone ou e-mail" />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Canal</label>
              <Select value={canalFilter} onValueChange={(v) => { setCanalFilter(v); setPage(1); }}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="1">WhatsApp</SelectItem>
                  <SelectItem value="2">E-mail</SelectItem>
                  <SelectItem value="3">Push</SelectItem>
                  <SelectItem value="4">Notificação interna</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <Select value={statusFilter} onValueChange={(v) => { setStatusFilter(v); setPage(1); }}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  {Object.entries(STATUS).map(([value, s]) => (
                    <SelectItem key={value} value={value}>{s.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{total} entrega(s)</CardTitle>
        </CardHeader>
        <CardContent>
          {items.length === 0 ? (
            <PageEmptyState title="Nenhuma entrega encontrada" description="Ajuste os filtros ou aguarde novas mensagens." />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Canal</TableHead>
                  <TableHead>Destino</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Tentativas</TableHead>
                  <TableHead>Processado em</TableHead>
                  <TableHead>Erro</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((e) => {
                  const status = STATUS[Number(e.status)] || STATUS[1];
                  return (
                    <TableRow key={e.id}>
                      <TableCell>{CANAL_LABEL[Number(e.canal)] ?? e.canal}</TableCell>
                      <TableCell>{e.destinoResolvido || '-'}</TableCell>
                      <TableCell><StatusBadge tone={status.tone}>{status.label}</StatusBadge></TableCell>
                      <TableCell>{e.tentativas}</TableCell>
                      <TableCell>{e.processadoEm ? formatDateTime(e.processadoEm) : '-'}</TableCell>
                      <TableCell className="max-w-[280px] whitespace-normal text-sm text-muted-foreground">{e.erro || '-'}</TableCell>
                      <TableCell className="text-right">
                        {e.podeReprocessar && (
                          <Button variant="outline" size="sm" onClick={() => reprocessar(e.id)}>
                            <RotateCcw className="h-4 w-4 mr-2" /> Reprocessar
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
          {total > 0 && (
            <DataTablePagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
