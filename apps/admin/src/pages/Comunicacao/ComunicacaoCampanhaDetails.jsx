import React, { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Play, Pause, Square, RefreshCw, Mail, MessageSquare, AlertTriangle, CheckCircle2, RotateCcw, Save } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageRefreshButton } from '@/components/ui/page-state';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { comunicacaoCampanhasApi, comunicacaoEntregasApi, comunicacaoTemplatesApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';
import { toast } from 'sonner';

const getStatusLabel = (status, t) => {
  switch (Number(status)) {
    case 1: return t('communicationCampaignDetails.status.draft');
    case 2: return t('communicationCampaignDetails.status.scheduled');
    case 3: return t('communicationCampaignDetails.status.processing');
    case 4: return t('communicationCampaignDetails.status.completed');
    case 5: return t('communicationCampaignDetails.status.withFailures');
    case 6: return t('communicationCampaignDetails.status.canceled');
    default: return t('communicationCampaignDetails.status.fallback', { status });
  }
};

const getEntregaStatus = (status, t) => {
  switch (Number(status)) {
    case 1: return t('communicationCampaignDetails.deliveryStatus.pending');
    case 2: return t('communicationCampaignDetails.deliveryStatus.reserved');
    case 3: return t('communicationCampaignDetails.deliveryStatus.sent');
    case 4: return t('communicationCampaignDetails.deliveryStatus.delivered');
    case 5: return t('communicationCampaignDetails.deliveryStatus.failed');
    case 6: return t('communicationCampaignDetails.deliveryStatus.canceled');
    case 7: return t('communicationCampaignDetails.deliveryStatus.ignored');
    default: return t('communicationCampaignDetails.deliveryStatus.fallback', { status });
  }
};

const getCanalLabel = (canal, t) => {
  switch (Number(canal)) {
    case 1: return t('communicationCampaignDetails.channels.whatsapp');
    case 2: return t('communicationCampaignDetails.channels.email');
    case 3: return t('communicationCampaignDetails.channels.push');
    case 4: return t('communicationCampaignDetails.channels.internalNotification');
    default: return t('communicationCampaignDetails.channels.fallback', { canal });
  }
};

const getCanalIcon = (canal) => {
  switch (Number(canal)) {
    case 1: return <MessageSquare className="w-4 h-4" />;
    case 2: return <Mail className="w-4 h-4" />;
    default: return <MessageSquare className="w-4 h-4" />;
  }
};

export default function ComunicacaoCampanhaDetails() {
  const { t } = useTranslation();
  const { id } = useParams();
  const [campanha, setCampanha] = useState(null);
  const [entregas, setEntregas] = useState([]);
  const [templates, setTemplates] = useState([]);
  const [savingTemplateCanal, setSavingTemplateCanal] = useState(null);
  const [statusFilter, setStatusFilter] = useState('todos');
  const [canalFilter, setCanalFilter] = useState('todos');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [savingSchedule, setSavingSchedule] = useState(false);
  const [scheduleDraft, setScheduleDraft] = useState('');
  const [error, setError] = useState(null);

  const toDateTimeInputValue = (value) => {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const offsetMs = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
  };

  const toApiDateTimeValue = (value) => (value ? `${value}:00` : null);

  const buildUpdatePayload = (campanhaAtual, dataAgendamento) => ({
    nome: campanhaAtual.nome,
    objetivo: campanhaAtual.objetivo,
    publicoAlvo: campanhaAtual.publicoAlvo,
    dataAgendamento,
    status: dataAgendamento ? 2 : campanhaAtual.status,
    canais: (campanhaAtual.canais || []).map((canal) => ({
      canal: canal.canal,
      templateId: canal.templateId,
      prioridade: canal.prioridade,
    })),
  });

  const load = useCallback(async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);

      const [campanhaResponse, entregasResponse] = await Promise.all([
        comunicacaoCampanhasApi.getById(id),
        comunicacaoCampanhasApi.getEntregas(id),
      ]);

      setCampanha(campanhaResponse.data || null);
      setScheduleDraft(toDateTimeInputValue(campanhaResponse.data?.dataAgendamento));
      setEntregas(entregasResponse.data || []);

      try {
        const templatesResponse = await comunicacaoTemplatesApi.getAll();
        setTemplates(templatesResponse.data || []);
      } catch {
        setTemplates([]);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, t('communicationCampaignDetails.errorLoad')));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [id, t]);

  useEffect(() => {
    load();
  }, [load]);

  const processarPendentes = async () => {
    try {
      setProcessing(true);
      const response = await comunicacaoEntregasApi.processarPendentes(100);
      toast.success(t('communicationCampaignDetails.processPendingSuccess', { count: response.data?.processadas ?? 0 }));
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationCampaignDetails.processPendingError')));
    } finally {
      setProcessing(false);
    }
  };

  const salvarAgendamento = async () => {
    if (!campanha) return;

    try {
      setSavingSchedule(true);
      const dataAgendamento = toApiDateTimeValue(scheduleDraft);
      const response = await comunicacaoCampanhasApi.update(
        campanha.id,
        buildUpdatePayload(campanha, dataAgendamento)
      );
      setCampanha(response.data || campanha);
      toast.success('Agendamento atualizado.');
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao atualizar agendamento.'));
    } finally {
      setSavingSchedule(false);
    }
  };

  const processarCampanhaAgora = async () => {
    if (!campanha) return;

    try {
      setProcessing(true);
      const now = new Date();
      const offsetMs = now.getTimezoneOffset() * 60000;
      const nowLocal = new Date(now.getTime() - offsetMs).toISOString().slice(0, 19);

      await comunicacaoCampanhasApi.update(
        campanha.id,
        buildUpdatePayload(campanha, nowLocal)
      );

      const response = await comunicacaoEntregasApi.processarPendentes(100);
      toast.success(`${response.data?.processadas ?? 0} entrega(s) processada(s).`);
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao processar campanha agora.'));
    } finally {
      setProcessing(false);
    }
  };

  const [lifecycleBusy, setLifecycleBusy] = useState(false);

  const executarAcaoCiclo = async (acao, label) => {
    if (!campanha) return;
    try {
      setLifecycleBusy(true);
      const response = await comunicacaoCampanhasApi[acao](campanha.id);
      if (response.data) setCampanha(response.data);
      toast.success(`Campanha: ${label}.`);
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, `Erro ao executar ação (${label}).`));
    } finally {
      setLifecycleBusy(false);
    }
  };

  const reprocessarEntrega = async (entregaId) => {
    try {
      const response = await comunicacaoEntregasApi.reprocessar(entregaId);
      const entregaAtualizada = response.data;
      setEntregas((prev) => prev.map((item) => (item.id === entregaId ? entregaAtualizada : item)));
      toast.success(t('communicationCampaignDetails.reprocessSuccess'));
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationCampaignDetails.reprocessError')));
    }
  };

  const alterarTemplateCanal = async (canalNumero, templateIdRaw) => {
    if (!campanha) return;

    const templateId = templateIdRaw ? Number(templateIdRaw) : null;
    const canaisAtualizados = (campanha.canais || []).map((canal) => ({
      canal: canal.canal,
      templateId: Number(canal.canal) === Number(canalNumero) ? templateId : canal.templateId,
      prioridade: canal.prioridade,
    }));

    try {
      setSavingTemplateCanal(canalNumero);
      const response = await comunicacaoCampanhasApi.update(campanha.id, {
        nome: campanha.nome,
        objetivo: campanha.objetivo,
        publicoAlvo: campanha.publicoAlvo,
        dataAgendamento: campanha.dataAgendamento,
        status: campanha.status,
        canais: canaisAtualizados,
      });
      setCampanha(response.data || campanha);
      toast.success(t('communicationCampaignDetails.channelsCard.templateUpdated'));
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationCampaignDetails.channelsCard.templateUpdateError')));
    } finally {
      setSavingTemplateCanal(null);
    }
  };

  if (loading) return <LoadingPage text={t('communicationCampaignDetails.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;
  if (!campanha) return <ErrorPage message={t('communicationCampaignDetails.notFound')} onRetry={load} />;

  const falhas = entregas.filter((entrega) => Number(entrega.status) === 5);
  const ignoradas = entregas.filter((entrega) => Number(entrega.status) === 7);
  const sucessos = entregas.filter((entrega) => [3, 4].includes(Number(entrega.status)));
  const entregasFiltradas = entregas.filter((entrega) => {
    const statusOk = statusFilter === 'todos' || String(entrega.status) === statusFilter;
    const canalOk = canalFilter === 'todos' || String(entrega.canal) === canalFilter;
    return statusOk && canalOk;
  });
  const resumoPorCanal = (campanha.canais || []).map((canal) => {
    const doCanal = entregas.filter((item) => Number(item.canal) === Number(canal.canal));
    const falhasCanal = doCanal.filter((item) => Number(item.status) === 5).length;
    const sucessosCanal = doCanal.filter((item) => [3, 4].includes(Number(item.status))).length;

    return {
      ...canal,
      total: doCanal.length,
      falhas: falhasCanal,
      sucessos: sucessosCanal,
      // Fonte de verdade: diagnóstico do IComunicacaoCanalProvider (conta ativa), vindo no DTO.
      diagnostico: canal.diagnostico || null,
      configOk: canal.configurado !== false,
    };
  });

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <Button variant="ghost" size="sm" asChild>
            <Link to="/comunicacao/campanhas">
              <ArrowLeft className="w-4 h-4" />
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold text-foreground">{campanha.nome}</h1>
            <p className="text-muted-foreground mt-1">{campanha.objetivo} • {campanha.publicoAlvo}</p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          {[1, 2].includes(Number(campanha.status)) && (
            <Button variant="outline" onClick={() => executarAcaoCiclo('iniciar', 'iniciada')} disabled={lifecycleBusy}>
              <Play className="w-4 h-4 mr-2" /> Iniciar
            </Button>
          )}
          {Number(campanha.status) === 3 && (
            <Button variant="outline" onClick={() => executarAcaoCiclo('pausar', 'pausada')} disabled={lifecycleBusy}>
              <Pause className="w-4 h-4 mr-2" /> Pausar
            </Button>
          )}
          {[2, 7].includes(Number(campanha.status)) && (
            <Button variant="outline" onClick={() => executarAcaoCiclo('retomar', 'retomada')} disabled={lifecycleBusy}>
              <RefreshCw className="w-4 h-4 mr-2" /> Retomar
            </Button>
          )}
          {![4, 6].includes(Number(campanha.status)) && (
            <Button variant="outline" onClick={() => executarAcaoCiclo('cancelar', 'cancelada')} disabled={lifecycleBusy}>
              <Square className="w-4 h-4 mr-2" /> Cancelar
            </Button>
          )}
          <Button onClick={processarPendentes} disabled={processing}>
            <Play className="w-4 h-4 mr-2" />
            {processing ? t('communicationCampaignDetails.actions.processing') : t('communicationCampaignDetails.actions.processPending')}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card><CardContent className="p-5"><div className="text-sm text-muted-foreground">{t('communicationCampaignDetails.cards.status')}</div><div className="text-lg font-semibold mt-1">{getStatusLabel(campanha.status, t)}</div></CardContent></Card>
        <Card><CardContent className="p-5"><div className="text-sm text-muted-foreground">{t('communicationCampaignDetails.cards.deliveries')}</div><div className="text-2xl font-bold mt-1">{campanha.totalEntregas}</div></CardContent></Card>
        <Card><CardContent className="p-5"><div className="text-sm text-muted-foreground">{t('communicationCampaignDetails.cards.failures')}</div><div className="text-2xl font-bold mt-1">{campanha.totalFalhas}</div></CardContent></Card>
        <Card><CardContent className="p-5"><div className="text-sm text-muted-foreground">{t('communicationCampaignDetails.cards.scheduling')}</div><div className="text-sm font-medium mt-1">{campanha.dataAgendamento ? formatDateTime(campanha.dataAgendamento) : t('communicationCampaignDetails.cards.immediateDraft')}</div></CardContent></Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Agendamento</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col gap-3 md:flex-row md:items-end">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground" htmlFor="campanha-data-agendamento">
                Data e hora de envio
              </label>
              <input
                id="campanha-data-agendamento"
                type="datetime-local"
                value={scheduleDraft}
                onChange={(event) => setScheduleDraft(event.target.value)}
                className="h-10 rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground outline-none ring-offset-background focus-visible:ring-2 focus-visible:ring-ring"
              />
            </div>
            <Button onClick={salvarAgendamento} disabled={savingSchedule}>
              <Save className="w-4 h-4 mr-2" />
              {savingSchedule ? 'Salvando...' : 'Salvar agendamento'}
            </Button>
            <Button variant="outline" onClick={processarCampanhaAgora} disabled={processing}>
              <Play className="w-4 h-4 mr-2" />
              Processar esta campanha agora
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('communicationCampaignDetails.channelsCard.title')}</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-3">
          {resumoPorCanal.map((canal) => (
            <div key={`${canal.canal}-${canal.prioridade}`} className="rounded-lg border border-border p-4 flex items-start justify-between gap-3">
              <div className="flex items-start gap-2 flex-1 min-w-0">
                {getCanalIcon(canal.canal)}
                <div className="flex-1 min-w-0 space-y-1">
                  <div className="font-medium">{getCanalLabel(canal.canal, t)}</div>
                  <select
                    value={canal.templateId ?? ''}
                    disabled={savingTemplateCanal !== null}
                    onChange={(event) => alterarTemplateCanal(canal.canal, event.target.value)}
                    className="w-full rounded-md border border-input bg-background px-2 py-1 text-sm text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-60"
                  >
                    <option value="">{t('communicationCampaignDetails.channelsCard.noLinkedTemplate')}</option>
                    {templates
                      .filter((template) => Number(template.canal) === Number(canal.canal))
                      .map((template) => (
                        <option key={template.id} value={template.id}>{template.nome}</option>
                      ))}
                  </select>
                  <div className="text-xs text-muted-foreground">
                    {canal.diagnostico
                      || (canal.configOk
                        ? t('communicationCampaignDetails.channelsCard.ready')
                        : t('communicationCampaignDetails.channelsCard.noDetailedDiagnosis'))}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {t('communicationCampaignDetails.channelsCard.summary', { success: canal.sucessos, failures: canal.falhas })}
                  </div>
                </div>
              </div>
              <div className="flex flex-col items-end gap-2">
                <Badge variant="secondary">{t('communicationCampaignDetails.channelsCard.priority', { priority: canal.prioridade })}</Badge>
                <Badge variant={canal.configOk ? 'outline' : 'destructive'}>
                  {canal.configOk ? t('communicationCampaignDetails.channelsCard.deliveries', { count: canal.total }) : t('communicationCampaignDetails.channelsCard.configurationPending')}
                </Badge>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      {falhas.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>{t('communicationCampaignDetails.recentFailures.title')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {falhas.slice(0, 10).map((entrega) => (
              <div key={`falha-${entrega.id}`} className="rounded-lg border border-red-200 bg-red-50/50 p-4">
                <div className="flex items-center justify-between gap-3">
                  <div className="font-medium">{getCanalLabel(entrega.canal, t)}</div>
                  <Badge variant="destructive">{getEntregaStatus(entrega.status, t)}</Badge>
                </div>
                <div className="text-sm text-muted-foreground mt-1">{entrega.destinoResolvido}</div>
                <div className="text-sm mt-2 whitespace-pre-wrap">{entrega.erro || t('communicationCampaignDetails.recentFailures.noDetailedMessage')}</div>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle>{t('communicationCampaignDetails.deliveriesTable.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap items-center gap-2 mb-4">
            <Button
              type="button"
              size="sm"
              variant={statusFilter === 'todos' ? 'default' : 'outline'}
              onClick={() => setStatusFilter('todos')}
            >
              {t('communicationCampaignDetails.filters.all')}
            </Button>
            <Button
              type="button"
              size="sm"
              variant={statusFilter === '5' ? 'destructive' : 'outline'}
              onClick={() => setStatusFilter('5')}
            >
              {t('communicationCampaignDetails.filters.failures')}
            </Button>
            <Button
              type="button"
              size="sm"
              variant={statusFilter === '1' ? 'secondary' : 'outline'}
              onClick={() => setStatusFilter('1')}
            >
              {t('communicationCampaignDetails.filters.pending')}
            </Button>
            <Button
              type="button"
              size="sm"
              variant={statusFilter === '3' ? 'secondary' : 'outline'}
              onClick={() => setStatusFilter('3')}
            >
              {t('communicationCampaignDetails.filters.sent')}
            </Button>
            <Button
              type="button"
              size="sm"
              variant={statusFilter === '7' ? 'secondary' : 'outline'}
              onClick={() => setStatusFilter('7')}
            >
              {t('communicationCampaignDetails.filters.ignored')}
            </Button>

            <div className="h-6 w-px bg-border mx-1" />

            <Button
              type="button"
              size="sm"
              variant={canalFilter === 'todos' ? 'default' : 'outline'}
              onClick={() => setCanalFilter('todos')}
            >
              {t('communicationCampaignDetails.filters.allChannels')}
            </Button>
            {(campanha.canais || []).map((canal) => (
              <Button
                key={`filter-${canal.canal}`}
                type="button"
                size="sm"
                variant={canalFilter === String(canal.canal) ? 'secondary' : 'outline'}
                onClick={() => setCanalFilter(String(canal.canal))}
              >
                {getCanalLabel(canal.canal, t)}
              </Button>
            ))}
          </div>

          <div className="flex flex-wrap gap-2 mb-4">
            <Badge variant="outline">{t('communicationCampaignDetails.summary.success', { count: sucessos.length })}</Badge>
            <Badge variant="destructive">{t('communicationCampaignDetails.summary.failures', { count: falhas.length })}</Badge>
            <Badge variant="secondary">{t('communicationCampaignDetails.summary.ignored', { count: ignoradas.length })}</Badge>
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('communicationCampaignDetails.deliveriesTable.channel')}</TableHead>
                <TableHead>{t('communicationCampaignDetails.deliveriesTable.destination')}</TableHead>
                <TableHead>{t('communicationCampaignDetails.deliveriesTable.status')}</TableHead>
                <TableHead>{t('communicationCampaignDetails.deliveriesTable.attempts')}</TableHead>
                <TableHead>{t('communicationCampaignDetails.deliveriesTable.error')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {entregasFiltradas.map((entrega) => (
                <TableRow key={entrega.id}>
                  <TableCell>{getCanalLabel(entrega.canal, t)}</TableCell>
                  <TableCell>{entrega.destinoResolvido}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      {Number(entrega.status) === 5 ? <AlertTriangle className="w-4 h-4 text-red-500" /> : <CheckCircle2 className="w-4 h-4 text-green-500" />}
                      <span>{getEntregaStatus(entrega.status, t)}</span>
                    </div>
                  </TableCell>
                  <TableCell>{entrega.tentativas}</TableCell>
                  <TableCell className="whitespace-normal">
                    <div className="space-y-2">
                      <div>{entrega.erro || '-'}</div>
                      {entrega.podeReprocessar && (
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={() => reprocessarEntrega(entrega.id)}
                        >
                          <RotateCcw className="w-4 h-4 mr-2" />
                          {t('communicationCampaignDetails.actions.reprocess')}
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
              {entregasFiltradas.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                    {t('communicationCampaignDetails.deliveriesTable.empty')}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
