import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, CalendarDays, CheckCircle2, Clock3, Filter, Settings, XCircle } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { usePagination } from '@/hooks/usePagination';
import { eventosApi, eventosOcorrenciasApi } from '@/lib/api';
import { escalasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { formatDateTime } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';

function getRiskClassName(nivelRisco) {
  if (nivelRisco === 'high') return 'bg-red-100 text-red-800 hover:bg-red-100';
  if (nivelRisco === 'attention') return 'bg-amber-100 text-amber-800 hover:bg-amber-100';
  if (nivelRisco === 'none') return 'bg-slate-100 text-slate-800 hover:bg-slate-100';
  return 'bg-green-100 text-green-800 hover:bg-green-100';
}

export default function PainelCoberturaVoluntariado() {
  const { isAdmin } = useAuth();
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [eventos, setEventos] = useState([]);
  const [cards, setCards] = useState([]);
  const [filtroEventoId, setFiltroEventoId] = useState('all');
  const [filtroRisco, setFiltroRisco] = useState('all');
  const [busca, setBusca] = useState('');
  const [dataInicio, setDataInicio] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 10);
  });
  const [dataFim, setDataFim] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() + 30);
    return d.toISOString().slice(0, 10);
  });

  const load = async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);

      const eventoId = filtroEventoId === 'all' ? undefined : Number(filtroEventoId);
      const [eventosRes, ocorrenciasRes] = await Promise.all([
        eventosApi.getAll(),
        eventosOcorrenciasApi.getCoberturaVoluntariado({
          dataInicio: `${dataInicio}T00:00:00`,
          dataFim: `${dataFim}T23:59:59`,
          eventoId,
          nivelRisco: filtroRisco === 'all' ? undefined : filtroRisco,
        }),
      ]);

      setEventos(eventosRes.data || []);
      setCards(ocorrenciasRes.data || []);
    } catch (err) {
      console.error(err);
      setError(t('volunteer.coverage.errorLoad'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    load();
  }, [filtroEventoId, filtroRisco, dataInicio, dataFim]);

  const resumoGeral = useMemo(() => {
    return cards.reduce((acc, item) => {
      acc.ocorrencias += 1;
      acc.vagas += item.totalVagas;
      acc.confirmados += item.confirmados;
      acc.pendentes += item.pendentes;
      acc.recusados += item.recusados;
      if (item.nivelRisco === 'high') acc.riscoAlto += 1;
      if (item.nivelRisco === 'attention') acc.atencao += 1;
      if (item.nivelRisco === 'none') acc.semEscala += 1;
      return acc;
    }, {
      ocorrencias: 0,
      vagas: 0,
      confirmados: 0,
      pendentes: 0,
      recusados: 0,
      riscoAlto: 0,
      atencao: 0,
      semEscala: 0,
    });
  }, [cards]);

  const filtered = useMemo(() => {
    return cards
      .filter((item) => !busca.trim() || item.eventoTitulo?.toLowerCase().includes(busca.trim().toLowerCase()))
      .sort((a, b) => {
        if (a.ordemRisco !== b.ordemRisco) return a.ordemRisco - b.ordemRisco;
        return new Date(a.dataHoraInicio) - new Date(b.dataHoraInicio);
      });
  }, [busca, cards]);

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 12);

  const handleProcessarLembretes = async () => {
    try {
      const res = await escalasApi.processarLembretes();
      const totalEnviados = res.data?.totalEnviados ?? 0;
      toast.success(
        totalEnviados > 0
          ? t('volunteer.coverage.remindersSent', { count: totalEnviados })
          : t('volunteer.coverage.noPendingReminders')
      );
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.coverage.errorProcessReminders'));
      toast.error(message);
    }
  };

  if (loading) return <LoadingPage text={t('volunteer.coverage.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.coverage.title')}</h1>
          <p className="text-muted-foreground">{t('volunteer.coverage.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          {isAdmin && (
            <Button variant="outline" onClick={handleProcessarLembretes}>
              {t('volunteer.coverage.processReminders')}
            </Button>
          )}
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-4 xl:grid-cols-7">
        <Card>
          <CardHeader><CardTitle>{t('volunteer.coverage.summary.occurrences')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumoGeral.ocorrencias}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.coverage.summary.highRisk')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-red-600">{resumoGeral.riscoAlto}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.coverage.summary.attention')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-amber-600">{resumoGeral.atencao}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.coverage.summary.noSchedule')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-slate-600">{resumoGeral.semEscala}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.coverage.summary.slots')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumoGeral.vagas}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.coverage.summary.pending')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-amber-600">{resumoGeral.pendentes}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.coverage.summary.declines')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-red-600">{resumoGeral.recusados}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Filter className="h-4 w-4" />
            {t('volunteer.coverage.filtersTitle')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <Label>{t('volunteer.coverage.eventLabel')}</Label>
              <Select value={filtroEventoId} onValueChange={setFiltroEventoId}>
                <SelectTrigger>
                  <SelectValue placeholder={t('volunteer.coverage.allEventsOption')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.coverage.allEventsOption')}</SelectItem>
                  {eventos.map((evento) => (
                    <SelectItem key={evento.id} value={String(evento.id)}>
                      {evento.titulo}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.coverage.startDateLabel')}</Label>
              <Input type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.coverage.endDateLabel')}</Label>
              <Input type="date" value={dataFim} onChange={(e) => setDataFim(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.coverage.riskLabel')}</Label>
              <Select value={filtroRisco} onValueChange={setFiltroRisco}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.coverage.riskOptions.all')}</SelectItem>
                  <SelectItem value="high">{t('volunteer.coverage.riskOptions.high')}</SelectItem>
                  <SelectItem value="attention">{t('volunteer.coverage.riskOptions.attention')}</SelectItem>
                  <SelectItem value="none">{t('volunteer.coverage.riskOptions.none')}</SelectItem>
                  <SelectItem value="ok">{t('volunteer.coverage.riskOptions.ok')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="pt-4">
            <Label>{t('volunteer.coverage.searchLabel')}</Label>
            <Input
              value={busca}
              onChange={(e) => setBusca(e.target.value)}
              placeholder={t('volunteer.coverage.searchPlaceholder')}
            />
          </div>
        </CardContent>
      </Card>

      <div className="space-y-4">
        {paginatedItems.length === 0 ? (
          <Card>
            <CardContent>
              <PageEmptyState
                title={t('volunteer.coverage.emptyTitle')}
                description={t('volunteer.coverage.emptyDescription')}
              />
            </CardContent>
          </Card>
        ) : (
          paginatedItems.map((item) => (
            <Card key={item.ocorrenciaId}>
              <CardHeader>
                <CardTitle className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                  <div>
                    <div>{item.eventoTitulo}</div>
                    <div className="mt-1 flex items-center gap-2 text-sm font-normal text-muted-foreground">
                      <CalendarDays className="h-4 w-4" />
                      {formatDateTime(item.dataHoraInicio)}
                    </div>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge className={getRiskClassName(item.nivelRisco)}>{item.rotuloRisco}</Badge>
                    <Button variant="outline" size="sm" asChild>
                      <Link to={`/voluntariado/escalas/ocorrencia/${item.ocorrenciaId}`}>
                        <Settings className="h-4 w-4 mr-2" />
                        {t('volunteer.coverage.openOccurrence')}
                      </Link>
                    </Button>
                  </div>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap gap-3 text-sm">
                  <span className="inline-flex items-center gap-1"><CheckCircle2 className="h-4 w-4 text-green-600" /> {item.confirmados} {t('volunteer.coverage.metrics.confirmed')}</span>
                  <span className="inline-flex items-center gap-1"><Clock3 className="h-4 w-4 text-amber-600" /> {item.pendentes} {t('volunteer.coverage.metrics.pending')}</span>
                  <span className="inline-flex items-center gap-1"><XCircle className="h-4 w-4 text-red-600" /> {item.recusados} {t('volunteer.coverage.metrics.declines')}</span>
                  <span className="inline-flex items-center gap-1"><AlertTriangle className="h-4 w-4 text-slate-600" /> {item.substituidos} {t('volunteer.coverage.metrics.replacements')}</span>
                </div>

                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                  {item.equipes.map((equipe) => (
                    <div key={equipe.equipeId} className="rounded-lg border p-3">
                      <div className="font-medium">{equipe.equipeNome}</div>
                      <div className="mt-2 flex flex-wrap gap-2 text-xs text-muted-foreground">
                        <span>{equipe.totalVagas} {t('volunteer.coverage.teamMetrics.slots')}</span>
                        <span>{equipe.confirmados} {t('volunteer.coverage.teamMetrics.confirmed')}</span>
                        <span>{equipe.pendentes} {t('volunteer.coverage.teamMetrics.pending')}</span>
                        <span>{equipe.recusados} {t('volunteer.coverage.teamMetrics.declines')}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>

      {filtered.length > 0 && (
        <DataTablePagination
          page={page}
          pageSize={pageSize}
          total={total}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
        />
      )}
    </div>
  );
}
