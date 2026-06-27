import { useEffect, useMemo, useState } from 'react';
import { CircleDollarSign, RefreshCw, Search } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { usePagination } from '@/hooks/usePagination';
import { doacoesApi } from '@/lib/api';

const STATUS_OPTIONS = [
  { value: 'all', label: 'Todos' },
  { value: '1', label: 'Pendente' },
  { value: '2', label: 'Aguardando pagamento' },
  { value: '3', label: 'Confirmada' },
  { value: '4', label: 'Expirada' },
  { value: '5', label: 'Cancelada' },
  { value: '6', label: 'Falhou' },
  { value: '7', label: 'Estornada' },
];

function formatCurrency(value) {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(Number(value || 0));
}

function formatDate(value) {
  if (!value) return '-';
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}

function getStatusVariant(status) {
  if (status === 3) return 'default';
  if (status === 2) return 'secondary';
  if ([4, 5, 6, 7].includes(status)) return 'destructive';
  return 'outline';
}

export default function DoacoesList() {
  const [items, setItems] = useState([]);
  const [busca, setBusca] = useState('');
  const [status, setStatus] = useState('all');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);

  const load = async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const res = await doacoesApi.getAll();
      setItems(res.data || []);
    } catch (err) {
      setError('Não foi possível carregar as doações.');
      console.error(err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const filtered = useMemo(() => {
    const term = busca.trim().toLowerCase();
    return items.filter((item) => {
      const matchesStatus = status === 'all' || String(item.status) === status;
      const donor = item.anonima ? 'doador anonimo' : item.nomeDoador || '';
      const searchable = [
        donor,
        item.email,
        item.whatsApp,
        item.finalidadeNome,
        item.metodoPagamentoDescricao,
        item.statusDescricao,
        item.provider,
        item.reciboToken,
      ].filter(Boolean).join(' ').toLowerCase();

      return matchesStatus && (!term || searchable.includes(term));
    });
  }, [items, busca, status]);

  const totalConfirmado = filtered
    .filter((item) => item.status === 3)
    .reduce((sum, item) => sum + Number(item.valor || 0), 0);

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 20);

  if (loading) return <LoadingPage text="Carregando doações..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">Doações recebidas</h1>
          <p className="text-muted-foreground">Acompanhe doações iniciadas no Portal e a conciliação com o financeiro.</p>
        </div>
        <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total filtrado</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{filtered.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Confirmadas</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{filtered.filter((item) => item.status === 3).length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Valor confirmado</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(totalConfirmado)}</div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filtros</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-[1fr_260px]">
            <label className="flex flex-col gap-2 text-sm font-medium">
              <span className="flex items-center gap-2">
                <Search className="h-4 w-4" />
                Buscar
              </span>
              <Input value={busca} onChange={(event) => setBusca(event.target.value)} placeholder="Doador, finalidade, e-mail ou recibo" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium">
              <span>Status</span>
              <Select value={status} onValueChange={setStatus}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {STATUS_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Doações ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title="Nenhuma doação encontrada"
              description="Quando alguém iniciar uma contribuição pelo Portal, ela aparecerá aqui."
              action={(
                <Button variant="outline" onClick={() => load({ silent: true })}>
                  <RefreshCw className="mr-2 h-4 w-4" />
                  Atualizar
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Doador</TableHead>
                  <TableHead>Finalidade</TableHead>
                  <TableHead>Valor</TableHead>
                  <TableHead>Método</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Datas</TableHead>
                  <TableHead>Financeiro</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>
                      <div className="font-medium">{item.anonima ? 'Doador anônimo' : item.nomeDoador}</div>
                      <div className="text-xs text-muted-foreground">{item.email || item.whatsApp || 'Sem contato'}</div>
                    </TableCell>
                    <TableCell>{item.finalidadeNome || 'Doação geral'}</TableCell>
                    <TableCell className="font-medium">{formatCurrency(item.valor)}</TableCell>
                    <TableCell>{item.metodoPagamentoDescricao}</TableCell>
                    <TableCell>
                      <Badge variant={getStatusVariant(item.status)}>{item.statusDescricao}</Badge>
                    </TableCell>
                    <TableCell>
                      <div className="text-sm">Criada: {formatDate(item.dataCriacao)}</div>
                      <div className="text-xs text-muted-foreground">Confirmada: {formatDate(item.dataConfirmacao)}</div>
                    </TableCell>
                    <TableCell>
                      {item.receitaId ? (
                        <Badge variant="secondary" className="gap-1">
                          <CircleDollarSign className="h-3 w-3" />
                          Receita #{item.receitaId}
                        </Badge>
                      ) : (
                        <span className="text-sm text-muted-foreground">Aguardando confirmação</span>
                      )}
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
    </div>
  );
}
