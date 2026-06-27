import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Plus, Trash2, Send, Wand2, CheckCircle2, XCircle, UserCheck, UserX } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { PromptDialog } from '@/components/ui/prompt-dialog';
import { usePromptDialog } from '@/hooks/usePromptDialog';
import { equipesApi, escalasApi, escalasModelosApi, eventosOcorrenciasApi, solicitacoesTrocasEscalasApi, voluntariosApi } from '@/lib/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';

function getEscalaStatusLabel(status) {
  const value = Number(status);
  if (value === 1) return 'draft';
  if (value === 2) return 'published';
  if (value === 3) return 'closed';
  return 'unknown';
}

function getEscalaStatusClassName(status) {
  const value = Number(status);
  if (value === 1) return 'border border-amber-500/40 bg-amber-500/15 text-amber-200';
  if (value === 2) return 'border border-emerald-500/40 bg-emerald-500/15 text-emerald-200';
  if (value === 3) return 'border border-slate-500/40 bg-slate-500/15 text-slate-200';
  return 'border border-muted bg-muted/20 text-muted-foreground';
}

function getEscalaItemStatusLabel(status) {
  const value = Number(status);
  if (value === 1) return 'pending';
  if (value === 2) return 'confirmed';
  if (value === 3) return 'declined';
  if (value === 4) return 'replaced';
  if (value === 5) return 'served';
  if (value === 6) return 'missed';
  return 'unknown';
}

function getEscalaItemStatusClassName(status) {
  const value = Number(status);
  if (value === 1) return 'border-amber-500/40 bg-amber-500/15 text-amber-200';
  if (value === 2) return 'border-emerald-500/40 bg-emerald-500/15 text-emerald-200';
  if (value === 3) return 'border-rose-500/40 bg-rose-500/15 text-rose-200';
  if (value === 4) return 'border-slate-500/40 bg-slate-500/15 text-slate-200';
  if (value === 5) return 'border-sky-500/40 bg-sky-500/15 text-sky-200';
  if (value === 6) return 'border-amber-500/50 bg-amber-400/20 text-amber-100';
  return 'border-muted bg-muted/20 text-muted-foreground';
}

function getActionButtonProps(item, action) {
  const status = Number(item.status);

  if (action === 'confirmar') {
    return status === 2
      ? {
          labelKey: 'confirmed',
          className: '!border-emerald-600 !bg-emerald-600 !text-white hover:!bg-emerald-700 hover:!text-white',
        }
      : {
          labelKey: 'confirm',
          className: '',
        };
  }

  if (action === 'recusar') {
    return status === 3
      ? {
          labelKey: 'declined',
          className: '!border-rose-600 !bg-rose-600 !text-white hover:!bg-rose-700 hover:!text-white',
        }
      : {
          labelKey: 'decline',
          className: '',
        };
  }

  if (action === 'serviu') {
    return status === 5
      ? {
          labelKey: 'served',
          className: '!border-sky-600 !bg-sky-600 !text-white hover:!bg-sky-700 hover:!text-white',
        }
      : {
          labelKey: 'served',
          className: '',
        };
  }

  if (action === 'faltou') {
    return status === 6
      ? {
          labelKey: 'missed',
          className: '!border-amber-500 !bg-amber-400 !text-black hover:!bg-amber-400 hover:!text-black',
        }
      : {
          labelKey: 'missed',
          className: '',
        };
  }

  return {
    labelKey: '',
    className: '',
  };
}

export default function EscalaEditor() {
  const { ocorrenciaId, equipeId } = useParams();
  const { isAdmin } = useAuth();
  const confirmDialog = useConfirmDialog();
  const promptDialog = usePromptDialog();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  const [ocorrencia, setOcorrencia] = useState(null);
  const [escala, setEscala] = useState(null);
  const [equipes, setEquipes] = useState([]);
  const [voluntarios, setVoluntarios] = useState([]);
  const [sugestoes, setSugestoes] = useState([]);
  const [solicitacoesTroca, setSolicitacoesTroca] = useState([]);
  const [modeloEscala, setModeloEscala] = useState(null);
  const [gerandoAuto, setGerandoAuto] = useState(false);
  const [trocaDialogOpen, setTrocaDialogOpen] = useState(false);
  const [trocaSelecionada, setTrocaSelecionada] = useState(null);
  const [substitutosDisponiveis, setSubstitutosDisponiveis] = useState([]);
  const [substitutoSelecionado, setSubstitutoSelecionado] = useState('');

  const { t } = useTranslation();
  const escalaRascunho = escala && Number(escala.status) === 1;

  const voluntariosElegiveis = useMemo(() => {
    const idsJaEscalados = new Set((escala?.itens || []).map((item) => Number(item.voluntarioId)));

    if (sugestoes.length > 0) {
      return sugestoes
        .filter((s) => Number(s.equipeId) === Number(equipeId))
        .map((s) => ({
        id: s.voluntarioId,
        nome: s.voluntarioNome,
        equipeId: s.equipeId,
        cargoId: s.cargoId,
        cargoNome: s.cargoNome,
        disponivel: s.disponivel,
        cargaRecente: s.cargaRecente,
        motivoBloqueio: s.motivoBloqueio,
      }));
    }

    return voluntarios
      .filter((v) => String(v.equipeId) === String(equipeId))
      .map((v) => ({
        ...v,
        disponivel: !idsJaEscalados.has(Number(v.id)),
        cargaRecente: 0,
        motivoBloqueio: idsJaEscalados.has(Number(v.id)) ? 'Já está na escala' : null,
      }));
  }, [voluntarios, sugestoes, escala?.itens, equipeId]);

  const voluntariosDisponiveis = useMemo(
    () => voluntariosElegiveis.filter((item) => item.disponivel && !(escala?.itens || []).some((escalaItem) => Number(escalaItem.voluntarioId) === Number(item.id))),
    [voluntariosElegiveis, escala?.itens]
  );

  const voluntariosBloqueados = useMemo(
    () => voluntariosElegiveis.filter((item) => !item.disponivel),
    [voluntariosElegiveis]
  );

  const coberturaModelo = useMemo(() => {
    if (!modeloEscala?.itens?.length) return [];

    const itensEscala = escala?.itens || [];

    return modeloEscala.itens.map((itemModelo) => {
      const preenchidos = itensEscala.filter((itemEscala) => {
        if (itemModelo.cargoId == null) return itemEscala.cargoId == null;
        return Number(itemEscala.cargoId) === Number(itemModelo.cargoId);
      }).length;

      const necessario = Number(itemModelo.quantidade || 0);
      const faltando = Math.max(0, necessario - preenchidos);

      return {
        id: itemModelo.id,
        cargoNome: itemModelo.cargoNome || 'Sem cargo definido',
        necessario,
        preenchidos,
        faltando,
        completo: faltando === 0,
      };
    });
  }, [modeloEscala, escala?.itens]);

  const resumoEscala = useMemo(() => {
    const itens = escala?.itens || [];
    return itens.reduce((acc, item) => {
      acc.total += 1;
      const status = Number(item.status);
      if (status === 1) acc.pendentes += 1;
      if (status === 2) acc.confirmados += 1;
      if (status === 3) acc.recusados += 1;
      if (status === 5) acc.serviram += 1;
      if (status === 6) acc.faltaram += 1;
      return acc;
    }, { total: 0, pendentes: 0, confirmados: 0, recusados: 0, serviram: 0, faltaram: 0 });
  }, [escala?.itens]);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);

      const [ocorrenciaRes, equipesRes, voluntariosRes] = await Promise.all([
        eventosOcorrenciasApi.getById(ocorrenciaId),
        equipesApi.getAll(),
        voluntariosApi.getAll(),
      ]);

      const ocorrenciaData = ocorrenciaRes.data;
      setOcorrencia(ocorrenciaData);
      setEquipes(equipesRes.data || []);
      setVoluntarios(voluntariosRes.data || []);

      try {
        const modeloRes = await escalasModelosApi.getByEventoAndEquipe(ocorrenciaData.eventoId, Number(equipeId));
        setModeloEscala(modeloRes.data || null);
      } catch (errModelo) {
        if (errModelo.response?.status === 404) {
          setModeloEscala(null);
        } else {
          console.error(t('volunteer.schedules.editor.logs.errorLoadModel'), errModelo);
          setModeloEscala(null);
        }
      }

      try {
        const escalaRes = await escalasApi.getByOcorrenciaAndEquipe(ocorrenciaId, equipeId);
        setEscala(escalaRes.data);
        const solicitacoesRes = await solicitacoesTrocasEscalasApi.getByEscala(escalaRes.data.id);
        setSolicitacoesTroca(solicitacoesRes.data || []);
      } catch (errEscala) {
        if (errEscala.response?.status === 404) {
          setEscala(null);
          setSolicitacoesTroca([]);
        } else {
          throw errEscala;
        }
      }
    } catch (err) {
      console.error(err);
      setError(t('volunteer.schedules.editor.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [ocorrenciaId, equipeId]);

  useEffect(() => {
    const carregarSugestoes = async () => {
      if (!escala?.id || !equipeId) {
        setSugestoes([]);
        return;
      }
      try {
        const res = await escalasApi.getSugestoes(escala.id, Number(equipeId));
        setSugestoes(res.data || []);
      } catch (err) {
        console.error(t('volunteer.schedules.editor.logs.errorLoadSuggestions'), err);
        setSugestoes([]);
      }
    };

    carregarSugestoes();
  }, [escala?.id, escala?.itens?.length, equipeId]);

  const ensureEscala = async () => {
    if (escala) return escala;

    const created = await escalasApi.create({
      eventoOcorrenciaId: Number(ocorrenciaId),
      equipeId: Number(equipeId),
      observacoes: null,
    });
    setEscala(created.data);
    toast.success(t('volunteer.schedules.editor.createSuccess'));
    return created.data;
  };

  const handleAddVoluntario = async (voluntario, forcarConflito = false) => {
    try {
      setSaving(true);
      const escalaAtual = await ensureEscala();
      let motivoExcecao = null;

      if (forcarConflito) {
        motivoExcecao = (await promptDialog.prompt({
          title: t('volunteer.schedules.editor.prompts.exceptionReasonTitle'),
          label: t('volunteer.schedules.editor.prompts.exceptionReason', { name: voluntario.nome }),
          confirmText: t('confirmDialog.confirm'),
          cancelText: t('actions.cancel'),
          required: true,
        }))?.trim();
        if (!motivoExcecao) {
          return;
        }
      }

      await escalasApi.addItem(escalaAtual.id, {
        equipeId: Number(equipeId),
        cargoId: voluntario?.cargoId ? Number(voluntario.cargoId) : null,
        voluntarioId: Number(voluntario.id),
        ordem: 0,
        forcarConflito,
        motivoExcecao,
      });

      await load();
      toast.success(forcarConflito
        ? t('volunteer.schedules.editor.addVolunteerExceptionSuccess')
        : t('volunteer.schedules.editor.addVolunteerSuccess'));
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorAddItem'));
      toast.error(message);
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteItem = async (item) => {
    confirmDialog.show({
      title: t('volunteer.schedules.editor.removeDialogTitle'),
      description: t('volunteer.schedules.editor.removeDialogDescription', {
        volunteer: item.voluntarioNome,
        team: item.equipeNome,
      }),
      confirmText: t('actions.remove'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await escalasApi.deleteItem(escala.id, item.id);
          toast.success(t('volunteer.schedules.editor.removeSuccess'));
          await load();
        } catch (err) {
          console.error(err);
          toast.error(t('volunteer.schedules.editor.errorRemoveItem'));
          throw err;
        }
      },
    });
  };

  const handleGerarAutomatico = async () => {
    try {
      setGerandoAuto(true);
      await escalasApi.gerarAutomatico(ocorrenciaId, equipeId);
      toast.success(t('volunteer.schedules.editor.autoFillSuccess'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorAutoFill'));
      toast.error(message);
    } finally {
      setGerandoAuto(false);
    }
  };

  const handlePublicar = async () => {
    if (!escala) return;
    try {
      await escalasApi.publicar(escala.id);
      toast.success(t('volunteer.schedules.editor.publishSuccess'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorPublish'));
      toast.error(message);
    }
  };

  const handleConfirmarItem = async (item) => {
    if (!escala) return;
    try {
      await escalasApi.confirmarItem(escala.id, item.id);
      toast.success(t('volunteer.schedules.editor.itemConfirmed'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorConfirmItem'));
      toast.error(message);
    }
  };

  const handleRecusarItem = async (item) => {
    if (!escala) return;

    const motivoRecusa = await promptDialog.prompt({
      title: t('volunteer.schedules.editor.prompts.declineReasonTitle'),
      label: t('volunteer.schedules.editor.prompts.declineReason', { name: item.voluntarioNome }),
      description: t('volunteer.schedules.editor.prompts.optionalHint'),
      defaultValue: item.motivoRecusa || '',
      confirmText: t('confirmDialog.confirm'),
      cancelText: t('actions.cancel'),
    });
    if (motivoRecusa === null) return;

    try {
      await escalasApi.recusarItem(escala.id, item.id, { motivoRecusa });
      toast.success(t('volunteer.schedules.editor.itemDeclined'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorDeclineItem'));
      toast.error(message);
    }
  };

  const handleAprovarTroca = async (solicitacao) => {
    if (!escala) return;

    const res = await escalasApi.getSugestoes(escala.id, Number(equipeId));
    const disponiveis = (res.data || []).filter((x) => x.disponivel && x.voluntarioId !== solicitacao.voluntarioSolicitanteId);
    if (!disponiveis.length) {
      toast.error(t('volunteer.schedules.editor.noSubstituteAvailable'));
      return;
    }
    setTrocaSelecionada(solicitacao);
    setSubstitutosDisponiveis(disponiveis);
    setSubstitutoSelecionado(String(disponiveis[0].voluntarioId));
    setTrocaDialogOpen(true);
  };

  const confirmAprovarTroca = async () => {
    if (!trocaSelecionada || !substitutoSelecionado) return;
    try {
      await solicitacoesTrocasEscalasApi.aprovar(trocaSelecionada.id, {
        voluntarioSubstitutoId: Number(substitutoSelecionado),
        observacaoResposta: null,
      });
      toast.success(t('volunteer.schedules.editor.exchangeApproved'));
      setTrocaDialogOpen(false);
      setTrocaSelecionada(null);
      setSubstitutosDisponiveis([]);
      setSubstitutoSelecionado('');
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorApproveExchange'));
      toast.error(message);
    }
  };

  const handleRejeitarTroca = async (solicitacao) => {
    const observacaoResposta = await promptDialog.prompt({
      title: t('volunteer.schedules.editor.prompts.rejectExchangeReasonTitle'),
      label: t('volunteer.schedules.editor.prompts.rejectExchangeReason', {
        name: solicitacao.voluntarioSolicitanteNome,
      }),
      description: t('volunteer.schedules.editor.prompts.optionalHint'),
      confirmText: t('confirmDialog.confirm'),
      cancelText: t('actions.cancel'),
    });
    if (observacaoResposta === null) return;

    try {
      await solicitacoesTrocasEscalasApi.rejeitar(solicitacao.id, { observacaoResposta });
      toast.success(t('volunteer.schedules.editor.exchangeRejected'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorRejectExchange'));
      toast.error(message);
    }
  };

  const handleRegistrarPresenca = async (item, compareceu) => {
    if (!escala) return;

    const observacaoOperacional = await promptDialog.prompt({
      title: compareceu
        ? t('volunteer.schedules.editor.prompts.attendanceNoteTitle')
        : t('volunteer.schedules.editor.prompts.absenceNoteTitle'),
      label: compareceu
        ? t('volunteer.schedules.editor.prompts.attendanceNote', { name: item.voluntarioNome })
        : t('volunteer.schedules.editor.prompts.absenceNote', { name: item.voluntarioNome }),
      description: t('volunteer.schedules.editor.prompts.optionalHint'),
      defaultValue: item.observacaoOperacional || '',
      confirmText: t('confirmDialog.confirm'),
      cancelText: t('actions.cancel'),
    });

    if (observacaoOperacional === null) return;

    try {
      await escalasApi.registrarPresenca(escala.id, item.id, {
        compareceu,
        observacaoOperacional,
      });
      toast.success(compareceu ? t('volunteer.schedules.editor.attendanceSuccess') : t('volunteer.schedules.editor.absenceSuccess'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.schedules.editor.errorRegisterAttendance'));
      toast.error(message);
    }
  };

  if (loading) return <LoadingPage text={t('volunteer.schedules.editor.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;
  if (!ocorrencia) return <ErrorPage message={t('volunteer.schedules.byOccurrence.notFound')} onRetry={load} />;

  const escalaStatusLabel = escala
    ? t(`volunteer.schedules.editor.scheduleStatus.${getEscalaStatusLabel(escala.status)}`)
    : t('volunteer.schedules.editor.scheduleStatus.notCreated');

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" asChild>
              <Link to={`/voluntariado/escalas/ocorrencia/${ocorrenciaId}`}>
                <ArrowLeft className="h-4 w-4 mr-2" />
                {t('actions.back')}
              </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold">
              {t('volunteer.schedules.editorTitle')}
              {escala?.equipeNome ? ` — ${escala.equipeNome}` : ''}
            </h1>
            <p className="text-muted-foreground">
              {ocorrencia.eventoTitulo} — {formatDateTime(ocorrencia.dataHoraInicio)}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <span className={`inline-flex items-center rounded-full px-3 py-1 text-sm font-semibold ${getEscalaStatusClassName(escala?.status)}`}>
            {escalaStatusLabel}
          </span>
          {escalaRascunho && (
            <Button
              variant="outline"
              onClick={handleGerarAutomatico}
              disabled={gerandoAuto}
            >
              <Wand2 className="h-4 w-4 mr-2" />
              {gerandoAuto ? t('volunteer.schedules.editor.autoFilling') : t('volunteer.schedules.editor.autoFill')}
            </Button>
          )}
          <Button
            onClick={handlePublicar}
            disabled={!escala || !escala.itens?.length}
          >
            <Send className="h-4 w-4 mr-2" />
            {t('volunteer.schedules.editor.publish')}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <Card><CardHeader><CardTitle>{t('volunteer.schedules.editor.summary.total')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold">{resumoEscala.total}</CardContent></Card>
        <Card><CardHeader><CardTitle>{t('volunteer.schedules.editor.summary.confirmed')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold text-emerald-400">{resumoEscala.confirmados}</CardContent></Card>
        <Card><CardHeader><CardTitle>{t('volunteer.schedules.editor.summary.pending')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold text-amber-400">{resumoEscala.pendentes}</CardContent></Card>
        <Card><CardHeader><CardTitle>{t('volunteer.schedules.editor.summary.declined')}</CardTitle></CardHeader><CardContent className="text-2xl font-bold text-rose-400">{resumoEscala.recusados}</CardContent></Card>
        <Card><CardHeader><CardTitle>{t('volunteer.schedules.editor.summary.eventDay')}</CardTitle></CardHeader><CardContent className="text-sm text-muted-foreground">{formatDateTime(ocorrencia.dataHoraInicio)}</CardContent></Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.schedules.editor.modelCoverageTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          {!modeloEscala ? (
            <div className="flex items-center justify-between gap-4 rounded-lg border border-dashed p-4">
              <div>
                <div className="font-medium">{t('volunteer.schedules.editor.noModelTitle')}</div>
                <div className="text-sm text-muted-foreground">
                  {t('volunteer.schedules.editor.noModelDescription')}
                </div>
              </div>
              <Button variant="outline" asChild>
                <Link to={`/voluntariado/modelos-escala/novo?equipeId=${equipeId}`}>
                  {t('volunteer.schedules.editor.createModel')}
                </Link>
              </Button>
            </div>
          ) : coberturaModelo.length === 0 ? (
            <div className="text-sm text-muted-foreground">{t('volunteer.schedules.editor.modelWithoutItems')}</div>
          ) : (
            <div className="space-y-3">
              {modeloEscala.nome && (
                <div className="text-sm text-muted-foreground">
                  {t('volunteer.schedules.editor.modelLabel')} <span className="font-medium text-foreground">{modeloEscala.nome}</span>
                </div>
              )}
              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                {coberturaModelo.map((item) => (
                  <div
                    key={item.id}
                    className={`rounded-lg border p-4 ${item.completo ? 'border-emerald-500/30 bg-emerald-500/10' : 'border-amber-500/30 bg-amber-500/10'}`}
                  >
                    <div className="font-medium">{item.cargoNome}</div>
                    <div className="mt-2 text-sm text-muted-foreground">
                      {t('volunteer.schedules.editor.filledLabel')} <span className="font-semibold text-foreground">{item.preenchidos}/{item.necessario}</span>
                    </div>
                    <div className={`mt-1 text-sm font-medium ${item.completo ? 'text-emerald-300' : 'text-amber-300'}`}>
                      {item.completo
                        ? t('volunteer.schedules.editor.fullCoverage')
                        : t('volunteer.schedules.editor.missingCount', { count: item.faltando })}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_1.4fr]">
        <Card>
          <CardHeader>
            <CardTitle>{t('volunteer.schedules.editor.availableNowTitle')}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t('volunteer.schedules.editor.availableNowDescription')}
            </p>
          </CardHeader>
          <CardContent className="space-y-3">
            {voluntariosDisponiveis.length === 0 ? (
              <div className="rounded-lg border border-dashed p-6 text-sm text-muted-foreground">
                {t('volunteer.schedules.editor.noAvailableVolunteers')}
              </div>
            ) : (
              voluntariosDisponiveis.map((voluntario) => (
                <div key={voluntario.id} className="flex items-center justify-between rounded-xl border p-4 gap-4">
                  <div className="space-y-1">
                      <div className="font-medium">{voluntario.nome}</div>
                      <div className="text-sm text-muted-foreground">
                      {voluntario.cargoNome || t('volunteer.schedules.byOccurrence.noRole')} • {t('volunteer.schedules.editor.recentHistory', { count: voluntario.cargaRecente })}
                      </div>
                    </div>
                  <Button size="sm" onClick={() => handleAddVoluntario(voluntario)} disabled={saving || Boolean(escala && !escalaRascunho)}>
                    <Plus className="h-4 w-4 mr-2" />
                    {t('actions.add')}
                  </Button>
                </div>
              ))
            )}

            {isAdmin && voluntariosBloqueados.length > 0 && (
              <div className="space-y-3 pt-2">
                <div className="text-sm font-medium text-muted-foreground">{t('volunteer.schedules.editor.blockedTitle')}</div>
                {voluntariosBloqueados.map((voluntario) => (
                  <div key={voluntario.id} className="flex items-center justify-between rounded-xl border border-dashed p-4 gap-4 opacity-80">
                    <div className="space-y-1">
                      <div className="font-medium">{voluntario.nome}</div>
                      <div className="text-sm text-muted-foreground">
                        {voluntario.cargoNome || t('volunteer.schedules.byOccurrence.noRole')} • {voluntario.motivoBloqueio || t('volunteer.schedules.editor.unavailable')}
                      </div>
                    </div>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleAddVoluntario(voluntario, true)}
                      disabled={saving || Boolean(escala && !escalaRascunho)}
                    >
                      {t('volunteer.schedules.editor.addWithException')}
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('volunteer.schedules.editor.builtScheduleTitle', { count: escala?.itens?.length || 0 })}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t('volunteer.schedules.editor.builtScheduleDescription')}
            </p>
          </CardHeader>
          <CardContent className="space-y-3">
            {!escala || !escala.itens?.length ? (
              <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
                {t('volunteer.schedules.editor.emptySchedule')}
              </div>
            ) : (
              escala.itens.map((item) => {
                const confirmarButton = getActionButtonProps(item, 'confirmar');
                const recusarButton = getActionButtonProps(item, 'recusar');
                const serviuButton = getActionButtonProps(item, 'serviu');
                const faltouButton = getActionButtonProps(item, 'faltou');

                return (
                  <div key={item.id} className="rounded-2xl border p-4 space-y-4">
                    <div className="flex items-start justify-between gap-4">
                      <div className="space-y-1">
                        <div className="font-medium text-base">{item.voluntarioNome}</div>
                        <div className="text-sm text-muted-foreground">
                          {item.cargoNome || t('volunteer.schedules.byOccurrence.noRole')} • {item.equipeNome}
                        </div>
                      </div>
                      <span className={`inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold ${getEscalaItemStatusClassName(item.status)}`}>
                        {t(`volunteer.schedules.editor.itemStatus.${getEscalaItemStatusLabel(item.status)}`)}
                      </span>
                    </div>

                    <div className="grid gap-3 md:grid-cols-2">
                      <div className="text-sm text-muted-foreground">
                        {t('volunteer.schedules.editor.responseLabel')} {item.dataConfirmacao
                          ? formatDateTime(item.dataConfirmacao)
                          : item.dataRecusa
                            ? formatDateTime(item.dataRecusa)
                            : t('volunteer.schedules.editor.awaitingResponse')}
                      </div>
                      <div className="text-sm text-muted-foreground">
                        {t('volunteer.schedules.editor.notesLabel')} {item.observacaoOperacional || item.motivoRecusa || (item.conflitoAprovado ? t('volunteer.schedules.editor.manualException') : '—')}
                      </div>
                    </div>

                    <div className="flex flex-wrap gap-2">
                      <Button variant="outline" size="sm" className={confirmarButton.className} onClick={() => handleConfirmarItem(item)}>
                        <CheckCircle2 className="h-4 w-4 mr-2" />
                        {t(`volunteer.schedules.editor.actionButtons.${confirmarButton.labelKey}`)}
                      </Button>
                      <Button variant="outline" size="sm" className={recusarButton.className} onClick={() => handleRecusarItem(item)}>
                        <XCircle className="h-4 w-4 mr-2" />
                        {t(`volunteer.schedules.editor.actionButtons.${recusarButton.labelKey}`)}
                      </Button>
                      <Button variant="outline" size="sm" className={serviuButton.className} onClick={() => handleRegistrarPresenca(item, true)}>
                        <UserCheck className="h-4 w-4 mr-2" />
                        {t(`volunteer.schedules.editor.actionButtons.${serviuButton.labelKey}`)}
                      </Button>
                      <Button variant="outline" size="sm" className={faltouButton.className} onClick={() => handleRegistrarPresenca(item, false)}>
                        <UserX className="h-4 w-4 mr-2" />
                        {t(`volunteer.schedules.editor.actionButtons.${faltouButton.labelKey}`)}
                      </Button>
                      {escalaRascunho && (
                        <Button variant="ghost" size="sm" onClick={() => handleDeleteItem(item)}>
                          <Trash2 className="h-4 w-4 mr-2" />
                          {t('actions.remove')}
                        </Button>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </CardContent>
        </Card>
      </div>

      {solicitacoesTroca.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>{t('volunteer.schedules.editor.exchangeRequestsTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('volunteer.schedules.exchangeRequests.table.requester')}</TableHead>
                  <TableHead>{t('volunteer.schedules.exchangeRequests.table.status')}</TableHead>
                  <TableHead>{t('volunteer.schedules.exchangeRequests.table.reason')}</TableHead>
                  <TableHead>{t('volunteer.schedules.exchangeRequests.table.substitute')}</TableHead>
                  <TableHead className="text-right">{t('volunteer.schedules.exchangeRequests.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {solicitacoesTroca.map((solicitacao) => (
                  <TableRow key={solicitacao.id}>
                    <TableCell className="font-medium">{solicitacao.voluntarioSolicitanteNome}</TableCell>
                    <TableCell>{solicitacao.status === 1 ? t('volunteer.schedules.exchangeRequests.status.pending') : solicitacao.status === 2 ? t('volunteer.schedules.exchangeRequests.status.approved') : t('volunteer.schedules.exchangeRequests.status.rejected')}</TableCell>
                    <TableCell>{solicitacao.motivo || t('common.notInformed')}</TableCell>
                    <TableCell>{solicitacao.voluntarioSubstitutoNome || t('common.notInformed')}</TableCell>
                    <TableCell className="text-right">
                      {solicitacao.status === 1 ? (
                        <div className="flex items-center justify-end gap-2">
                          <Button variant="outline" size="sm" onClick={() => handleAprovarTroca(solicitacao)}>
                            {t('volunteer.schedules.exchangeRequests.status.approved')}
                          </Button>
                          <Button variant="outline" size="sm" onClick={() => handleRejeitarTroca(solicitacao)}>
                            {t('volunteer.schedules.exchangeRequests.status.rejected')}
                          </Button>
                        </div>
                      ) : t('common.notInformed')}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      <Dialog open={trocaDialogOpen} onOpenChange={setTrocaDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('volunteer.schedules.editor.approveExchangeTitle')}</DialogTitle>
            <DialogDescription>
              {t('volunteer.schedules.editor.approveExchangeDescription')}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            {substitutosDisponiveis.map((sub) => (
              <label key={sub.voluntarioId} className="flex items-center justify-between rounded-lg border p-3 cursor-pointer">
                <div>
                  <div className="font-medium">{sub.voluntarioNome}</div>
                  <div className="text-sm text-muted-foreground">
                    {sub.cargoNome || t('volunteer.schedules.byOccurrence.noRole')} • {t('volunteer.schedules.editor.recentLoad', { count: sub.cargaRecente })}
                  </div>
                </div>
                <input
                  type="radio"
                  name="substituto"
                  value={sub.voluntarioId}
                  checked={String(sub.voluntarioId) === substitutoSelecionado}
                  onChange={(e) => setSubstitutoSelecionado(e.target.value)}
                />
              </label>
            ))}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setTrocaDialogOpen(false)}>{t('actions.cancel')}</Button>
            <Button onClick={confirmAprovarTroca}>{t('volunteer.schedules.editor.confirmSubstitute')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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

      <PromptDialog
        open={promptDialog.open}
        onOpenChange={promptDialog.onOpenChange}
        value={promptDialog.value}
        onValueChange={promptDialog.setValue}
        onConfirm={promptDialog.handleConfirm}
        config={promptDialog.config}
      />
    </div>
  );
}
