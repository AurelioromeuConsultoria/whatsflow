import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { AlertTriangle, ArrowLeft, CheckCircle2, Clock3, PlusCircle, Settings, XCircle } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { eventosOcorrenciasApi, escalasApi, equipesApi } from '@/lib/api';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';

function getEscalaStatusLabel(status) {
  const v = Number(status);
  if (v === 1) return 'draft';
  if (v === 2) return 'published';
  if (v === 3) return 'closed';
  return 'unknown';
}

function getItemStatusCounts(itens = []) {
  return itens.reduce((acc, item) => {
    const status = Number(item.status);
    if (status === 1) acc.pendentes += 1;
    if (status === 2) acc.confirmados += 1;
    if (status === 3) acc.recusados += 1;
    if (status === 4) acc.substituidos += 1;
    if (status === 6) acc.faltas += 1;
    return acc;
  }, {
    pendentes: 0,
    confirmados: 0,
    recusados: 0,
    substituidos: 0,
    faltas: 0,
  });
}

function getCoverageRisk(escala) {
  const counts = getItemStatusCounts(escala?.itens);
  const total = escala?.itens?.length || 0;

  if (!escala || total === 0) {
    return { labelKey: 'noCoverage', className: 'bg-red-100 text-red-800 hover:bg-red-100' };
  }

  if (counts.recusados > 0 || counts.faltas > 0) {
    return { labelKey: 'highRisk', className: 'bg-red-100 text-red-800 hover:bg-red-100' };
  }

  if (counts.pendentes > 0 || Number(escala.status) === 1) {
    return { labelKey: 'attention', className: 'bg-amber-100 text-amber-800 hover:bg-amber-100' };
  }

  return { labelKey: 'covered', className: 'bg-green-100 text-green-800 hover:bg-green-100' };
}

export default function EscalasPorOcorrencia() {
  const { ocorrenciaId } = useParams();
  const { can } = useAuth();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [ocorrencia, setOcorrencia] = useState(null);
  const [escalas, setEscalas] = useState([]);
  const [equipes, setEquipes] = useState([]);
  const [insightsByEquipe, setInsightsByEquipe] = useState({});
  const { t } = useTranslation();

  const canEdit = can(RESOURCES.VOLUNTARIOS, ACTIONS.EDIT);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const [ocRes, escRes, eqRes] = await Promise.all([
        eventosOcorrenciasApi.getById(ocorrenciaId),
        escalasApi.getAllByOcorrencia(ocorrenciaId),
        equipesApi.getAll(),
      ]);
      const escalasData = escRes.data || [];
      setOcorrencia(ocRes.data);
      setEscalas(escalasData);
      setEquipes(eqRes.data || []);

      const escalasComEquipe = escalasData.filter((escala) => escala?.id && escala?.equipeId);
      if (escalasComEquipe.length === 0) {
        setInsightsByEquipe({});
        return;
      }

      const insightsEntries = await Promise.all(
        escalasComEquipe.map(async (escala) => {
          const sugestoesRes = await escalasApi.getSugestoes(escala.id, escala.equipeId);
          const sugestoes = sugestoesRes.data || [];
          const sugestoesByVoluntarioId = new Map(sugestoes.map((item) => [item.voluntarioId, item]));

          const maisAcionados = (escala.itens || [])
            .map((item) => {
              const sugestao = sugestoesByVoluntarioId.get(item.voluntarioId);
              return {
                id: item.id,
                nome: item.voluntarioNome,
                cargoNome: item.cargoNome,
                cargaRecente: sugestao?.cargaRecente || 0,
              };
            })
            .sort((a, b) => b.cargaRecente - a.cargaRecente || a.nome.localeCompare(b.nome))
            .slice(0, 3);

          const atencaoPessoas = (escala.itens || [])
            .filter((item) => [1, 3, 6].includes(Number(item.status)))
            .map((item) => ({
              id: item.id,
              nome: item.voluntarioNome,
              cargoNome: item.cargoNome,
              status: Number(item.status),
              motivoRecusa: item.motivoRecusa,
            }));

          return [escala.equipeId, { maisAcionados, atencaoPessoas }];
        })
      );

      setInsightsByEquipe(Object.fromEntries(insightsEntries));
    } catch (err) {
      console.error(err);
      setError(t('volunteer.schedules.byOccurrence.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [ocorrenciaId]);

  const equipesComEscala = new Set(escalas?.map((e) => e.equipeId) || []);
  const equipesSemEscala = (equipes || []).filter((eq) => !equipesComEscala.has(eq.id));
  const resumo = useMemo(() => {
    const base = {
      equipesTotal: equipes.length,
      equipesComEscala: escalas.length,
      equipesSemEscala: equipesSemEscala.length,
      vagas: 0,
      confirmados: 0,
      pendentes: 0,
      recusados: 0,
    };

    for (const escala of escalas) {
      const counts = getItemStatusCounts(escala.itens);
      base.vagas += escala.itens?.length || 0;
      base.confirmados += counts.confirmados;
      base.pendentes += counts.pendentes;
      base.recusados += counts.recusados;
    }

    return base;
  }, [equipes.length, equipesSemEscala.length, escalas]);

  if (loading) return <LoadingPage text={t('volunteer.schedules.byOccurrence.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;
  if (!ocorrencia) return <ErrorPage message={t('volunteer.schedules.byOccurrence.notFound')} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" asChild>
          <Link to="/voluntariado/escalas">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.schedules.byTeamTitle')}</h1>
          <p className="text-muted-foreground">
            {ocorrencia.eventoTitulo} — {formatDateTime(ocorrencia.dataHoraInicio)}
          </p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-3 xl:grid-cols-6">
        <Card>
          <CardHeader><CardTitle>{t('volunteer.schedules.byOccurrence.summary.teams')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.equipesTotal}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.schedules.byOccurrence.summary.withSchedule')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.equipesComEscala}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.schedules.byOccurrence.summary.withoutSchedule')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-red-600">{resumo.equipesSemEscala}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.schedules.byOccurrence.summary.slots')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.vagas}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.schedules.byOccurrence.summary.pending')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-amber-600">{resumo.pendentes}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('volunteer.schedules.byOccurrence.summary.declines')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold text-red-600">{resumo.recusados}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.schedules.byOccurrence.teamsTitle')}</CardTitle>
          <p className="text-sm text-muted-foreground">
            {t('volunteer.schedules.byOccurrence.teamsDescription')}
          </p>
        </CardHeader>
        <CardContent className="space-y-4">
          {escalas?.length > 0 && (
            <>
              <h3 className="font-medium">{t('volunteer.schedules.byOccurrence.createdSchedules')}</h3>
              <ul className="space-y-2">
                {escalas.map((esc) => {
                  const counts = getItemStatusCounts(esc.itens);
                  const risk = getCoverageRisk(esc);

                  return (
                    <li key={esc.id} className="rounded-lg border p-4 space-y-3">
                      <div className="flex items-center justify-between gap-4">
                        <div className="space-y-1">
                          <div className="font-medium">{esc.equipeNome || t('volunteer.schedules.byOccurrence.teamFallback', { id: esc.equipeId })}</div>
                          <div className="flex flex-wrap items-center gap-2">
                            <Badge variant="outline">{t(`volunteer.schedules.byOccurrence.scheduleStatus.${getEscalaStatusLabel(esc.status)}`)}</Badge>
                            <Badge className={risk.className}>{t(`volunteer.schedules.byOccurrence.coverageRisk.${risk.labelKey}`)}</Badge>
                          </div>
                        </div>
                        {canEdit && (
                          <Button variant="outline" size="sm" asChild>
                            <Link
                              to={`/voluntariado/escalas/ocorrencia/${ocorrenciaId}/equipe/${esc.equipeId}`}
                              state={{
                                breadcrumbLabels: {
                                  [`/voluntariado/escalas/ocorrencia/${ocorrenciaId}`]: ocorrencia.eventoTitulo,
                                  [`/voluntariado/escalas/ocorrencia/${ocorrenciaId}/equipe/${esc.equipeId}`]: esc.equipeNome || t('volunteer.schedules.byOccurrence.teamFallback', { id: esc.equipeId }),
                                },
                              }}
                            >
                              <Settings className="h-4 w-4 mr-2" />
                              {t('volunteer.schedules.editAction')}
                            </Link>
                          </Button>
                        )}
                      </div>

                      <div className="grid gap-3 md:grid-cols-5">
                        <div className="rounded-md bg-muted/40 p-3">
                          <div className="text-xs text-muted-foreground">{t('volunteer.schedules.byOccurrence.metrics.slots')}</div>
                          <div className="text-lg font-semibold">{esc.itens?.length || 0}</div>
                        </div>
                        <div className="rounded-md bg-green-50 p-3">
                          <div className="flex items-center gap-2 text-xs text-green-700">
                            <CheckCircle2 className="h-3.5 w-3.5" />
                            {t('volunteer.schedules.byOccurrence.metrics.confirmed')}
                          </div>
                          <div className="text-lg font-semibold text-green-700">{counts.confirmados}</div>
                        </div>
                        <div className="rounded-md bg-amber-50 p-3">
                          <div className="flex items-center gap-2 text-xs text-amber-700">
                            <Clock3 className="h-3.5 w-3.5" />
                            {t('volunteer.schedules.byOccurrence.metrics.pending')}
                          </div>
                          <div className="text-lg font-semibold text-amber-700">{counts.pendentes}</div>
                        </div>
                        <div className="rounded-md bg-red-50 p-3">
                          <div className="flex items-center gap-2 text-xs text-red-700">
                            <XCircle className="h-3.5 w-3.5" />
                            {t('volunteer.schedules.byOccurrence.metrics.declines')}
                          </div>
                          <div className="text-lg font-semibold text-red-700">{counts.recusados}</div>
                        </div>
                        <div className="rounded-md bg-slate-50 p-3">
                          <div className="flex items-center gap-2 text-xs text-slate-700">
                            <AlertTriangle className="h-3.5 w-3.5" />
                            {t('volunteer.schedules.byOccurrence.metrics.replacements')}
                          </div>
                          <div className="text-lg font-semibold text-slate-700">{counts.substituidos}</div>
                        </div>
                      </div>

                      <div className="grid gap-3 lg:grid-cols-2">
                        <div className="rounded-md border bg-background p-3">
                          <div className="text-sm font-medium mb-2">{t('volunteer.schedules.byOccurrence.mostCalledTitle')}</div>
                          {insightsByEquipe[esc.equipeId]?.maisAcionados?.length ? (
                            <div className="space-y-2">
                              {insightsByEquipe[esc.equipeId].maisAcionados.map((item) => (
                                <div key={item.id} className="flex items-center justify-between gap-3 text-sm">
                                  <div>
                                    <div className="font-medium">{item.nome}</div>
                                    <div className="text-muted-foreground">{item.cargoNome || t('volunteer.schedules.byOccurrence.noRole')}</div>
                                  </div>
                                  <Badge variant="outline">{t('volunteer.schedules.byOccurrence.recentSchedules', { count: item.cargaRecente })}</Badge>
                                </div>
                              ))}
                            </div>
                          ) : (
                            <div className="text-sm text-muted-foreground">{t('volunteer.schedules.byOccurrence.noRecentLoadData')}</div>
                          )}
                        </div>

                        <div className="rounded-md border bg-background p-3">
                          <div className="text-sm font-medium mb-2">{t('volunteer.schedules.byOccurrence.attentionPeopleTitle')}</div>
                          {insightsByEquipe[esc.equipeId]?.atencaoPessoas?.length ? (
                            <div className="space-y-2">
                              {insightsByEquipe[esc.equipeId].atencaoPessoas.map((item) => (
                                <div key={item.id} className="rounded-md bg-muted/40 p-2 text-sm">
                                  <div className="flex items-center justify-between gap-3">
                                    <div>
                                      <div className="font-medium">{item.nome}</div>
                                      <div className="text-muted-foreground">{item.cargoNome || t('volunteer.schedules.byOccurrence.noRole')}</div>
                                    </div>
                                    <Badge
                                      className={
                                        item.status === 3 || item.status === 6
                                          ? 'bg-red-100 text-red-800 hover:bg-red-100'
                                          : 'bg-amber-100 text-amber-800 hover:bg-amber-100'
                                      }
                                    >
                                      {item.status === 3
                                        ? t('volunteer.schedules.byOccurrence.attentionStatus.declined')
                                        : item.status === 6
                                          ? t('volunteer.schedules.byOccurrence.attentionStatus.missed')
                                          : t('volunteer.schedules.byOccurrence.attentionStatus.pending')}
                                    </Badge>
                                  </div>
                                  {item.motivoRecusa && (
                                    <div className="mt-1 text-xs text-red-700">{t('volunteer.schedules.byOccurrence.reasonLabel', { reason: item.motivoRecusa })}</div>
                                  )}
                                </div>
                              ))}
                            </div>
                          ) : (
                            <div className="text-sm text-muted-foreground">{t('volunteer.schedules.byOccurrence.noCriticalPending')}</div>
                          )}
                        </div>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </>
          )}
          {equipesSemEscala?.length > 0 && canEdit && (
            <>
              <h3 className="font-medium pt-2">{t('volunteer.schedules.byOccurrence.createForTeamTitle')}</h3>
              <ul className="space-y-2">
                {equipesSemEscala.map((eq) => (
                  <li key={eq.id} className="flex items-center justify-between rounded-lg border border-dashed p-3">
                    <span>{eq.nome}</span>
                    <Button size="sm" asChild>
                      <Link to={`/voluntariado/escalas/ocorrencia/${ocorrenciaId}/equipe/${eq.id}`}>
                        <PlusCircle className="h-4 w-4 mr-2" />
                        {t('volunteer.schedules.byOccurrence.createSchedule')}
                      </Link>
                    </Button>
                  </li>
                ))}
              </ul>
            </>
          )}
          {(!escalas || escalas.length === 0) && (!equipesSemEscala || equipesSemEscala.length === 0) && (
            <p className="text-muted-foreground">{t('volunteer.schedules.byOccurrence.noTeamsRegistered')}</p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
