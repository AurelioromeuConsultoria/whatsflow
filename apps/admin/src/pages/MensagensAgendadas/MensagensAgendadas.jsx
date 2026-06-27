import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  AlertTriangle,
  CalendarClock,
  CheckCircle2,
  Eye,
  MessageSquare,
  Play,
  Radio,
  RotateCw,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { AdvancedSearch } from '@/components/ui/advanced-search';
import { comunicacaoCampanhasApi, comunicacaoEntregasApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';

const STATUS = {
  DRAFT: 1,
  SCHEDULED: 2,
  PROCESSING: 3,
  COMPLETED: 4,
  COMPLETED_WITH_FAILURES: 5,
  CANCELED: 6,
};

const getStatusLabel = (status) => {
  switch (Number(status)) {
    case STATUS.DRAFT: return 'Rascunho';
    case STATUS.SCHEDULED: return 'Agendada';
    case STATUS.PROCESSING: return 'Processando';
    case STATUS.COMPLETED: return 'Concluida';
    case STATUS.COMPLETED_WITH_FAILURES: return 'Concluida com falhas';
    case STATUS.CANCELED: return 'Cancelada';
    default: return `Status ${status}`;
  }
};

const getStatusBadge = (status) => {
  switch (Number(status)) {
    case STATUS.SCHEDULED:
      return <Badge className="bg-blue-500 hover:bg-blue-600">Agendada</Badge>;
    case STATUS.PROCESSING:
      return <Badge variant="secondary">Processando</Badge>;
    case STATUS.COMPLETED:
      return <Badge className="bg-green-500 hover:bg-green-600">Concluida</Badge>;
    case STATUS.COMPLETED_WITH_FAILURES:
      return <Badge variant="destructive">Com falhas</Badge>;
    case STATUS.CANCELED:
      return <Badge variant="outline">Cancelada</Badge>;
    default:
      return <Badge variant="secondary">{getStatusLabel(status)}</Badge>;
  }
};

const getOrigemLabel = (campanha) => {
  if (String(campanha.objetivo || '').includes('onboarding-visitante')) {
    return 'Automacao de visitantes';
  }

  if (String(campanha.nome || '').toLowerCase().includes('automacao')) {
    return 'Automacao';
  }

  return campanha.publicoAlvo || 'Comunicacao';
};

export default function MensagensAgendadas() {
  const [campanhas, setCampanhas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [autoRefreshSeconds, setAutoRefreshSeconds] = useState(30);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [stats, setStats] = useState({
    totalCampanhas: 0,
    campanhasAgendadas: 0,
    entregasPendentes: 0,
    entregasEnviadas: 0,
    entregasComFalha: 0,
  });
  const [filters, setFilters] = useState({
    texto: '',
    status: undefined,
    publicoAlvo: undefined,
  });

  const load = useCallback(async ({ showLoader = false } = {}) => {
    try {
      setError(null);
      if (showLoader) setLoading(true);
      else setRefreshing(true);

      const [campanhasResponse, statsResponse] = await Promise.all([
        comunicacaoCampanhasApi.getPaged({
          page,
          pageSize,
          texto: filters.texto || undefined,
          status: filters.status ? Number(filters.status) : undefined,
          publicoAlvo: filters.publicoAlvo || undefined,
        }),
        comunicacaoCampanhasApi.getStats(),
      ]);

      const data = campanhasResponse.data || {};
      setCampanhas(data.items || []);
      setTotal(Number(data.total || 0));
      setStats(statsResponse.data || {});
    } catch (err) {
      const msg = getApiErrorMessage(err, 'Erro ao carregar a fila de comunicacao.');
      setError(msg);
      toast.error(msg);
    } finally {
      if (showLoader) setLoading(false);
      setRefreshing(false);
    }
  }, [filters.publicoAlvo, filters.status, filters.texto, page, pageSize]);

  useEffect(() => {
    load({ showLoader: true });
  }, [load]);

  useEffect(() => {
    if (!autoRefreshSeconds || autoRefreshSeconds <= 0) return;

    const intervalMs = autoRefreshSeconds * 1000;
    const id = window.setInterval(() => {
      if (document.visibilityState !== 'visible') return;
      if (loading || refreshing || processing) return;
      load();
    }, intervalMs);

    return () => window.clearInterval(id);
  }, [autoRefreshSeconds, load, loading, processing, refreshing]);

  useEffect(() => {
    setPage(1);
  }, [filters]);

  const publicoOptions = useMemo(() => {
    const values = new Set(campanhas.map((item) => item.publicoAlvo).filter(Boolean));
    return Array.from(values).map((value) => ({ value, label: value }));
  }, [campanhas]);

  const processarPendentes = async () => {
    try {
      setProcessing(true);
      const response = await comunicacaoEntregasApi.processarPendentes(100);
      toast.success(`${response.data?.processadas ?? 0} entrega(s) processada(s).`);
      await load();
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao processar entregas pendentes.'));
    } finally {
      setProcessing(false);
    }
  };

  if (loading) return <LoadingPage text="Carregando fila de comunicacao..." />;
  if (error) return <ErrorPage message={error} onRetry={() => load({ showLoader: true })} />;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold text-foreground sm:text-3xl">Fila de Comunicacao</h1>
          <p className="text-muted-foreground mt-1">
            Campanhas, automacoes e entregas criadas pelo modulo novo de comunicacao.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Auto-atualizar</span>
            <Select value={String(autoRefreshSeconds)} onValueChange={(value) => setAutoRefreshSeconds(Number(value))}>
              <SelectTrigger className="w-[140px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="0">Desligado</SelectItem>
                <SelectItem value="10">10s</SelectItem>
                <SelectItem value="30">30s</SelectItem>
                <SelectItem value="60">60s</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <PageRefreshButton onClick={() => load()} refreshing={refreshing} />
          <Button onClick={processarPendentes} disabled={processing}>
            {processing ? <RotateCw className="w-4 h-4 mr-2 animate-spin" /> : <Play className="w-4 h-4 mr-2" />}
            Processar pendentes
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center gap-4">
              <MessageSquare className="w-8 h-8 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium text-muted-foreground">Campanhas</p>
                <p className="text-2xl font-bold text-foreground">{stats.totalCampanhas ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center gap-4">
              <CalendarClock className="w-8 h-8 text-blue-500" />
              <div>
                <p className="text-sm font-medium text-muted-foreground">Agendadas</p>
                <p className="text-2xl font-bold text-blue-500">{stats.campanhasAgendadas ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center gap-4">
              <Radio className="w-8 h-8 text-amber-500" />
              <div>
                <p className="text-sm font-medium text-muted-foreground">Entregas pendentes</p>
                <p className="text-2xl font-bold text-amber-500">{stats.entregasPendentes ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center gap-4">
              {(stats.entregasComFalha ?? 0) > 0 ? (
                <AlertTriangle className="w-8 h-8 text-red-500" />
              ) : (
                <CheckCircle2 className="w-8 h-8 text-green-500" />
              )}
              <div>
                <p className="text-sm font-medium text-muted-foreground">Falhas</p>
                <p className={(stats.entregasComFalha ?? 0) > 0 ? 'text-2xl font-bold text-red-500' : 'text-2xl font-bold text-green-500'}>
                  {stats.entregasComFalha ?? 0}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <AdvancedSearch
        searchFields={[
          {
            key: 'texto',
            label: 'Campanha ou objetivo',
            type: 'text',
            placeholder: 'Buscar por nome, objetivo ou publico...',
          },
        ]}
        filterFields={[
          {
            key: 'status',
            label: 'Status da campanha',
            type: 'select',
            options: [
              { value: String(STATUS.DRAFT), label: 'Rascunho' },
              { value: String(STATUS.SCHEDULED), label: 'Agendada' },
              { value: String(STATUS.PROCESSING), label: 'Processando' },
              { value: String(STATUS.COMPLETED), label: 'Concluida' },
              { value: String(STATUS.COMPLETED_WITH_FAILURES), label: 'Com falhas' },
              { value: String(STATUS.CANCELED), label: 'Cancelada' },
            ],
          },
          {
            key: 'publicoAlvo',
            label: 'Publico',
            type: 'select',
            options: publicoOptions,
          },
        ]}
        values={filters}
        onChange={setFilters}
        onReset={() => setFilters({ texto: '', status: undefined, publicoAlvo: undefined })}
      />

      <Card>
        <CardHeader>
          <CardTitle>Campanhas e automacoes ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {campanhas.length === 0 ? (
            <PageEmptyState
              title={total === 0 ? 'Nenhuma comunicacao encontrada' : 'Nenhum resultado nesta pagina'}
              description={total === 0
                ? 'Quando um visitante for cadastrado ou uma campanha for criada, ela aparece aqui com suas entregas.'
                : 'Ajuste os filtros ou volte para a primeira pagina.'}
              icon={MessageSquare}
              action={(
                <Button asChild>
                  <Link to="/comunicacao/campanhas/nova">Criar campanha</Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Campanha</TableHead>
                  <TableHead>Origem</TableHead>
                  <TableHead>Agendamento</TableHead>
                  <TableHead>Entregas</TableHead>
                  <TableHead>Falhas</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Acoes</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {campanhas.map((campanha) => (
                  <TableRow key={campanha.id}>
                    <TableCell>
                      <div className="min-w-0">
                        <Link
                          to={`/comunicacao/campanhas/${campanha.id}`}
                          className="font-medium text-foreground hover:underline"
                        >
                          {campanha.nome}
                        </Link>
                        <div className="text-sm text-muted-foreground truncate max-w-[360px]">
                          {campanha.objetivo || 'Sem objetivo informado'}
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="text-sm text-foreground">{getOrigemLabel(campanha)}</div>
                      <div className="text-xs text-muted-foreground">{campanha.publicoAlvo || 'Sem publico'}</div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2 text-sm">
                        <CalendarClock className="w-4 h-4 text-muted-foreground" />
                        {campanha.dataAgendamento ? formatDateTime(campanha.dataAgendamento) : 'Envio manual ou rascunho'}
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="font-medium">{campanha.totalEntregas ?? 0}</span>
                    </TableCell>
                    <TableCell>
                      <span className={(campanha.totalFalhas ?? 0) > 0 ? 'font-medium text-red-500' : 'font-medium text-muted-foreground'}>
                        {campanha.totalFalhas ?? 0}
                      </span>
                    </TableCell>
                    <TableCell>{getStatusBadge(campanha.status)}</TableCell>
                    <TableCell>
                      <div className="flex justify-end">
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={`/comunicacao/campanhas/${campanha.id}`} title="Abrir detalhes">
                            <Eye className="w-4 h-4" />
                          </Link>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          {total > 0 && (
            <DataTablePagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              onPageSizeChange={(newSize) => {
                setPageSize(newSize);
                setPage(1);
              }}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
