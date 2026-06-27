import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  BadgeCheck,
  Ban,
  Building2,
  Eye,
  Filter,
  LogOut,
  MessageSquareWarning,
  Pencil,
  PlusCircle,
  Send,
  Search,
  ShieldAlert,
  Ticket,
  TriangleAlert,
  Users,
  Layers3,
  UserPlus,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { kidsApi, pessoasApi } from '../../lib/api';
import Loading from '../../components/ui/loading';
import ErrorMessage from '../../components/ui/error-message';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ConteudoAulaDialog, CriancaDialog, HistoricoDialog, OcorrenciaDialog, ResponsavelDialog, SalaDialog, TurmaDialog } from './components/KidsDialogs';
import { CheckPanelIcon, EstadoVazio, IndicadorLinha, PainelCriancaCard, ResumoCard } from './components/KidsShared';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { buildCriticalDescription, formatOcorrenciaTipo, getOcorrenciaStatusConfig } from './components/kidsHelpers';
import { formatDateTime } from '@/lib/formatters';
import { getAbsoluteUrl } from '@/lib/utils';

const FORMULARIO_INICIAL = {
  criancaPessoaId: '',
  checkinId: null,
  tipo: 'OUTRO',
  titulo: '',
  descricao: '',
  requerContatoResponsavel: false,
  visivelAoResponsavel: false,
};

const SALA_FORM_INICIAL = {
  id: '',
  nome: '',
  capacidadeMaxima: '',
  ativo: true,
};

const TURMA_FORM_INICIAL = {
  id: '',
  salaId: '',
  nome: '',
  capacidadeMaxima: '',
  ativo: true,
};

// Versão do termo de consentimento parental vigente (LGPD).
const CONSENTIMENTO_PARENTAL_VERSAO = 'v1';

const CRIANCA_FORM_INICIAL = {
  nome: '',
  dataNascimento: '',
  salaId: '',
  turmaId: '',
  alergias: '',
  restricoesAlimentares: '',
  observacoes: '',
  consentimentoParental: false,
};

const RESPONSAVEL_FORM_INICIAL = {
  parentesco: '',
  podeRetirar: true,
};

const CONTEUDO_AULA_FORM_INICIAL = {
  id: null,
  titulo: '',
  tema: '',
  versiculo: '',
  resumo: '',
  atividadeEmCasa: '',
  observacaoResponsavel: '',
  dataReferencia: '',
  salaId: '',
  turmaId: '',
  anexos: [],
};

const KidsCheckinsList = ({ section = 'overview' }) => {
  const { t } = useTranslation();
  const confirmDialog = useConfirmDialog();
  const [painel, setPainel] = useState(null);
  const [indicadores, setIndicadores] = useState(null);
  const [checkins, setCheckins] = useState([]);
  const [criancas, setCriancas] = useState([]);
  const [salas, setSalas] = useState([]);
  const [turmas, setTurmas] = useState([]);
  const [ocorrenciasAbertas, setOcorrenciasAbertas] = useState([]);
  const [preCheckinsPendentes, setPreCheckinsPendentes] = useState([]);
  const [conteudosAula, setConteudosAula] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [abaAtiva, setAbaAtiva] = useState('painel');
  const [filtros, setFiltros] = useState({
    criancaPessoaId: '',
    status: '',
    dataInicio: '',
    dataFim: '',
    busca: '',
    salaId: 'todas',
  });
  const [ocorrenciaDialogOpen, setOcorrenciaDialogOpen] = useState(false);
  const [historicoDialogOpen, setHistoricoDialogOpen] = useState(false);
  const [ocorrenciaForm, setOcorrenciaForm] = useState(FORMULARIO_INICIAL);
  const [ocorrenciaSaving, setOcorrenciaSaving] = useState(false);
  const [historicoLoading, setHistoricoLoading] = useState(false);
  const [historicoUpdatingId, setHistoricoUpdatingId] = useState(null);
  const [criancaHistorico, setCriancaHistorico] = useState(null);
  const [ocorrenciasHistorico, setOcorrenciasHistorico] = useState([]);
  const [salaDialogOpen, setSalaDialogOpen] = useState(false);
  const [turmaDialogOpen, setTurmaDialogOpen] = useState(false);
  const [criancaDialogOpen, setCriancaDialogOpen] = useState(false);
  const [salaForm, setSalaForm] = useState(SALA_FORM_INICIAL);
  const [turmaForm, setTurmaForm] = useState(TURMA_FORM_INICIAL);
  const [criancaForm, setCriancaForm] = useState(CRIANCA_FORM_INICIAL);
  const [salaSaving, setSalaSaving] = useState(false);
  const [turmaSaving, setTurmaSaving] = useState(false);
  const [criancaSaving, setCriancaSaving] = useState(false);
  const [responsavelDialogOpen, setResponsavelDialogOpen] = useState(false);
  const [criancaResponsavel, setCriancaResponsavel] = useState(null);
  const [responsavelForm, setResponsavelForm] = useState(RESPONSAVEL_FORM_INICIAL);
  const [responsavelQuery, setResponsavelQuery] = useState('');
  const [responsavelResultados, setResponsavelResultados] = useState([]);
  const [responsavelSelecionado, setResponsavelSelecionado] = useState(null);
  const [buscandoResponsavel, setBuscandoResponsavel] = useState(false);
  const [responsavelSaving, setResponsavelSaving] = useState(false);
  const [desvinculandoResponsavelId, setDesvinculandoResponsavelId] = useState(null);
  const [confirmandoPreCheckinId, setConfirmandoPreCheckinId] = useState(null);
  const [cancelandoPreCheckinId, setCancelandoPreCheckinId] = useState(null);
  const [conteudoDialogOpen, setConteudoDialogOpen] = useState(false);
  const [conteudoForm, setConteudoForm] = useState(CONTEUDO_AULA_FORM_INICIAL);
  const [conteudoSaving, setConteudoSaving] = useState(false);
  const [publicandoConteudoId, setPublicandoConteudoId] = useState(null);
  const [arquivandoConteudoId, setArquivandoConteudoId] = useState(null);

  const sectionConfig = {
    title: t(`kids.sections.${section}.title`, { defaultValue: t('kids.sections.overview.title') }),
    subtitle: t(`kids.sections.${section}.subtitle`, { defaultValue: t('kids.sections.overview.subtitle') }),
  };
  const isOverview = section === 'overview';
  const showPainel = isOverview || section === 'painel';
  const showCriancas = isOverview || section === 'criancas';
  const showEstrutura = isOverview || section === 'estrutura';
  const showHistorico = isOverview || section === 'historico';
  const showConteudos = isOverview || section === 'conteudos';

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const salaFiltro = filtros.salaId && filtros.salaId !== 'todas' ? filtros.salaId : undefined;
      const [painelResponse, indicadoresResponse, checkinsResponse, criancasResponse, ocorrenciasAbertasResponse, salasResponse, turmasResponse, preCheckinsResponse, conteudosAulaResponse] = await Promise.all([
        kidsApi.getPainelOperacional(salaFiltro ? { salaId: salaFiltro } : {}),
        kidsApi.getIndicadores({ dias: 30 }),
        kidsApi.getCheckins(),
        kidsApi.getCriancas(),
        kidsApi.getOcorrenciasAbertas(),
        kidsApi.getSalas(),
        kidsApi.getTurmas(),
        kidsApi.getPreCheckinsPendentes(salaFiltro ? { salaId: salaFiltro } : {}),
        kidsApi.getConteudosAula({ limit: 8 }),
      ]);

      setPainel(painelResponse.data);
      setIndicadores(indicadoresResponse.data);
      setCheckins(checkinsResponse.data);
      setCriancas(criancasResponse.data);
      setOcorrenciasAbertas(ocorrenciasAbertasResponse.data || []);
      setSalas(salasResponse.data || []);
      setTurmas(turmasResponse.data || []);
      setPreCheckinsPendentes(preCheckinsResponse.data || []);
      setConteudosAula(conteudosAulaResponse.data || []);
    } catch (err) {
      setError(t('kids.errorLoad', 'Erro ao carregar dados do Kids'));
      console.error('Erro ao buscar dados do painel Kids:', err);
      toast.error(t('kids.toastError', 'Erro ao carregar painel de Kids'));
    } finally {
      setLoading(false);
    }
  }, [filtros.salaId, t]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  useEffect(() => {
    if (section === 'historico' && abaAtiva !== 'historico') {
      setAbaAtiva('historico');
    }
    if (section !== 'historico' && abaAtiva !== 'painel') {
      setAbaAtiva('painel');
    }
  }, [section, abaAtiva]);

  const handleFiltroChange = (name, value) => {
    setFiltros((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const limparFiltros = () => {
    setFiltros({
      criancaPessoaId: '',
      status: '',
      dataInicio: '',
      dataFim: '',
      busca: '',
      salaId: 'todas',
    });
  };

  const salasDisponiveis = useMemo(() => {
    return salas.map((sala) => sala.id);
  }, [salas]);

  const turmasPorSala = useMemo(() => {
    return turmas.reduce((acc, turma) => {
      if (!acc[turma.salaId]) {
        acc[turma.salaId] = [];
      }
      acc[turma.salaId].push(turma);
      return acc;
    }, {});
  }, [turmas]);

  const checkinsFiltrados = useMemo(() => {
    return checkins.filter((checkin) => {
      if (filtros.criancaPessoaId && checkin.criancaPessoaId !== parseInt(filtros.criancaPessoaId, 10)) {
        return false;
      }

      if (filtros.status) {
        const statusLower = (checkin.status || '').toLowerCase();
        if (filtros.status === 'ativo' && statusLower !== 'checkedin' && statusLower !== 'checked_in' && statusLower !== 'ativo') {
          return false;
        }
        if (filtros.status === 'finalizado' && statusLower !== 'checkedout' && statusLower !== 'checked_out' && statusLower !== 'finalizado') {
          return false;
        }
      }

      if (filtros.dataInicio) {
        const dataInicio = new Date(filtros.dataInicio);
        dataInicio.setHours(0, 0, 0, 0);
        const checkinDate = new Date(checkin.checkinTime);
        checkinDate.setHours(0, 0, 0, 0);
        if (checkinDate < dataInicio) {
          return false;
        }
      }

      if (filtros.dataFim) {
        const dataFim = new Date(filtros.dataFim);
        dataFim.setHours(23, 59, 59, 999);
        if (new Date(checkin.checkinTime) > dataFim) {
          return false;
        }
      }

      if (filtros.busca) {
        const buscaLower = filtros.busca.toLowerCase();
        if (!checkin.criancaNome?.toLowerCase().includes(buscaLower)) {
          return false;
        }
      }

      return true;
    });
  }, [checkins, filtros]);

  const criancasOrdenadas = useMemo(() => {
    return [...criancas].sort((a, b) => (a.nome || '').localeCompare(b.nome || ''));
  }, [criancas]);

  const formatDate = (dateString) => {
    return formatDateTime(dateString, '-', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const formatDuration = (checkinTime, checkoutTime) => {
    if (!checkoutTime) return '-';
    const inicio = new Date(checkinTime);
    const fim = new Date(checkoutTime);
    const diff = fim - inicio;
    const horas = Math.floor(diff / (1000 * 60 * 60));
    const minutos = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    return `${horas}h ${minutos}m`;
  };

  const getConteudoAnexoIconLabel = (tipo) => {
    const normalized = (tipo || '').toLowerCase();
    if (normalized === 'imagem') return 'IMG';
    if (normalized === 'pdf') return 'PDF';
    if (normalized === 'link') return 'LINK';
    return 'ARQ';
  };

  const getStatusBadge = (status) => {
    const statusLower = status?.toLowerCase() || '';
    if (statusLower === 'checked_in' || statusLower === 'checkedin' || statusLower === 'ativo') {
      return <Badge className="bg-green-500 hover:bg-green-600">{t('kids.active', 'Ativo')}</Badge>;
    }
    if (statusLower === 'checked_out' || statusLower === 'checkedout' || statusLower === 'finalizado') {
      return <Badge className="bg-gray-500 hover:bg-gray-600">{t('kids.finished', 'Finalizado')}</Badge>;
    }
    return <Badge variant="secondary">{status || t('kids.unknown', 'Desconhecido')}</Badge>;
  };

  const getMetodoBadge = (metodo) => {
    const metodoLower = metodo?.toLowerCase() || '';
    const cores = {
      qr: 'bg-blue-500',
      pin: 'bg-amber-500',
      admin: 'bg-orange-500',
      excecao: 'bg-red-500',
    };
    return <Badge className={cores[metodoLower] || 'bg-gray-500'}>{metodo?.toUpperCase() || 'N/A'}</Badge>;
  };

  const abrirRegistroOcorrencia = (crianca) => {
    const checkinAtivo = checkins.find((item) => item.criancaPessoaId === crianca.criancaPessoaId && !item.checkoutTime);

    setOcorrenciaForm({
      criancaPessoaId: String(crianca.criancaPessoaId),
      checkinId: checkinAtivo?.id ?? null,
      tipo: 'OUTRO',
      titulo: '',
      descricao: '',
      requerContatoResponsavel: false,
      visivelAoResponsavel: false,
    });
    setOcorrenciaDialogOpen(true);
  };

  const carregarHistoricoCrianca = async (crianca) => {
    try {
      setHistoricoLoading(true);
      setCriancaHistorico(crianca);
      setHistoricoDialogOpen(true);
      const response = await kidsApi.getOcorrenciasByCrianca(crianca.criancaPessoaId);
      setOcorrenciasHistorico(response.data || []);
    } catch (err) {
      console.error('Error loading occurrence history:', err);
      toast.error(t('kids.history.loadError'));
    } finally {
      setHistoricoLoading(false);
    }
  };

  const handleOcorrenciaFormChange = (name, value) => {
    setOcorrenciaForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSalaFormChange = (name, value) => {
    setSalaForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleTurmaFormChange = (name, value) => {
    setTurmaForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleCriancaFormChange = (name, value) => {
    setCriancaForm((prev) => {
      if (name === 'salaId') {
        const turmaAtual = prev.turmaId;
        const turmaAindaValida = turmas.some((item) => item.id === turmaAtual && item.salaId === value);
        return {
          ...prev,
          salaId: value,
          turmaId: turmaAindaValida ? turmaAtual : '',
        };
      }

      return {
        ...prev,
        [name]: value,
      };
    });
  };

  const handleResponsavelFormChange = (name, value) => {
    setResponsavelForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleConteudoFormChange = (name, value) => {
    setConteudoForm((prev) => {
      if (name === 'salaId') {
        const turmaAtual = prev.turmaId;
        const turmaAindaValida = turmas.some((item) => item.id === turmaAtual && item.salaId === value);
        return {
          ...prev,
          salaId: value,
          turmaId: turmaAindaValida ? turmaAtual : '',
        };
      }

      return {
        ...prev,
        [name]: value,
      };
    });
  };

  const handleConteudoAnexoChange = (index, field, value) => {
    setConteudoForm((prev) => ({
      ...prev,
      anexos: prev.anexos.map((item, itemIndex) => (itemIndex === index ? { ...item, [field]: value } : item)),
    }));
  };

  const adicionarConteudoAnexo = () => {
    setConteudoForm((prev) => ({
      ...prev,
      anexos: [
        ...prev.anexos,
        {
          tipo: 'Pdf',
          nomeExibicao: '',
          url: '',
        },
      ],
    }));
  };

  const removerConteudoAnexo = (index) => {
    setConteudoForm((prev) => ({
      ...prev,
      anexos: prev.anexos.filter((_, itemIndex) => itemIndex !== index),
    }));
  };

  const abrirResponsavelDialog = (crianca) => {
    setCriancaResponsavel(crianca);
    setResponsavelDialogOpen(true);
    setResponsavelForm(RESPONSAVEL_FORM_INICIAL);
    setResponsavelQuery('');
    setResponsavelResultados([]);
    setResponsavelSelecionado(null);
  };

  const abrirNovoConteudoDialog = () => {
    setConteudoForm({
      ...CONTEUDO_AULA_FORM_INICIAL,
      dataReferencia: new Date().toISOString().slice(0, 10),
    });
    setConteudoDialogOpen(true);
  };

  const abrirEditarConteudoDialog = (conteudo) => {
    setConteudoForm({
      id: conteudo.id,
      titulo: conteudo.titulo || '',
      tema: conteudo.tema || '',
      versiculo: conteudo.versiculo || '',
      resumo: conteudo.resumo || '',
      atividadeEmCasa: conteudo.atividadeEmCasa || '',
      observacaoResponsavel: conteudo.observacaoResponsavel || '',
      dataReferencia: conteudo.dataReferencia ? new Date(conteudo.dataReferencia).toISOString().slice(0, 10) : '',
      salaId: conteudo.salaId || '',
      turmaId: conteudo.turmaId || '',
      anexos: (conteudo.anexos || []).map((anexo) => ({
        tipo: anexo.tipo || 'Pdf',
        nomeExibicao: anexo.nomeExibicao || '',
        url: anexo.url || anexo.storagePath || '',
      })),
    });
    setConteudoDialogOpen(true);
  };

  const buscarResponsaveis = async () => {
    const query = responsavelQuery.trim();
    if (query.length < 2) {
      toast.error(t('kids.children.guardianSearchMinChars', { defaultValue: 'Digite pelo menos 2 caracteres para buscar.' }));
      return;
    }

    try {
      setBuscandoResponsavel(true);
      const queryDigits = query.replace(/\D/g, '');
      const looksLikeEmail = query.includes('@');
      const looksLikePhone = queryDigits.length >= 8;
      const requests = looksLikePhone
        ? [
            pessoasApi.getPaged({
              page: 1,
              pageSize: 8,
              sort: 'nome',
              direction: 'asc',
              telefone: queryDigits,
            }),
            pessoasApi.getPaged({
              page: 1,
              pageSize: 8,
              sort: 'nome',
              direction: 'asc',
              whatsApp: queryDigits,
            }),
          ]
        : [
            pessoasApi.getPaged({
              page: 1,
              pageSize: 8,
              sort: 'nome',
              direction: 'asc',
              nome: !looksLikeEmail ? query : undefined,
              email: looksLikeEmail ? query : undefined,
            }),
          ];

      const responses = await Promise.all(requests);
      const merged = responses.flatMap((response) => response.data?.items || []);
      const deduped = Array.from(new Map(merged.map((item) => [item.id, item])).values());
      setResponsavelResultados(deduped.slice(0, 8));
    } catch (err) {
      console.error('Erro ao buscar responsáveis:', err);
      toast.error(t('kids.children.guardianSearchError', { defaultValue: 'Erro ao buscar pessoas.' }));
    } finally {
      setBuscandoResponsavel(false);
    }
  };

  const selecionarResponsavel = (pessoa) => {
    setResponsavelSelecionado(pessoa);
    setResponsavelResultados([]);
    setResponsavelQuery(pessoa.nome || String(pessoa.id));
  };

  const vincularResponsavel = async () => {
    if (!criancaResponsavel || !responsavelSelecionado) {
      toast.error(t('kids.children.guardianSelectFirst', { defaultValue: 'Selecione um responsável antes de salvar.' }));
      return;
    }

    try {
      setResponsavelSaving(true);
      await kidsApi.vincularResponsavel(criancaResponsavel.pessoaId, {
        responsavelPessoaId: Number(responsavelSelecionado.id),
        parentesco: responsavelForm.parentesco?.trim() || null,
        podeRetirar: responsavelForm.podeRetirar,
      });
      toast.success(t('kids.children.guardianLinkedSuccess', { defaultValue: 'Responsável vinculado com sucesso.' }));
      setResponsavelDialogOpen(false);
      await fetchData();
    } catch (err) {
      console.error('Erro ao vincular responsável:', err);
      toast.error(err.response?.data?.message || t('kids.children.guardianLinkedError', { defaultValue: 'Erro ao vincular responsável.' }));
    } finally {
      setResponsavelSaving(false);
    }
  };

  const desvincularResponsavel = async (responsavel) => {
    if (!responsavel?.id) return;

    try {
      setDesvinculandoResponsavelId(responsavel.id);
      await kidsApi.desvincularResponsavel(responsavel.id);
      toast.success(t('kids.children.guardianUnlinkedSuccess', { defaultValue: 'Responsável removido com sucesso.' }));
      await fetchData();
      setCriancaResponsavel((prev) => {
        if (!prev) return prev;
        return {
          ...prev,
          responsaveis: (prev.responsaveis || []).filter((item) => item.id !== responsavel.id),
        };
      });
    } catch (err) {
      console.error('Erro ao desvincular responsável:', err);
      toast.error(err.response?.data?.message || t('kids.children.guardianUnlinkedError', { defaultValue: 'Erro ao remover responsável.' }));
    } finally {
      setDesvinculandoResponsavelId(null);
    }
  };

  const handleCriarOcorrencia = async () => {
    if (!ocorrenciaForm.criancaPessoaId || !ocorrenciaForm.titulo.trim() || !ocorrenciaForm.descricao.trim()) {
      toast.error(t('kids.occurrence.validationRequired'));
      return;
    }

    try {
      setOcorrenciaSaving(true);
      await kidsApi.createOcorrencia({
        criancaPessoaId: Number(ocorrenciaForm.criancaPessoaId),
        checkinId: ocorrenciaForm.checkinId,
        tipo: ocorrenciaForm.tipo,
        titulo: ocorrenciaForm.titulo.trim(),
        descricao: ocorrenciaForm.descricao.trim(),
        requerContatoResponsavel: ocorrenciaForm.requerContatoResponsavel,
        visivelAoResponsavel: ocorrenciaForm.visivelAoResponsavel,
      });

      toast.success(t('kids.occurrence.createSuccess'));
      setOcorrenciaDialogOpen(false);
      setOcorrenciaForm(FORMULARIO_INICIAL);
      await fetchData();

      if (criancaHistorico && Number(ocorrenciaForm.criancaPessoaId) === criancaHistorico.criancaPessoaId) {
        await carregarHistoricoCrianca(criancaHistorico);
      }
    } catch (err) {
      console.error('Error creating occurrence:', err);
      toast.error(t('kids.occurrence.createError'));
    } finally {
      setOcorrenciaSaving(false);
    }
  };

  const handleAtualizarOcorrencia = async (ocorrenciaId, payload) => {
    try {
      setHistoricoUpdatingId(ocorrenciaId);
      await kidsApi.updateOcorrencia(ocorrenciaId, payload);
      toast.success(t('kids.occurrence.updateSuccess'));

      if (criancaHistorico) {
        await carregarHistoricoCrianca(criancaHistorico);
      }
      await fetchData();
    } catch (err) {
      console.error('Error updating occurrence:', err);
      toast.error(t('kids.occurrence.updateError'));
    } finally {
      setHistoricoUpdatingId(null);
    }
  };

  const handleCriarSala = async () => {
    if (!salaForm.id.trim() || !salaForm.nome.trim()) {
      toast.error(t('kids.structure.roomValidation'));
      return;
    }

    try {
      setSalaSaving(true);
      await kidsApi.createSala({
        id: salaForm.id.trim(),
        nome: salaForm.nome.trim(),
        capacidadeMaxima: salaForm.capacidadeMaxima ? Number(salaForm.capacidadeMaxima) : null,
        ativo: salaForm.ativo,
      });
      toast.success(t('kids.structure.roomCreateSuccess'));
      setSalaDialogOpen(false);
      setSalaForm(SALA_FORM_INICIAL);
      await fetchData();
    } catch (err) {
      console.error('Error creating room:', err);
      toast.error(t('kids.structure.roomCreateError'));
    } finally {
      setSalaSaving(false);
    }
  };

  const handleCriarTurma = async () => {
    if (!turmaForm.id.trim() || !turmaForm.nome.trim() || !turmaForm.salaId) {
      toast.error(t('kids.structure.classValidation'));
      return;
    }

    try {
      setTurmaSaving(true);
      await kidsApi.createTurma({
        id: turmaForm.id.trim(),
        salaId: turmaForm.salaId,
        nome: turmaForm.nome.trim(),
        capacidadeMaxima: turmaForm.capacidadeMaxima ? Number(turmaForm.capacidadeMaxima) : null,
        ativo: turmaForm.ativo,
      });
      toast.success(t('kids.structure.classCreateSuccess'));
      setTurmaDialogOpen(false);
      setTurmaForm(TURMA_FORM_INICIAL);
      await fetchData();
    } catch (err) {
      console.error('Error creating class:', err);
      toast.error(t('kids.structure.classCreateError'));
    } finally {
      setTurmaSaving(false);
    }
  };

  const handleCriarCrianca = async () => {
    if (!criancaForm.nome.trim() || !criancaForm.dataNascimento || !criancaForm.salaId) {
      toast.error(t('kids.children.validationRequired'));
      return;
    }

    if (!criancaForm.consentimentoParental) {
      toast.error(t('kids.children.parentalConsentRequired', {
        defaultValue: 'É necessário confirmar o consentimento parental para cadastrar a criança.',
      }));
      return;
    }

    try {
      setCriancaSaving(true);
      await kidsApi.createCrianca({
        nome: criancaForm.nome.trim(),
        dataNascimento: new Date(criancaForm.dataNascimento).toISOString(),
        salaId: criancaForm.salaId || null,
        turmaId: criancaForm.turmaId || null,
        alergias: criancaForm.alergias.trim() || null,
        restricoesAlimentares: criancaForm.restricoesAlimentares.trim() || null,
        observacoes: criancaForm.observacoes.trim() || null,
        consentimentoParentalVersao: CONSENTIMENTO_PARENTAL_VERSAO,
      });
      toast.success(t('kids.children.createSuccess'));
      setCriancaDialogOpen(false);
      setCriancaForm(CRIANCA_FORM_INICIAL);
      await fetchData();
    } catch (err) {
      console.error('Error creating child:', err);
      toast.error(t('kids.children.createError'));
    } finally {
      setCriancaSaving(false);
    }
  };

  const handleSalvarConteudo = async () => {
    if (!conteudoForm.titulo.trim() || !conteudoForm.resumo.trim() || !conteudoForm.dataReferencia) {
      toast.error(t('kids.lessonContent.validationRequired', { defaultValue: 'Título, resumo e data são obrigatórios.' }));
      return;
    }

    const anexosInvalidos = conteudoForm.anexos.some((anexo) => !anexo.nomeExibicao.trim() || !anexo.url.trim());
    if (anexosInvalidos) {
      toast.error(t('kids.lessonContent.validationAttachment', { defaultValue: 'Preencha nome e referência de todos os materiais anexos.' }));
      return;
    }

    const payload = {
      titulo: conteudoForm.titulo.trim(),
      tema: conteudoForm.tema.trim() || null,
      versiculo: conteudoForm.versiculo.trim() || null,
      resumo: conteudoForm.resumo.trim(),
      atividadeEmCasa: conteudoForm.atividadeEmCasa.trim() || null,
      observacaoResponsavel: conteudoForm.observacaoResponsavel.trim() || null,
      dataReferencia: new Date(`${conteudoForm.dataReferencia}T12:00:00`).toISOString(),
      salaId: conteudoForm.salaId || null,
      turmaId: conteudoForm.turmaId || null,
      anexos: conteudoForm.anexos.map((anexo, index) => ({
        tipo: anexo.tipo,
        nomeExibicao: anexo.nomeExibicao.trim(),
        url: anexo.url.trim(),
        ordem: index + 1,
      })),
    };

    try {
      setConteudoSaving(true);
      if (conteudoForm.id) {
        await kidsApi.updateConteudoAula(conteudoForm.id, payload);
        toast.success(t('kids.lessonContent.updateSuccess', { defaultValue: 'Conteúdo da aula atualizado.' }));
      } else {
        await kidsApi.createConteudoAula(payload);
        toast.success(t('kids.lessonContent.createSuccess', { defaultValue: 'Conteúdo da aula criado.' }));
      }

      setConteudoDialogOpen(false);
      setConteudoForm(CONTEUDO_AULA_FORM_INICIAL);
      await fetchData();
    } catch (err) {
      console.error('Error saving lesson content:', err);
      toast.error(err.response?.data?.message || t('kids.lessonContent.saveError', { defaultValue: 'Erro ao salvar conteúdo da aula.' }));
    } finally {
      setConteudoSaving(false);
    }
  };

  const handlePublicarConteudo = async (conteudoId) => {
    try {
      setPublicandoConteudoId(conteudoId);
      await kidsApi.publicarConteudoAula(conteudoId);
      toast.success(t('kids.lessonContent.publishSuccess', { defaultValue: 'Conteúdo publicado no AppKids.' }));
      await fetchData();
    } catch (err) {
      console.error('Error publishing lesson content:', err);
      toast.error(err.response?.data?.message || t('kids.lessonContent.publishError', { defaultValue: 'Erro ao publicar conteúdo da aula.' }));
    } finally {
      setPublicandoConteudoId(null);
    }
  };

  const handleArquivarConteudo = async (conteudoId) => {
    try {
      setArquivandoConteudoId(conteudoId);
      await kidsApi.arquivarConteudoAula(conteudoId);
      toast.success(t('kids.lessonContent.archiveSuccess', { defaultValue: 'Conteúdo arquivado.' }));
      await fetchData();
    } catch (err) {
      console.error('Error archiving lesson content:', err);
      toast.error(err.response?.data?.message || t('kids.lessonContent.archiveError', { defaultValue: 'Erro ao arquivar conteúdo da aula.' }));
    } finally {
      setArquivandoConteudoId(null);
    }
  };

  const handleConfirmarPreCheckin = (preCheckin) => {
    confirmDialog.show({
      title: t('kids.preCheckins.confirmTitle', { defaultValue: 'Confirmar pré-check-in' }),
      description: t('kids.preCheckins.confirmPrompt', {
        defaultValue: 'Confirmar o pré-check-in de {{name}} e concluir o check-in operacional agora?',
        name: preCheckin.criancaNome,
      }),
      confirmText: t('actions.confirm', { defaultValue: 'Confirmar' }),
      cancelText: t('actions.cancel'),
      onConfirm: async () => {
        try {
          setConfirmandoPreCheckinId(preCheckin.id);
          await kidsApi.confirmarPreCheckin(preCheckin.id, {
            salaId: preCheckin.salaId || null,
            turmaId: preCheckin.turmaId || null,
          });
          toast.success(
            t('kids.preCheckins.confirmSuccess', {
              defaultValue: 'Pré-check-in confirmado para {{name}}.',
              name: preCheckin.criancaNome,
            }),
          );
          await fetchData();
        } catch (err) {
          console.error('Erro ao confirmar pré-check-in:', err);
          toast.error(err.response?.data?.message || t('kids.preCheckins.confirmError', { defaultValue: 'Erro ao confirmar pré-check-in.' }));
          throw err;
        } finally {
          setConfirmandoPreCheckinId(null);
        }
      },
    });
  };

  const handleCancelarPreCheckin = (preCheckin) => {
    confirmDialog.show({
      title: t('kids.preCheckins.cancelTitle', { defaultValue: 'Cancelar pré-check-in' }),
      description: t('kids.preCheckins.cancelPrompt', {
        defaultValue: 'Cancelar o pré-check-in de {{name}}?',
        name: preCheckin.criancaNome,
      }),
      confirmText: t('actions.confirm', { defaultValue: 'Confirmar' }),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          setCancelandoPreCheckinId(preCheckin.id);
          await kidsApi.cancelarPreCheckin(preCheckin.id, {
            motivo: 'Cancelado pela equipe no painel operacional.',
          });
          toast.success(
            t('kids.preCheckins.cancelSuccess', {
              defaultValue: 'Pré-check-in cancelado para {{name}}.',
              name: preCheckin.criancaNome,
            }),
          );
          await fetchData();
        } catch (err) {
          console.error('Erro ao cancelar pré-check-in:', err);
          toast.error(err.response?.data?.message || t('kids.preCheckins.cancelError', { defaultValue: 'Erro ao cancelar pré-check-in.' }));
          throw err;
        } finally {
          setCancelandoPreCheckinId(null);
        }
      },
    });
  };

  if (loading) {
    return <Loading text={t('kids.loading', 'Carregando...')} />;
  }

  if (error) {
    return <ErrorMessage message={error} onRetry={fetchData} />;
  }

  const painelContent = (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ResumoCard
          title={t('kids.panel.presentNow', 'Presentes agora')}
          value={painel?.totalPresentes ?? 0}
          description={t('kids.panel.presentNowHint')}
          icon={Users}
          valueClassName="text-blue-600"
        />
        <ResumoCard
          title={t('kids.panel.pendingPickup', 'Pendentes de retirada')}
          value={painel?.totalPendentesRetirada ?? 0}
          description={t('kids.panel.pendingPickupHint')}
          icon={LogOut}
          valueClassName="text-amber-600"
        />
        <ResumoCard
          title={t('kids.panel.completedToday', 'Retiradas hoje')}
          value={painel?.totalRetiradasHoje ?? 0}
          description={t('kids.panel.completedTodayHint')}
          icon={CheckPanelIcon}
          valueClassName="text-emerald-600"
        />
        <ResumoCard
          title={t('kids.occurrence.openTitle')}
          value={ocorrenciasAbertas.length}
          description={t('kids.occurrence.openDescription')}
          icon={MessageSquareWarning}
          valueClassName="text-rose-600"
        />
        <ResumoCard
          title={t('kids.preCheckins.pendingTitle', { defaultValue: 'Pré-check-ins pendentes' })}
          value={preCheckinsPendentes.length}
          description={t('kids.preCheckins.pendingDescription', { defaultValue: 'Famílias aguardando confirmação na recepção' })}
          icon={Ticket}
          valueClassName="text-indigo-600"
        />
      </div>

      <div className="grid gap-4 xl:grid-cols-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('kids.panel.indicators30Days')}</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-3 text-sm">
            <IndicadorLinha label={t('kids.panel.checkinsInPeriod')} value={indicadores?.totalCheckinsPeriodo ?? 0} />
            <IndicadorLinha label={t('kids.panel.dailyAverage')} value={indicadores?.mediaCheckinsPorDia ?? 0} />
            <IndicadorLinha label={t('kids.panel.presentNow')} value={indicadores?.totalCriancasPresentesAgora ?? 0} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('kids.panel.pickupMethods')}</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-3 text-sm">
            <IndicadorLinha label="QR" value={indicadores?.totalRetiradasQr ?? 0} />
            <IndicadorLinha label="PIN" value={indicadores?.totalRetiradasPin ?? 0} />
            <IndicadorLinha label={t('kids.panel.exception')} value={indicadores?.totalRetiradasExcecao ?? 0} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('kids.panel.registeredBase')}</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-3 text-sm">
            <IndicadorLinha label={t('kids.panel.activeChildren')} value={indicadores?.totalCriancasAtivas ?? 0} />
            <IndicadorLinha label={t('kids.panel.activeGuardians')} value={indicadores?.totalResponsaveisAtivos ?? 0} />
            <IndicadorLinha label={t('kids.panel.roomsClasses')} value={`${indicadores?.totalSalasAtivas ?? 0} / ${indicadores?.totalTurmasAtivas ?? 0}`} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('kids.preCheckins.operationalQueue', { defaultValue: 'Fila de recepção' })}</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-3 text-sm">
            <IndicadorLinha
              label={t('kids.preCheckins.waitingNow', { defaultValue: 'Aguardando agora' })}
              value={preCheckinsPendentes.length}
            />
            <IndicadorLinha
              label={t('kids.preCheckins.withRoom', { defaultValue: 'Com sala sugerida' })}
              value={preCheckinsPendentes.filter((item) => item.salaId).length}
            />
            <IndicadorLinha
              label={t('kids.preCheckins.expiringSoon', { defaultValue: 'Expirando em 15 min' })}
              value={preCheckinsPendentes.filter((item) => {
                const expiration = new Date(item.expiraEm).getTime();
                return expiration - Date.now() <= 15 * 60 * 1000;
              }).length}
            />
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
        <Card>
          <CardHeader>
            <CardTitle>{t('kids.panel.presentList')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {painel?.criancasPresentes?.length ? (
              painel.criancasPresentes.map((crianca) => (
                <PainelCriancaCard
                  key={crianca.criancaPessoaId}
                  crianca={crianca}
                  onRegistrarOcorrencia={() => abrirRegistroOcorrencia(crianca)}
                  onVerHistorico={() => carregarHistoricoCrianca(crianca)}
                />
              ))
            ) : (
              <EstadoVazio texto={t('kids.panel.noChildrenPresent')} />
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>{t('kids.panel.byRoom')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {painel?.salas?.length ? (
                painel.salas.map((sala) => (
                  <div
                    key={sala.salaId}
                    className="rounded-xl border border-border bg-muted/20 p-4"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <h3 className="font-semibold text-foreground">{sala.salaId}</h3>
                        <p className="text-sm text-muted-foreground">
                          {t('kids.panel.roomSummary', {
                            defaultValue: '{{presentes}} presentes • {{pendentes}} pendentes',
                            presentes: sala.totalPresentes,
                            pendentes: sala.totalPendentesRetirada,
                          })}
                        </p>
                      </div>
                      {sala.totalAlertasCriticos > 0 && (
                        <Badge className="bg-red-500 hover:bg-red-600">
                          {t('kids.panel.alertCount', {
                            defaultValue: '{{count}} alertas',
                            count: sala.totalAlertasCriticos,
                          })}
                        </Badge>
                      )}
                    </div>
                  </div>
                ))
              ) : (
                <EstadoVazio texto={t('kids.panel.noRooms')} />
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t('kids.preCheckins.pendingList', { defaultValue: 'Pré-check-ins aguardando recepção' })}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {preCheckinsPendentes.length ? (
                preCheckinsPendentes.map((item) => (
                  <div key={item.id} className="rounded-xl border border-border bg-background p-4">
                    <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                      <div className="space-y-2">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-semibold text-foreground">{item.criancaNome}</span>
                          <Badge variant="outline">
                            {t('kids.preCheckins.codeLabel', { defaultValue: 'Código {{code}}', code: item.codigoCurto })}
                          </Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {t('kids.preCheckins.guardianLabel', { defaultValue: 'Responsável: {{name}}', name: item.responsavelNome })}
                        </div>
                        <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
                          <span>{t('kids.preCheckins.createdAt', { defaultValue: 'Criado em {{value}}', value: formatDate(item.criadoEm) })}</span>
                          <span>{t('kids.preCheckins.expiresAt', { defaultValue: 'Expira em {{value}}', value: formatDate(item.expiraEm) })}</span>
                        </div>
                        <div className="flex flex-wrap gap-2">
                          {item.salaId ? <Badge variant="secondary">{t('kids.common.room')}: {item.salaId}</Badge> : null}
                          {item.turmaId ? <Badge variant="secondary">{t('kids.common.class')}: {item.turmaId}</Badge> : null}
                        </div>
                        {item.observacoesResponsavel ? (
                          <p className="text-sm text-foreground/80">
                            {t('kids.preCheckins.familyNote', { defaultValue: 'Observação da família: {{value}}', value: item.observacoesResponsavel })}
                          </p>
                        ) : null}
                      </div>
                      <div className="flex flex-wrap gap-2">
                        <Button
                          size="sm"
                          onClick={() => handleConfirmarPreCheckin(item)}
                          disabled={confirmandoPreCheckinId === item.id || cancelandoPreCheckinId === item.id}
                        >
                          <BadgeCheck className="mr-2 h-4 w-4" />
                          {confirmandoPreCheckinId === item.id
                            ? t('kids.preCheckins.confirming', { defaultValue: 'Confirmando...' })
                            : t('kids.preCheckins.confirmAction', { defaultValue: 'Confirmar entrada' })}
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleCancelarPreCheckin(item)}
                          disabled={confirmandoPreCheckinId === item.id || cancelandoPreCheckinId === item.id}
                        >
                          <Ban className="mr-2 h-4 w-4" />
                          {cancelandoPreCheckinId === item.id
                            ? t('kids.preCheckins.cancelling', { defaultValue: 'Cancelando...' })
                            : t('kids.preCheckins.cancelAction', { defaultValue: 'Cancelar' })}
                        </Button>
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <EstadoVazio texto={t('kids.preCheckins.empty', { defaultValue: 'Nenhum pré-check-in pendente no momento.' })} />
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t('kids.panel.criticalList')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {painel?.alertasCriticos?.length ? (
                painel.alertasCriticos.map((crianca) => (
                  <div
                    key={crianca.criancaPessoaId}
                    className="rounded-xl border border-red-200 bg-red-50 p-4"
                  >
                    <div className="flex items-center gap-2">
                      <TriangleAlert className="h-4 w-4 text-red-600" />
                      <span className="font-semibold text-red-900">{crianca.nome}</span>
                    </div>
                    <p className="mt-2 text-sm text-red-800">
                      {buildCriticalDescription(crianca, t)}
                    </p>
                  </div>
                ))
              ) : (
                <EstadoVazio texto={t('kids.panel.noCriticalAlerts')} />
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t('kids.occurrence.openTitle')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {ocorrenciasAbertas.length ? (
                ocorrenciasAbertas.map((ocorrencia) => (
                  <div key={ocorrencia.id} className="rounded-xl border border-border bg-background p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <Badge className={getOcorrenciaStatusConfig(ocorrencia.status, t).className}>
                            {getOcorrenciaStatusConfig(ocorrencia.status, t).label}
                          </Badge>
                          <span className="font-semibold text-foreground">{ocorrencia.criancaNome}</span>
                        </div>
                        <p className="mt-2 text-sm text-muted-foreground">
                          {formatOcorrenciaTipo(ocorrencia.tipo, t)} • {formatDate(ocorrencia.dataCriacao)}
                        </p>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => carregarHistoricoCrianca({
                          criancaPessoaId: ocorrencia.criancaPessoaId,
                          nome: ocorrencia.criancaNome,
                        })}
                      >
                        <Eye className="mr-2 h-4 w-4" />
                        {t('actions.see')}
                      </Button>
                    </div>
                  </div>
                ))
              ) : (
                <EstadoVazio texto={t('kids.occurrence.noneOpen')} />
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );

  const conteudosContent = (
    <Card>
      <CardHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div>
          <CardTitle>{t('kids.lessonContent.panelTitle', { defaultValue: 'Conteúdo da aula' })}</CardTitle>
          <p className="text-sm text-muted-foreground">
            {t('kids.lessonContent.panelDescription', {
              defaultValue: 'Publique resumo, atividade em casa e materiais para os responsáveis no AppKids.',
            })}
          </p>
        </div>
        <Button onClick={abrirNovoConteudoDialog}>
          <PlusCircle className="mr-2 h-4 w-4" />
          {t('kids.lessonContent.new', { defaultValue: 'Novo conteúdo' })}
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        {conteudosAula.length ? (
          conteudosAula.map((conteudo) => {
            const statusLower = (conteudo.status || '').toLowerCase();
            const isPublished = statusLower === 'published';
            const isArchived = statusLower === 'archived';

            return (
              <div key={conteudo.id} className="rounded-xl border border-border bg-background p-4 shadow-sm">
                <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                  <div className="space-y-3">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold text-foreground">{conteudo.titulo}</span>
                      <Badge className={isPublished ? 'bg-emerald-600 hover:bg-emerald-700' : isArchived ? 'bg-slate-500 hover:bg-slate-600' : 'bg-amber-500 hover:bg-amber-600'}>
                        {conteudo.status}
                      </Badge>
                      {conteudo.tema ? <Badge variant="outline">{conteudo.tema}</Badge> : null}
                      {conteudo.turmaId ? <Badge variant="outline">{conteudo.turmaId}</Badge> : null}
                      {!conteudo.turmaId && conteudo.salaId ? <Badge variant="outline">{conteudo.salaId}</Badge> : null}
                      {!conteudo.turmaId && !conteudo.salaId ? (
                        <Badge variant="outline">{t('kids.lessonContent.generalAudience', { defaultValue: 'Geral do Kids' })}</Badge>
                      ) : null}
                    </div>
                    <p className="max-w-3xl text-sm text-muted-foreground">{conteudo.resumo}</p>
                    <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
                      <span>{t('kids.lessonContent.referenceDateShort', { defaultValue: 'Data' })}: {formatDate(conteudo.dataReferencia)}</span>
                      <span>{t('kids.lessonContent.materialCount', { defaultValue: '{{count}} materiais', count: conteudo.anexos?.length || 0 })}</span>
                      {conteudo.publicadoEm ? (
                        <span>{t('kids.lessonContent.publishedAt', { defaultValue: 'Publicado em {{date}}', date: formatDate(conteudo.publicadoEm) })}</span>
                      ) : null}
                    </div>
                    {conteudo.anexos?.length ? (
                      <div className="flex flex-wrap gap-2">
                        {conteudo.anexos.slice(0, 4).map((anexo) => {
                          const normalized = (anexo.tipo || '').toLowerCase();
                          const previewUrl = anexo.url ? getAbsoluteUrl(anexo.url) : null;
                          return (
                            <button
                              key={`${conteudo.id}-${anexo.id}`}
                              type="button"
                              className="flex items-center gap-2 rounded-lg border border-border bg-muted/20 px-2 py-1 text-left"
                              onClick={() => {
                                const target = anexo.url || anexo.storagePath;
                                if (target) {
                                  window.open(getAbsoluteUrl(target), '_blank');
                                }
                              }}
                            >
                              {normalized === 'imagem' && previewUrl ? (
                                <img src={previewUrl} alt={anexo.nomeExibicao} className="h-10 w-10 rounded object-cover" />
                              ) : (
                                <div className="flex h-10 w-10 items-center justify-center rounded bg-background text-[10px] font-bold text-muted-foreground">
                                  {getConteudoAnexoIconLabel(anexo.tipo)}
                                </div>
                              )}
                              <div className="max-w-[140px]">
                                <div className="truncate text-sm font-medium text-foreground">{anexo.nomeExibicao}</div>
                                <div className="text-xs text-muted-foreground">{anexo.tipo}</div>
                              </div>
                            </button>
                          );
                        })}
                      </div>
                    ) : null}
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" size="sm" onClick={() => abrirEditarConteudoDialog(conteudo)}>
                      <Pencil className="mr-2 h-4 w-4" />
                      {t('actions.edit')}
                    </Button>
                    {!isPublished ? (
                      <Button size="sm" onClick={() => handlePublicarConteudo(conteudo.id)} disabled={publicandoConteudoId === conteudo.id}>
                        <Send className="mr-2 h-4 w-4" />
                        {publicandoConteudoId === conteudo.id
                          ? t('kids.lessonContent.publishing', { defaultValue: 'Publicando...' })
                          : t('kids.lessonContent.publish', { defaultValue: 'Publicar' })}
                      </Button>
                    ) : null}
                    {!isArchived ? (
                      <Button variant="ghost" size="sm" onClick={() => handleArquivarConteudo(conteudo.id)} disabled={arquivandoConteudoId === conteudo.id}>
                        <Ban className="mr-2 h-4 w-4" />
                        {arquivandoConteudoId === conteudo.id
                          ? t('kids.lessonContent.archiving', { defaultValue: 'Arquivando...' })
                          : t('kids.lessonContent.archive', { defaultValue: 'Arquivar' })}
                      </Button>
                    ) : null}
                  </div>
                </div>
              </div>
            );
          })
        ) : (
          <EstadoVazio
            texto={t('kids.lessonContent.empty', {
              defaultValue: 'Nenhum conteúdo da aula foi criado ainda.',
            })}
          />
        )}
      </CardContent>
    </Card>
  );

  const criancasContent = (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-3">
        <ResumoCard
          title={t('kids.children.registeredTitle')}
          value={criancas.length}
          description={t('kids.children.registeredDescription')}
          icon={Users}
          valueClassName="text-blue-600"
        />
        <ResumoCard
          title={t('kids.children.checkedInNowTitle')}
          value={criancas.filter((crianca) => crianca.estaCheckedIn).length}
          description={t('kids.children.checkedInNowDescription')}
          icon={ShieldAlert}
          valueClassName="text-amber-600"
        />
        <ResumoCard
          title={t('kids.children.withCriticalAlertTitle')}
          value={criancas.filter((crianca) => crianca.alergias || crianca.restricoesAlimentares || crianca.observacoes).length}
          description={t('kids.children.withCriticalAlertDescription')}
          icon={TriangleAlert}
          valueClassName="text-rose-600"
        />
      </div>

      <Card>
        <CardHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <CardTitle>{t('kids.children.baseTitle')}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t('kids.children.baseSubtitle')}
            </p>
          </div>
          <Button onClick={() => setCriancaDialogOpen(true)}>
            <PlusCircle className="mr-2 h-4 w-4" />
            {t('kids.children.new')}
          </Button>
        </CardHeader>
        <CardContent className="space-y-3">
          {criancasOrdenadas.length ? (
            criancasOrdenadas.map((crianca) => (
              <div key={crianca.pessoaId} className="rounded-xl border border-border bg-background p-4">
                <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                  <div className="space-y-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold text-foreground">{crianca.nome}</span>
                      {crianca.estaCheckedIn ? (
                        <Badge className="bg-blue-600 hover:bg-blue-700">{t('kids.children.checkedIn')}</Badge>
                      ) : (
                        <Badge variant="outline">{t('kids.children.outOfRoom')}</Badge>
                      )}
                      {crianca.alergias && <Badge variant="destructive">{t('kids.panel.allergy')}</Badge>}
                      {crianca.restricoesAlimentares && <Badge className="bg-amber-600 hover:bg-amber-700">{t('kids.panel.restriction')}</Badge>}
                      {crianca.observacoes && <Badge className="bg-rose-600 hover:bg-rose-700">{t('kids.children.noteBadge')}</Badge>}
                    </div>
                    <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
                      <span>{t('kids.children.roomLabel', { value: crianca.salaId || t('kids.children.noRoom') })}</span>
                      <span>{t('kids.children.classLabel', { value: crianca.turmaId || t('kids.children.noClass') })}</span>
                      <span>{t('kids.children.guardiansLabel', { count: crianca.responsaveis?.length || 0 })}</span>
                    </div>
                    {crianca.responsaveis?.length ? (
                      <div className="flex flex-wrap gap-2 pt-1">
                        {crianca.responsaveis.map((responsavel) => (
                          <Badge key={responsavel.id} variant="outline" className="gap-1">
                            <span>{responsavel.responsavelNome}</span>
                            {responsavel.parentesco ? <span>• {responsavel.parentesco}</span> : null}
                            {responsavel.podeRetirar ? (
                              <span>• {t('kids.children.guardianPickupAllowedShort', { defaultValue: 'retira' })}</span>
                            ) : null}
                          </Badge>
                        ))}
                      </div>
                    ) : null}
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" size="sm" onClick={() => abrirResponsavelDialog(crianca)}>
                      <UserPlus className="mr-2 h-4 w-4" />
                      {t('kids.children.linkGuardianAction', { defaultValue: 'Vincular responsável' })}
                    </Button>
                    <Button variant="outline" size="sm" onClick={() => carregarHistoricoCrianca({ criancaPessoaId: crianca.pessoaId, nome: crianca.nome })}>
                      <Eye className="mr-2 h-4 w-4" />
                      {t('kids.history.view')}
                    </Button>
                  </div>
                </div>
              </div>
            ))
          ) : (
            <EstadoVazio texto={t('kids.children.empty')} />
          )}
        </CardContent>
      </Card>
    </div>
  );

  const estruturaContent = (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-3">
        <ResumoCard
          title={t('kids.structure.activeRoomsTitle')}
          value={salas.filter((sala) => sala.ativo).length}
          description={t('kids.structure.activeRoomsDescription')}
          icon={Building2}
          valueClassName="text-blue-600"
        />
        <ResumoCard
          title={t('kids.structure.activeClassesTitle')}
          value={turmas.filter((turma) => turma.ativo).length}
          description={t('kids.structure.activeClassesDescription')}
          icon={Layers3}
          valueClassName="text-emerald-600"
        />
        <ResumoCard
          title={t('kids.structure.capacityMonitoredTitle')}
          value={salas.filter((sala) => sala.capacidadeMaxima).length}
          description={t('kids.structure.capacityMonitoredDescription')}
          icon={ShieldAlert}
          valueClassName="text-amber-600"
        />
      </div>

      <Card>
        <CardHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <CardTitle>{t('kids.structure.currentTitle')}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t('kids.structure.currentSubtitle')}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => setSalaDialogOpen(true)}>
              <Building2 className="mr-2 h-4 w-4" />
              {t('kids.structure.newRoom')}
            </Button>
            <Button onClick={() => setTurmaDialogOpen(true)}>
              <Layers3 className="mr-2 h-4 w-4" />
              {t('kids.structure.newClass')}
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {salas.length ? (
            salas.map((sala) => (
              <div key={sala.id} className="rounded-xl border border-border bg-background p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-foreground">{sala.nome}</span>
                      <Badge variant="outline">{sala.id}</Badge>
                    </div>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {t('kids.structure.capacityLabel', { value: sala.capacidadeMaxima || t('kids.structure.notDefined') })}
                    </p>
                  </div>
                  <Badge className={sala.ativo ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-slate-500 hover:bg-slate-600'}>
                    {sala.ativo ? t('kids.structure.active') : t('kids.structure.inactive')}
                  </Badge>
                </div>

                <div className="mt-3 space-y-2">
                  {(turmasPorSala[sala.id] || []).length ? (
                    turmasPorSala[sala.id].map((turma) => (
                      <div key={turma.id} className="flex items-center justify-between rounded-lg bg-muted/30 px-3 py-2 text-sm">
                        <div className="flex items-center gap-2">
                          <Pencil className="h-3.5 w-3.5 text-muted-foreground" />
                          <span className="font-medium text-foreground">{turma.nome}</span>
                          <span className="text-muted-foreground">({turma.id})</span>
                        </div>
                        <span className="text-muted-foreground">
                          {t('kids.structure.shortCapacity', { value: turma.capacidadeMaxima || '-' })}
                        </span>
                      </div>
                    ))
                  ) : (
                    <EstadoVazio texto={t('kids.structure.noClassesForRoom')} />
                  )}
                </div>
              </div>
            ))
          ) : (
            <EstadoVazio texto={t('kids.structure.empty')} />
          )}
        </CardContent>
      </Card>
    </div>
  );

  const historicoContent = (
    <div className="space-y-6">
      <Card>
        <CardHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <CardTitle>{t('kids.occurrence.openTitle')}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t('kids.occurrence.historySubtitle')}
            </p>
          </div>
          <Button variant="outline" onClick={() => setOcorrenciaDialogOpen(true)}>
            <PlusCircle className="mr-2 h-4 w-4" />
            {t('kids.occurrence.register')}
          </Button>
        </CardHeader>
        <CardContent className="space-y-3">
          {ocorrenciasAbertas.length ? (
            ocorrenciasAbertas.map((ocorrencia) => (
              <div key={ocorrencia.id} className="rounded-xl border border-border bg-background p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <Badge className={getOcorrenciaStatusConfig(ocorrencia.status, t).className}>
                        {getOcorrenciaStatusConfig(ocorrencia.status, t).label}
                      </Badge>
                      <span className="font-semibold text-foreground">{ocorrencia.criancaNome}</span>
                    </div>
                    <p className="mt-2 text-sm text-muted-foreground">
                      {formatOcorrenciaTipo(ocorrencia.tipo, t)} • {formatDate(ocorrencia.dataCriacao)}
                    </p>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => carregarHistoricoCrianca({
                      criancaPessoaId: ocorrencia.criancaPessoaId,
                      nome: ocorrencia.criancaNome,
                    })}
                  >
                    <Eye className="mr-2 h-4 w-4" />
                    {t('actions.see')}
                  </Button>
                </div>
              </div>
            ))
          ) : (
            <EstadoVazio texto={t('kids.occurrence.noneOpen')} />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Filter className="h-4 w-4" />
            {t('kids.filters')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('kids.search')}</label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={filtros.busca}
                  onChange={(e) => handleFiltroChange('busca', e.target.value)}
                  placeholder={t('kids.searchPlaceholder')}
                  className="pl-9"
                />
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('kids.child')}</label>
              <Select value={filtros.criancaPessoaId || 'todas'} onValueChange={(value) => handleFiltroChange('criancaPessoaId', value === 'todas' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('kids.allChildren')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todas">{t('kids.allChildren')}</SelectItem>
                  {criancas.map((crianca) => (
                    <SelectItem key={crianca.pessoaId} value={String(crianca.pessoaId)}>
                      {crianca.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('kids.status')}</label>
              <Select value={filtros.status || 'todos'} onValueChange={(value) => handleFiltroChange('status', value === 'todos' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('kids.allStatus')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">{t('kids.allStatus')}</SelectItem>
                  <SelectItem value="ativo">{t('kids.active')}</SelectItem>
                  <SelectItem value="finalizado">{t('kids.finished')}</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('kids.dateStart')}</label>
              <Input
                type="date"
                value={filtros.dataInicio}
                onChange={(e) => handleFiltroChange('dataInicio', e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">{t('kids.dateEnd')}</label>
              <Input
                type="date"
                value={filtros.dataFim}
                onChange={(e) => handleFiltroChange('dataFim', e.target.value)}
              />
            </div>
          </div>

          <div className="mt-4 flex justify-end">
            <Button variant="outline" onClick={limparFiltros}>
              {t('kids.clearFilters')}
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('kids.historyTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          {checkinsFiltrados.length ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('kids.table.child')}</TableHead>
                  <TableHead>{t('kids.table.checkin')}</TableHead>
                  <TableHead>{t('kids.table.checkout')}</TableHead>
                  <TableHead>{t('kids.table.duration')}</TableHead>
                  <TableHead>{t('kids.table.method')}</TableHead>
                  <TableHead>{t('kids.table.status')}</TableHead>
                  <TableHead>{t('kids.table.checkinBy')}</TableHead>
                  <TableHead>{t('kids.table.checkoutBy')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {checkinsFiltrados.map((checkin) => (
                  <TableRow key={checkin.id}>
                    <TableCell className="font-medium">{checkin.criancaNome}</TableCell>
                    <TableCell>{formatDate(checkin.checkinTime)}</TableCell>
                    <TableCell>{formatDate(checkin.checkoutTime)}</TableCell>
                    <TableCell>{formatDuration(checkin.checkinTime, checkin.checkoutTime)}</TableCell>
                    <TableCell>{getMetodoBadge(checkin.retiradaMetodo || checkin.metodo)}</TableCell>
                    <TableCell>{getStatusBadge(checkin.status)}</TableCell>
                    <TableCell>{checkin.checkinByNome || '-'}</TableCell>
                    <TableCell>{checkin.checkoutByNome || checkin.retiradaPessoaNome || '-'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <EstadoVazio texto={t('kids.emptyMessage')} />
          )}
        </CardContent>
      </Card>
    </div>
  );

  return (
    <>
      <div className="space-y-6 p-6">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h1 className="text-3xl font-bold text-foreground">
              {sectionConfig.title}
            </h1>
            <p className="mt-1 text-muted-foreground">
              {sectionConfig.subtitle}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            {showPainel && (
              <Select value={filtros.salaId} onValueChange={(value) => handleFiltroChange('salaId', value)}>
                <SelectTrigger className="w-[220px]">
                  <SelectValue placeholder={t('kids.panel.selectRoom')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todas">{t('kids.panel.allRooms')}</SelectItem>
                  {salasDisponiveis.map((sala) => (
                    <SelectItem key={sala} value={sala}>
                      {sala}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
            {showEstrutura && (
              <>
                <Button variant="outline" onClick={() => setSalaDialogOpen(true)}>
                  <Building2 className="mr-2 h-4 w-4" />
                  {t('kids.structure.newRoom')}
                </Button>
                <Button variant="outline" onClick={() => setTurmaDialogOpen(true)}>
                  <Layers3 className="mr-2 h-4 w-4" />
                  {t('kids.structure.newClass')}
                </Button>
              </>
            )}
            {showCriancas && (
              <Button onClick={() => setCriancaDialogOpen(true)}>
                <PlusCircle className="mr-2 h-4 w-4" />
                {t('kids.children.new')}
              </Button>
            )}
            <Button variant="outline" onClick={fetchData}>
              {t('kids.panel.refresh')}
            </Button>
          </div>
        </div>

        {isOverview ? (
          <Tabs value={abaAtiva} onValueChange={setAbaAtiva} className="space-y-4">
            <TabsList>
              <TabsTrigger value="painel">{t('kids.panel.currentTab')}</TabsTrigger>
              <TabsTrigger value="historico">{t('kids.panel.historyTab')}</TabsTrigger>
            </TabsList>

            <TabsContent value="painel" className="space-y-6">
              {painelContent}
            </TabsContent>

            <TabsContent value="historico" className="space-y-6">
              {historicoContent}
            </TabsContent>
          </Tabs>
        ) : (
          <div className="space-y-6">
            {showPainel && painelContent}
            {showCriancas && criancasContent}
            {showEstrutura && estruturaContent}
            {showHistorico && historicoContent}
            {showConteudos && conteudosContent}
          </div>
        )}
      </div>

      <CriancaDialog
        open={criancaDialogOpen}
        onOpenChange={setCriancaDialogOpen}
        form={criancaForm}
        onChange={handleCriancaFormChange}
        onSave={handleCriarCrianca}
        saving={criancaSaving}
        salas={salas}
        turmas={turmas}
      />

      <ResponsavelDialog
        open={responsavelDialogOpen}
        onOpenChange={setResponsavelDialogOpen}
        crianca={criancaResponsavel}
        query={responsavelQuery}
        onQueryChange={setResponsavelQuery}
        onBuscar={buscarResponsaveis}
        searching={buscandoResponsavel}
        resultados={responsavelResultados}
        onSelecionarPessoa={selecionarResponsavel}
        pessoaSelecionada={responsavelSelecionado}
        form={responsavelForm}
        onChange={handleResponsavelFormChange}
        onSave={vincularResponsavel}
        saving={responsavelSaving}
        onDesvincular={desvincularResponsavel}
        desvinculandoId={desvinculandoResponsavelId}
      />

      <ConteudoAulaDialog
        open={conteudoDialogOpen}
        onOpenChange={setConteudoDialogOpen}
        form={conteudoForm}
        onChange={handleConteudoFormChange}
        onAnexoChange={handleConteudoAnexoChange}
        onAddAnexo={adicionarConteudoAnexo}
        onRemoveAnexo={removerConteudoAnexo}
        onSave={handleSalvarConteudo}
        saving={conteudoSaving}
        salas={salas}
        turmas={turmas}
        isEditing={Boolean(conteudoForm.id)}
      />

      <OcorrenciaDialog
        open={ocorrenciaDialogOpen}
        onOpenChange={setOcorrenciaDialogOpen}
        form={ocorrenciaForm}
        onChange={handleOcorrenciaFormChange}
        onSave={handleCriarOcorrencia}
        saving={ocorrenciaSaving}
        criancasPresentes={painel?.criancasPresentes || []}
      />

      <SalaDialog
        open={salaDialogOpen}
        onOpenChange={setSalaDialogOpen}
        form={salaForm}
        onChange={handleSalaFormChange}
        onSave={handleCriarSala}
        saving={salaSaving}
      />

      <TurmaDialog
        open={turmaDialogOpen}
        onOpenChange={setTurmaDialogOpen}
        form={turmaForm}
        onChange={handleTurmaFormChange}
        onSave={handleCriarTurma}
        saving={turmaSaving}
        salas={salas}
      />

      <HistoricoDialog
        open={historicoDialogOpen}
        onOpenChange={setHistoricoDialogOpen}
        criancaHistorico={criancaHistorico}
        historicoLoading={historicoLoading}
        ocorrenciasHistorico={ocorrenciasHistorico}
        historicoUpdatingId={historicoUpdatingId}
        onAtualizarOcorrencia={handleAtualizarOcorrencia}
        formatDate={formatDate}
      />

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
    </>
  );
};

export default KidsCheckinsList;
