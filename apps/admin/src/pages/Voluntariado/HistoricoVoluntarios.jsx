import { useEffect, useMemo, useState } from 'react';
import { RefreshCcw, ShieldAlert, UserCheck } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { equipesApi, escalasApi, eventosApi } from '@/lib/api';
import { usePagination } from '@/hooks/usePagination';
import { formatDateTime } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';

export default function HistoricoVoluntarios() {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [equipes, setEquipes] = useState([]);
  const [eventos, setEventos] = useState([]);
  const [registros, setRegistros] = useState([]);
  const [busca, setBusca] = useState('');
  const [equipeId, setEquipeId] = useState('all');
  const [eventoId, setEventoId] = useState('all');
  const [dataInicio, setDataInicio] = useState(() => {
    const d = new Date();
    d.setMonth(d.getMonth() - 6);
    return d.toISOString().slice(0, 10);
  });
  const [dataFim, setDataFim] = useState(() => {
    const d = new Date();
    d.setMonth(d.getMonth() + 1);
    return d.toISOString().slice(0, 10);
  });

  const load = async () => {
    try {
      setLoading(true);
      setError(null);

      const params = {
        equipeId: equipeId === 'all' ? undefined : Number(equipeId),
        eventoId: eventoId === 'all' ? undefined : Number(eventoId),
        dataInicio: `${dataInicio}T00:00:00`,
        dataFim: `${dataFim}T23:59:59`,
      };

      const [equipesRes, eventosRes, historicoRes] = await Promise.all([
        equipesApi.getAll(),
        eventosApi.getAll(),
        escalasApi.getHistoricoVoluntarios(params),
      ]);

      setEquipes(equipesRes.data || []);
      setEventos(eventosRes.data || []);
      setRegistros(historicoRes.data || []);
    } catch (err) {
      console.error(err);
      setError(t('volunteer.history.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [equipeId, eventoId, dataInicio, dataFim]);

  const resumo = useMemo(() => {
    return registros.reduce((acc, item) => {
      acc.voluntarios += 1;
      acc.presencas += item.presencas;
      acc.faltas += item.faltas;
      acc.pendentes += item.pendentes;
      return acc;
    }, {
      voluntarios: 0,
      presencas: 0,
      faltas: 0,
      pendentes: 0,
    });
  }, [registros]);

  const filtrados = useMemo(() => {
    const termo = busca.trim().toLowerCase();
    return registros.filter((item) => {
      if (!termo) return true;
      return item.voluntarioNome?.toLowerCase().includes(termo)
        || item.equipes?.join(', ').toLowerCase().includes(termo);
    });
  }, [registros, busca]);

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtrados, 15);

  if (loading) return <LoadingPage text={t('volunteer.history.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.history.title')}</h1>
          <p className="text-muted-foreground">{t('volunteer.history.subtitle')}</p>
        </div>
        <Button variant="outline" onClick={load}>
          <RefreshCcw className="h-4 w-4 mr-2" />
          {t('volunteer.history.refresh')}
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader><CardTitle>{t('volunteer.history.summary.volunteers')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.voluntarios}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.history.summary.presences')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-green-600">{resumo.presencas}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.history.summary.absences')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-red-600">{resumo.faltas}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.history.summary.pending')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-amber-600">{resumo.pendentes}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.history.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-5">
          <div className="space-y-2 md:col-span-2">
            <Label>{t('volunteer.history.searchLabel')}</Label>
            <Input value={busca} onChange={(e) => setBusca(e.target.value)} placeholder={t('volunteer.history.searchPlaceholder')} />
          </div>
          <div className="space-y-2">
            <Label>{t('volunteer.history.teamLabel')}</Label>
            <Select value={equipeId} onValueChange={setEquipeId}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="all">{t('volunteer.history.allTeamsOption')}</SelectItem>
                {equipes.map((equipe) => (
                  <SelectItem key={equipe.id} value={String(equipe.id)}>{equipe.nome}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>{t('volunteer.history.eventLabel')}</Label>
            <Select value={eventoId} onValueChange={setEventoId}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="all">{t('volunteer.history.allEventsOption')}</SelectItem>
                {eventos.map((evento) => (
                  <SelectItem key={evento.id} value={String(evento.id)}>{evento.titulo}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid gap-4 md:grid-cols-2 md:col-span-5">
            <div className="space-y-2">
              <Label>{t('volunteer.history.startDateLabel')}</Label>
              <Input type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.history.endDateLabel')}</Label>
              <Input type="date" value={dataFim} onChange={(e) => setDataFim(e.target.value)} />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.history.listTitle', { total })}</CardTitle>
        </CardHeader>
        <CardContent>
          {filtrados.length === 0 ? (
            <div className="py-10 text-center text-muted-foreground">{t('volunteer.history.empty')}</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('volunteer.history.table.volunteer')}</TableHead>
                  <TableHead>{t('volunteer.history.table.teams')}</TableHead>
                  <TableHead>{t('volunteer.history.table.total')}</TableHead>
                  <TableHead>{t('volunteer.history.table.presences')}</TableHead>
                  <TableHead>{t('volunteer.history.table.absences')}</TableHead>
                  <TableHead>{t('volunteer.history.table.pending')}</TableHead>
                  <TableHead>{t('volunteer.history.table.monthLoad')}</TableHead>
                  <TableHead>{t('volunteer.history.table.lastSchedule')}</TableHead>
                  <TableHead>{t('volunteer.history.table.nextSchedule')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((item) => (
                  <TableRow key={item.pessoaId}>
                    <TableCell className="font-medium">
                      <div className="flex items-center gap-2">
                        {item.faltas > 0 ? <ShieldAlert className="h-4 w-4 text-red-600" /> : <UserCheck className="h-4 w-4 text-green-600" />}
                        {item.voluntarioNome}
                      </div>
                    </TableCell>
                    <TableCell>{item.equipes?.join(', ') || '-'}</TableCell>
                    <TableCell>{item.totalEscalas}</TableCell>
                    <TableCell className="text-green-600">{item.presencas}</TableCell>
                    <TableCell className="text-red-600">{item.faltas}</TableCell>
                    <TableCell className="text-amber-600">{item.pendentes}</TableCell>
                    <TableCell>{item.cargaMesAtual}</TableCell>
                    <TableCell>{item.ultimaEscalaEm ? formatDateTime(item.ultimaEscalaEm) : '-'}</TableCell>
                    <TableCell>{item.proximaEscalaEm ? formatDateTime(item.proximaEscalaEm) : '-'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          {filtrados.length > 0 && (
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
