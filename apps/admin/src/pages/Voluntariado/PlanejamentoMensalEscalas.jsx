import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, CalendarDays, CheckCircle2, Clock3, Download, Eye, Send, Share2, Users, XCircle } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { cargosApi, equipesApi, escalasApi, eventosApi, uploadApi, voluntariosApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

const SCHEDULE_LOGO_URL = '/images/kingdom-logo-white.png';

function getCurrentMonthValue() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

function getStatusMeta(status, t) {
  const value = Number(status);
  if (value === 2 || value === 5) return { label: t('volunteer.monthlyPlanning.status.confirmed'), className: 'bg-green-100 text-green-800 hover:bg-green-100', icon: CheckCircle2 };
  if (value === 3 || value === 4) return { label: t('volunteer.monthlyPlanning.status.declined'), className: 'bg-red-100 text-red-800 hover:bg-red-100', icon: XCircle };
  if (value === 6) return { label: t('volunteer.monthlyPlanning.status.absent'), className: 'bg-slate-100 text-slate-800 hover:bg-slate-100', icon: AlertTriangle };
  return { label: t('volunteer.monthlyPlanning.status.pending'), className: 'bg-amber-100 text-amber-800 hover:bg-amber-100', icon: Clock3 };
}

function getShortDate(value) {
  return formatDate(value, '-', { day: '2-digit', month: '2-digit' });
}

function drawText(ctx, text, x, y, maxWidth, lineHeight, maxLines = 2) {
  const words = String(text || '').split(/\s+/).filter(Boolean);
  const lines = [];
  let line = '';

  words.forEach((word) => {
    const testLine = line ? `${line} ${word}` : word;
    if (ctx.measureText(testLine).width <= maxWidth) {
      line = testLine;
    } else {
      if (line) lines.push(line);
      line = word;
    }
  });
  if (line) lines.push(line);

  lines.slice(0, maxLines).forEach((currentLine, index) => {
    const suffix = index === maxLines - 1 && lines.length > maxLines ? '...' : '';
    ctx.fillText(`${currentLine}${suffix}`, x, y + index * lineHeight);
  });
}

function fillRoundRect(ctx, x, y, width, height, radius) {
  if (ctx.roundRect) {
    ctx.beginPath();
    ctx.roundRect(x, y, width, height, radius);
    ctx.fill();
    return;
  }

  ctx.beginPath();
  ctx.moveTo(x + radius, y);
  ctx.lineTo(x + width - radius, y);
  ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
  ctx.lineTo(x + width, y + height - radius);
  ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
  ctx.lineTo(x + radius, y + height);
  ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
  ctx.lineTo(x, y + radius);
  ctx.quadraticCurveTo(x, y, x + radius, y);
  ctx.fill();
}

function strokeRoundRect(ctx, x, y, width, height, radius) {
  if (ctx.roundRect) {
    ctx.beginPath();
    ctx.roundRect(x, y, width, height, radius);
    ctx.stroke();
    return;
  }

  ctx.beginPath();
  ctx.moveTo(x + radius, y);
  ctx.lineTo(x + width - radius, y);
  ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
  ctx.lineTo(x + width, y + height - radius);
  ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
  ctx.lineTo(x + radius, y + height);
  ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
  ctx.lineTo(x, y + radius);
  ctx.quadraticCurveTo(x, y, x + radius, y);
  ctx.stroke();
}

function loadCanvasImage(src) {
  return new Promise((resolve) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => resolve(null);
    image.src = src;
  });
}

function drawContainedImage(ctx, image, x, y, maxWidth, maxHeight) {
  if (!image) return;

  const ratio = Math.min(maxWidth / image.width, maxHeight / image.height);
  const width = image.width * ratio;
  const height = image.height * ratio;
  ctx.drawImage(image, x + (maxWidth - width) / 2, y + (maxHeight - height) / 2, width, height);
}

async function generateMonthlyScheduleCanvas({ planejamento, equipeNome, mesLabel, t }) {
  const ocorrencias = planejamento?.ocorrencias || [];
  const voluntarios = (planejamento?.voluntarios || []).filter((voluntario) => voluntario.totalEscalas > 0);
  if (ocorrencias.length === 0 || voluntarios.length === 0) return null;

  const logo = await loadCanvasImage(SCHEDULE_LOGO_URL);
  const nameWidth = 330;
  const totalWidth = 72;
  const occurrenceWidth = 205;
  const padding = 46;
  const headerHeight = 190;
  const tableHeaderHeight = 82;
  const rowHeight = 100;
  const width = padding * 2 + nameWidth + totalWidth + occurrenceWidth * ocorrencias.length;
  const height = padding * 2 + headerHeight + tableHeaderHeight + rowHeight * voluntarios.length + 34;
  const scale = 2;

  const canvas = document.createElement('canvas');
  canvas.width = width * scale;
  canvas.height = height * scale;
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;

  const ctx = canvas.getContext('2d');
  ctx.scale(scale, scale);

  ctx.fillStyle = '#f4f1ea';
  ctx.fillRect(0, 0, width, height);

  const headerGradient = ctx.createLinearGradient(0, 0, width, headerHeight + padding);
  headerGradient.addColorStop(0, '#101714');
  headerGradient.addColorStop(0.55, '#15251f');
  headerGradient.addColorStop(1, '#214235');
  ctx.fillStyle = headerGradient;
  ctx.fillRect(0, 0, width, headerHeight + padding);

  ctx.fillStyle = 'rgba(216, 171, 88, 0.16)';
  fillRoundRect(ctx, width - 420, -80, 520, 250, 120);
  ctx.fillStyle = 'rgba(89, 141, 115, 0.22)';
  fillRoundRect(ctx, width - 520, 84, 380, 140, 70);

  ctx.fillStyle = '#d8ab58';
  fillRoundRect(ctx, padding, 48, 116, 28, 14);
  ctx.fillStyle = '#111714';
  ctx.font = '700 12px Arial';
  ctx.fillText(t('volunteer.monthlyPlanning.share.badge'), padding + 16, 67);

  ctx.fillStyle = '#ffffff';
  ctx.font = '700 46px Arial';
  ctx.fillText(t('volunteer.monthlyPlanning.share.title'), padding, 126);
  ctx.font = '500 21px Arial';
  ctx.fillStyle = '#e7ded0';
  ctx.fillText(`${equipeNome || t('volunteer.monthlyPlanning.share.allTeams')} - ${mesLabel}`, padding, 162);

  ctx.font = '500 14px Arial';
  ctx.fillStyle = '#b9c9bd';
  ctx.fillText(t('volunteer.monthlyPlanning.share.subtitle'), padding, 190);

  drawContainedImage(ctx, logo, width - padding - 154, 42, 154, 120);

  const tableTop = padding + headerHeight - 8;
  const tableLeft = padding;
  ctx.fillStyle = '#ffffff';
  fillRoundRect(ctx, tableLeft, tableTop, width - padding * 2, tableHeaderHeight + rowHeight * voluntarios.length, 18);
  ctx.strokeStyle = '#ddd5c8';
  ctx.lineWidth = 1;
  strokeRoundRect(ctx, tableLeft, tableTop, width - padding * 2, tableHeaderHeight + rowHeight * voluntarios.length, 18);

  ctx.fillStyle = '#efe7dc';
  fillRoundRect(ctx, tableLeft, tableTop, width - padding * 2, tableHeaderHeight, 18);
  ctx.fillStyle = '#25362d';
  ctx.font = '700 18px Arial';
  ctx.fillText(t('volunteer.monthlyPlanning.table.volunteer'), tableLeft + 20, tableTop + 48);
  ctx.fillText(t('volunteer.monthlyPlanning.table.total'), tableLeft + nameWidth + 17, tableTop + 48);

  ocorrencias.forEach((ocorrencia, index) => {
    const x = tableLeft + nameWidth + totalWidth + index * occurrenceWidth;
    ctx.fillStyle = 'rgba(216, 171, 88, 0.26)';
    fillRoundRect(ctx, x + 14, tableTop + 18, 58, 38, 12);
    ctx.font = '700 18px Arial';
    ctx.fillStyle = '#25362d';
    ctx.fillText(getShortDate(ocorrencia.dataHoraInicio), x + 24, tableTop + 43);
    ctx.font = '500 12px Arial';
    ctx.fillStyle = '#6c6a61';
    drawText(ctx, ocorrencia.eventoTitulo, x + 84, tableTop + 34, occurrenceWidth - 96, 15, 2);
  });

  voluntarios.forEach((voluntario, rowIndex) => {
    const y = tableTop + tableHeaderHeight + rowIndex * rowHeight;
    if (rowIndex % 2 === 0) {
      ctx.fillStyle = '#fbfaf7';
      ctx.fillRect(tableLeft, y, width - padding * 2, rowHeight);
    }
    ctx.strokeStyle = '#e7ded0';
    ctx.beginPath();
    ctx.moveTo(tableLeft, y);
    ctx.lineTo(width - padding, y);
    ctx.stroke();

    ctx.fillStyle = '#d8ab58';
    fillRoundRect(ctx, tableLeft + 20, y + 30, 6, 42, 3);
    ctx.font = '700 18px Arial';
    ctx.fillStyle = '#19231e';
    drawText(ctx, voluntario.nome, tableLeft + 38, y + 35, nameWidth - 58, 20, 2);
    ctx.fillStyle = '#e8f3ed';
    fillRoundRect(ctx, tableLeft + nameWidth + 18, y + 30, 36, 36, 18);
    ctx.font = '700 19px Arial';
    ctx.fillStyle = '#1f5f45';
    ctx.textAlign = 'center';
    ctx.fillText(String(voluntario.totalEscalas), tableLeft + nameWidth + 36, y + 54);
    ctx.textAlign = 'left';

    ocorrencias.forEach((ocorrencia, colIndex) => {
      const x = tableLeft + nameWidth + totalWidth + colIndex * occurrenceWidth + 16;
      const alocacoes = (voluntario.alocacoes || []).filter((item) => item.ocorrenciaId === ocorrencia.ocorrenciaId);

      if (alocacoes.length === 0) {
        ctx.font = '500 16px Arial';
        ctx.fillStyle = '#c9c0b3';
        ctx.fillText('-', x, y + 50);
        return;
      }

      alocacoes.slice(0, 2).forEach((alocacao, index) => {
        const offset = index * 40;
        ctx.fillStyle = '#f2eee6';
        fillRoundRect(ctx, x - 2, y + 18 + offset, occurrenceWidth - 28, 32, 10);
        ctx.font = '700 15px Arial';
        ctx.fillStyle = '#213a2e';
        drawText(ctx, alocacao.equipeNome, x + 10, y + 39 + offset, occurrenceWidth - 48, 17, 1);
      });
    });
  });

  ctx.fillStyle = '#756f65';
  ctx.font = '500 13px Arial';
  ctx.fillText(t('volunteer.monthlyPlanning.share.footer'), padding, height - 24);
  ctx.fillStyle = '#d8ab58';
  fillRoundRect(ctx, width - padding - 120, height - 34, 120, 4, 2);

  return canvas;
}

function downloadCanvas(canvas, filename) {
  const link = document.createElement('a');
  link.download = filename;
  link.href = canvas.toDataURL('image/png');
  link.click();
}

function canvasToBlob(canvas) {
  return new Promise((resolve) => {
    canvas.toBlob((blob) => resolve(blob), 'image/png');
  });
}

function createWhatsAppCanvasVariant(canvas) {
  const sourceWidth = Number.parseFloat(canvas.style.width) || canvas.width / 2 || canvas.width;
  const sourceHeight = Number.parseFloat(canvas.style.height) || canvas.height / 2 || canvas.height;
  const maxWidth = 1800;
  const maxHeight = 2400;
  const maxPixels = 2_000_000;
  const ratioByWidth = maxWidth / sourceWidth;
  const ratioByHeight = maxHeight / sourceHeight;
  const ratioByPixels = Math.sqrt(maxPixels / (sourceWidth * sourceHeight));
  const scale = Math.min(1, ratioByWidth, ratioByHeight, ratioByPixels);

  if (!Number.isFinite(scale) || scale >= 1) {
    return canvas;
  }

  const resizedCanvas = document.createElement('canvas');
  resizedCanvas.width = Math.max(1, Math.round(sourceWidth * scale));
  resizedCanvas.height = Math.max(1, Math.round(sourceHeight * scale));

  const ctx = resizedCanvas.getContext('2d');
  ctx.fillStyle = '#f4f1ea';
  ctx.fillRect(0, 0, resizedCanvas.width, resizedCanvas.height);
  ctx.drawImage(canvas, 0, 0, resizedCanvas.width, resizedCanvas.height);
  return resizedCanvas;
}

function canvasToBlobWithOptions(canvas, type = 'image/png', quality) {
  return new Promise((resolve) => {
    canvas.toBlob((blob) => resolve(blob), type, quality);
  });
}

export default function PlanejamentoMensalEscalas() {
  const { t } = useTranslation();
  const [month, setMonth] = useState(getCurrentMonthValue);
  const [eventoId, setEventoId] = useState('all');
  const [equipeId, setEquipeId] = useState('all');
  const [eventos, setEventos] = useState([]);
  const [equipes, setEquipes] = useState([]);
  const [voluntariosEquipe, setVoluntariosEquipe] = useState([]);
  const [cargos, setCargos] = useState([]);
  const [planejamento, setPlanejamento] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [autoGenerating, setAutoGenerating] = useState(false);
  const [sendingWhatsApp, setSendingWhatsApp] = useState(false);
  const [previewingImage, setPreviewingImage] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [previewImageUrl, setPreviewImageUrl] = useState('');
  const [previewImageSize, setPreviewImageSize] = useState({ width: 0, height: 0 });
  const [previewActualSize, setPreviewActualSize] = useState(false);
  const [error, setError] = useState(null);
  const [manualForm, setManualForm] = useState({
    ocorrenciaId: '',
    voluntarioId: '',
    cargoId: '',
  });

  const loadBase = async () => {
    const [eventosRes, equipesRes, cargosRes] = await Promise.all([
      eventosApi.getAll(),
      equipesApi.getAll(),
      cargosApi.getAll(),
    ]);
    setEventos(eventosRes.data || []);
    setEquipes(equipesRes.data || []);
    setCargos(cargosRes.data || []);
  };

  const loadVoluntariosEquipe = async () => {
    if (equipeId === 'all') {
      setVoluntariosEquipe([]);
      setManualForm((prev) => ({ ...prev, voluntarioId: '', cargoId: '' }));
      return;
    }

    const res = await voluntariosApi.getByEquipe(equipeId);
    setVoluntariosEquipe(res.data || []);
  };

  const loadPlanejamento = async ({ silent = false } = {}) => {
    const [ano, mes] = month.split('-').map(Number);
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);

      const res = await escalasApi.getPlanejamentoMensal({
        ano,
        mes,
        eventoId: eventoId === 'all' ? undefined : Number(eventoId),
        equipeId: equipeId === 'all' ? undefined : Number(equipeId),
      });
      setPlanejamento(res.data);
    } catch (err) {
      console.error(err);
      setError(t('volunteer.monthlyPlanning.errorLoad'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadBase().catch((err) => {
      console.error(err);
      setError(t('volunteer.monthlyPlanning.errorLoad'));
      setLoading(false);
    });
  }, []);

  useEffect(() => {
    loadPlanejamento();
  }, [month, eventoId, equipeId]);

  useEffect(() => {
    loadVoluntariosEquipe().catch((err) => {
      console.error(err);
      setVoluntariosEquipe([]);
    });
  }, [equipeId]);

  const ocorrencias = planejamento?.ocorrencias || [];
  const voluntarios = planejamento?.voluntarios || [];

  const alocacoesByPessoaOcorrencia = useMemo(() => {
    const map = new Map();
    voluntarios.forEach((voluntario) => {
      const byOcorrencia = new Map();
      (voluntario.alocacoes || []).forEach((alocacao) => {
        const list = byOcorrencia.get(alocacao.ocorrenciaId) || [];
        list.push(alocacao);
        byOcorrencia.set(alocacao.ocorrenciaId, list);
      });
      map.set(voluntario.pessoaId, byOcorrencia);
    });
    return map;
  }, [voluntarios]);

  if (loading) return <LoadingPage text={t('volunteer.monthlyPlanning.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={loadPlanejamento} />;

  const resumo = planejamento?.resumo || {};
  const equipeSelecionada = equipeId !== 'all';
  const [anoSelecionado, mesSelecionado] = month.split('-').map(Number);

  const handleAutoGenerate = async () => {
    if (!equipeSelecionada) {
      toast.error(t('volunteer.monthlyPlanning.actions.selectTeamRequired'));
      return;
    }

    try {
      setAutoGenerating(true);
      const res = await escalasApi.gerarPlanejamentoMensalAutomatico({
        ano: anoSelecionado,
        mes: mesSelecionado,
        equipeId: Number(equipeId),
        eventoId: eventoId === 'all' ? null : Number(eventoId),
      });
      const avisos = res.data?.avisos || [];
      toast.success(t('volunteer.monthlyPlanning.actions.autoGenerateSuccess', {
        count: res.data?.escalasGeradas ?? 0,
      }));
      if (avisos.length > 0) {
        toast.warning(avisos.slice(0, 2).join(' | '));
      }
      await loadPlanejamento({ silent: true });
    } catch (err) {
      console.error(err);
      toast.error(err.response?.data || t('volunteer.monthlyPlanning.actions.autoGenerateError'));
    } finally {
      setAutoGenerating(false);
    }
  };

  const handleManualAdd = async (event) => {
    event.preventDefault();
    if (!equipeSelecionada || !manualForm.ocorrenciaId || !manualForm.voluntarioId) {
      toast.error(t('volunteer.monthlyPlanning.actions.manualValidation'));
      return;
    }

    const voluntario = voluntariosEquipe.find((item) => String(item.id) === String(manualForm.voluntarioId));

    try {
      setSaving(true);
      await escalasApi.criarAlocacaoPlanejamentoMensal({
        eventoOcorrenciaId: Number(manualForm.ocorrenciaId),
        equipeId: Number(equipeId),
        voluntarioId: Number(manualForm.voluntarioId),
        cargoId: manualForm.cargoId ? Number(manualForm.cargoId) : (voluntario?.cargoId ?? null),
      });
      toast.success(t('volunteer.monthlyPlanning.actions.manualAddSuccess'));
      setManualForm({ ocorrenciaId: '', voluntarioId: '', cargoId: '' });
      await loadPlanejamento({ silent: true });
    } catch (err) {
      console.error(err);
      toast.error(err.response?.data || t('volunteer.monthlyPlanning.actions.manualAddError'));
    } finally {
      setSaving(false);
    }
  };

  const handleRemoveAllocation = async (alocacao) => {
    try {
      setSaving(true);
      await escalasApi.deleteItem(alocacao.escalaId, alocacao.escalaItemId);
      toast.success(t('volunteer.monthlyPlanning.actions.removeSuccess'));
      await loadPlanejamento({ silent: true });
    } catch (err) {
      console.error(err);
      toast.error(err.response?.data || t('volunteer.monthlyPlanning.actions.removeError'));
    } finally {
      setSaving(false);
    }
  };

  const handleScheduleHere = async (ocorrenciaId, voluntario) => {
    const vinculo = voluntariosEquipe.find((item) => String(item.pessoaId) === String(voluntario.pessoaId));
    if (!vinculo) {
      toast.error(t('volunteer.monthlyPlanning.actions.volunteerNotLinkedToTeam'));
      return;
    }

    try {
      setSaving(true);
      await escalasApi.criarAlocacaoPlanejamentoMensal({
        eventoOcorrenciaId: Number(ocorrenciaId),
        equipeId: Number(equipeId),
        voluntarioId: Number(vinculo.id),
        cargoId: vinculo.cargoId ?? null,
      });
      toast.success(t('volunteer.monthlyPlanning.actions.manualAddSuccess'));
      await loadPlanejamento({ silent: true });
    } catch (err) {
      console.error(err);
      toast.error(err.response?.data || t('volunteer.monthlyPlanning.actions.manualAddError'));
    } finally {
      setSaving(false);
    }
  };

  const getShareCanvas = () => {
    const equipeNome = equipes.find((equipe) => String(equipe.id) === String(equipeId))?.nome;
    const mesLabel = formatDate(`${month}-01T00:00:00`, month, { month: 'long', year: 'numeric' });
    return generateMonthlyScheduleCanvas({ planejamento, equipeNome, mesLabel, t });
  };

  const getShareFilename = () => {
    const equipeSlug = (equipes.find((equipe) => String(equipe.id) === String(equipeId))?.nome || 'escala')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-zA-Z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
      .toLowerCase();
    return `escala-${equipeSlug}-${month}.png`;
  };

  const getWhatsAppMessage = () => {
    const equipeNome = equipes.find((equipe) => String(equipe.id) === String(equipeId))?.nome || t('volunteer.monthlyPlanning.share.allTeams');
    const mesLabel = formatDate(`${month}-01T00:00:00`, month, { month: 'long', year: 'numeric' });
    return t('volunteer.monthlyPlanning.whatsapp.message', { team: equipeNome, month: mesLabel });
  };

  const handleDownloadImage = async () => {
    const canvas = await getShareCanvas();
    if (!canvas) {
      toast.error(t('volunteer.monthlyPlanning.share.noData'));
      return;
    }
    downloadCanvas(canvas, getShareFilename());
  };

  const handlePreviewImage = async () => {
    try {
      setPreviewingImage(true);
      const canvas = await getShareCanvas();
      if (!canvas) {
        toast.error(t('volunteer.monthlyPlanning.share.noData'));
        return;
      }

      setPreviewImageUrl(canvas.toDataURL('image/png'));
      setPreviewImageSize({
        width: Number.parseFloat(canvas.style.width) || canvas.width / 2,
        height: Number.parseFloat(canvas.style.height) || canvas.height / 2,
      });
      setPreviewActualSize(false);
      setPreviewOpen(true);
    } catch (err) {
      console.error(err);
      toast.error(t('volunteer.monthlyPlanning.share.error'));
    } finally {
      setPreviewingImage(false);
    }
  };

  const handleShareImage = async () => {
    const canvas = await getShareCanvas();
    if (!canvas) {
      toast.error(t('volunteer.monthlyPlanning.share.noData'));
      return;
    }

    canvas.toBlob(async (blob) => {
      if (!blob) {
        toast.error(t('volunteer.monthlyPlanning.share.error'));
        return;
      }

      const file = new File([blob], getShareFilename(), { type: 'image/png' });
      if (navigator.canShare?.({ files: [file] })) {
        try {
          await navigator.share({
            files: [file],
            title: t('volunteer.monthlyPlanning.share.title'),
          });
        } catch (err) {
          if (err?.name !== 'AbortError') {
            toast.error(t('volunteer.monthlyPlanning.share.error'));
          }
        }
      } else {
        downloadCanvas(canvas, getShareFilename());
        toast.info(t('volunteer.monthlyPlanning.share.downloadedFallback'));
      }
    }, 'image/png');
  };

  const handleSendWhatsApp = async () => {
    if (!equipeSelecionada) {
      toast.error(t('volunteer.monthlyPlanning.actions.selectTeamRequired'));
      return;
    }

    const canvas = await getShareCanvas();
    if (!canvas) {
      toast.error(t('volunteer.monthlyPlanning.share.noData'));
      return;
    }

    try {
      setSendingWhatsApp(true);
      const whatsappCanvas = createWhatsAppCanvasVariant(canvas);
      const blob = await canvasToBlobWithOptions(whatsappCanvas, 'image/jpeg', 0.86);
      if (!blob) {
        toast.error(t('volunteer.monthlyPlanning.share.error'));
        return;
      }

      const formData = new FormData();
      const jpgFilename = getShareFilename().replace(/\.png$/i, '.jpg');
      formData.append('file', new File([blob], jpgFilename, { type: 'image/jpeg' }));
      const uploadRes = await uploadApi.uploadImage(formData);
      const imagemUrl = uploadRes.data?.url || uploadRes.data?.path;
      if (!imagemUrl) {
        toast.error(t('volunteer.monthlyPlanning.whatsapp.uploadError'));
        return;
      }

      const res = await escalasApi.dispararPlanejamentoMensalWhatsApp({
        ano: anoSelecionado,
        mes: mesSelecionado,
        equipeId: Number(equipeId),
        eventoId: eventoId === 'all' ? null : Number(eventoId),
        imagemUrl,
        mensagem: getWhatsAppMessage(),
      });

      const resultado = res.data || {};
      if ((resultado.totalDestinatarios || 0) === 0) {
        toast.warning(t('volunteer.monthlyPlanning.whatsapp.noRecipients'));
        return;
      }

      toast.success(t('volunteer.monthlyPlanning.whatsapp.sent', {
        sent: resultado.totalEnviados || 0,
        total: resultado.totalDestinatarios || 0,
      }));

      if ((resultado.totalFalhas || 0) > 0) {
        toast.warning(t('volunteer.monthlyPlanning.whatsapp.failures', {
          count: resultado.totalFalhas,
          details: (resultado.falhas || []).slice(0, 2).join(' | '),
        }));
      }
    } catch (err) {
      console.error(err);
      toast.error(err.response?.data || t('volunteer.monthlyPlanning.whatsapp.error'));
    } finally {
      setSendingWhatsApp(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.monthlyPlanning.title')}</h1>
          <p className="text-muted-foreground">{t('volunteer.monthlyPlanning.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={handlePreviewImage} disabled={previewingImage}>
            <Eye className="h-4 w-4 mr-2" />
            {previewingImage ? t('volunteer.monthlyPlanning.share.previewing') : t('volunteer.monthlyPlanning.share.preview')}
          </Button>
          <Button variant="outline" onClick={handleDownloadImage}>
            <Download className="h-4 w-4 mr-2" />
            {t('volunteer.monthlyPlanning.share.download')}
          </Button>
          <Button variant="outline" onClick={handleShareImage}>
            <Share2 className="h-4 w-4 mr-2" />
            {t('volunteer.monthlyPlanning.share.share')}
          </Button>
          <Button variant="outline" onClick={handleSendWhatsApp} disabled={!equipeSelecionada || sendingWhatsApp}>
            <Send className="h-4 w-4 mr-2" />
            {sendingWhatsApp ? t('volunteer.monthlyPlanning.whatsapp.sending') : t('volunteer.monthlyPlanning.whatsapp.send')}
          </Button>
          <Button onClick={handleAutoGenerate} disabled={!equipeSelecionada || autoGenerating}>
            {autoGenerating ? t('volunteer.monthlyPlanning.actions.generating') : t('volunteer.monthlyPlanning.actions.autoGenerate')}
          </Button>
          <PageRefreshButton onClick={() => loadPlanejamento({ silent: true })} refreshing={refreshing} />
        </div>
      </div>

      <Dialog open={previewOpen} onOpenChange={setPreviewOpen}>
        <DialogContent className="max-w-[calc(100vw-2rem)] max-h-[calc(100vh-2rem)] overflow-hidden">
          <DialogHeader>
            <DialogTitle>{t('volunteer.monthlyPlanning.share.previewTitle')}</DialogTitle>
          </DialogHeader>
          <div className="max-h-[calc(100vh-12rem)] overflow-auto rounded-md border bg-muted/30 p-3">
            {previewImageUrl && (
              <img
                src={previewImageUrl}
                alt={t('volunteer.monthlyPlanning.share.previewTitle')}
                className={`h-auto rounded-md ${previewActualSize ? 'max-w-none' : 'w-full max-w-full'}`}
                style={{
                  width: previewActualSize && previewImageSize.width ? `${previewImageSize.width}px` : undefined,
                  height: previewActualSize && previewImageSize.height ? `${previewImageSize.height}px` : undefined,
                }}
              />
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setPreviewActualSize((current) => !current)}>
              {previewActualSize
                ? t('volunteer.monthlyPlanning.share.fitPreview')
                : t('volunteer.monthlyPlanning.share.actualSize')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.monthlyPlanning.filtersTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label>{t('volunteer.monthlyPlanning.monthLabel')}</Label>
              <Input type="month" value={month} onChange={(event) => setMonth(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.monthlyPlanning.eventLabel')}</Label>
              <Select value={eventoId} onValueChange={setEventoId}>
                <SelectTrigger>
                  <SelectValue placeholder={t('volunteer.monthlyPlanning.allEvents')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.monthlyPlanning.allEvents')}</SelectItem>
                  {eventos.map((evento) => (
                    <SelectItem key={evento.id} value={String(evento.id)}>{evento.titulo}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t('volunteer.monthlyPlanning.teamLabel')}</Label>
              <Select value={equipeId} onValueChange={setEquipeId}>
                <SelectTrigger>
                  <SelectValue placeholder={t('volunteer.monthlyPlanning.allTeams')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('volunteer.monthlyPlanning.allTeams')}</SelectItem>
                  {equipes.map((equipe) => (
                    <SelectItem key={equipe.id} value={String(equipe.id)}>{equipe.nome}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.monthlyPlanning.manualTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          {!equipeSelecionada ? (
            <p className="text-sm text-muted-foreground">{t('volunteer.monthlyPlanning.manualSelectTeamHint')}</p>
          ) : (
            <form onSubmit={handleManualAdd} className="grid gap-4 md:grid-cols-4">
              <div className="space-y-2">
                <Label>{t('volunteer.monthlyPlanning.manualFields.occurrence')}</Label>
                <Select
                  value={manualForm.ocorrenciaId}
                  onValueChange={(value) => setManualForm((prev) => ({ ...prev, ocorrenciaId: value }))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('volunteer.monthlyPlanning.manualFields.selectOccurrence')} />
                  </SelectTrigger>
                  <SelectContent>
                    {ocorrencias.map((ocorrencia) => (
                      <SelectItem key={ocorrencia.ocorrenciaId} value={String(ocorrencia.ocorrenciaId)}>
                        {getShortDate(ocorrencia.dataHoraInicio)} - {ocorrencia.eventoTitulo}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>{t('volunteer.monthlyPlanning.manualFields.volunteer')}</Label>
                <Select
                  value={manualForm.voluntarioId}
                  onValueChange={(value) => {
                    const voluntario = voluntariosEquipe.find((item) => String(item.id) === String(value));
                    setManualForm((prev) => ({ ...prev, voluntarioId: value, cargoId: voluntario?.cargoId ? String(voluntario.cargoId) : prev.cargoId }));
                  }}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('volunteer.monthlyPlanning.manualFields.selectVolunteer')} />
                  </SelectTrigger>
                  <SelectContent>
                    {voluntariosEquipe.map((voluntario) => (
                      <SelectItem key={voluntario.id} value={String(voluntario.id)}>
                        {voluntario.nome} - {voluntario.nomeCargo || '-'}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>{t('volunteer.monthlyPlanning.manualFields.role')}</Label>
                <Select
                  value={manualForm.cargoId || 'none'}
                  onValueChange={(value) => setManualForm((prev) => ({ ...prev, cargoId: value === 'none' ? '' : value }))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('volunteer.monthlyPlanning.manualFields.selectRole')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">{t('volunteer.monthlyPlanning.manualFields.keepVolunteerRole')}</SelectItem>
                    {cargos.map((cargo) => (
                      <SelectItem key={cargo.id} value={String(cargo.id)}>{cargo.nome}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-end">
                <Button type="submit" disabled={saving}>
                  {saving ? t('actions.saving') : t('volunteer.monthlyPlanning.actions.manualAdd')}
                </Button>
              </div>
            </form>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-5">
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">{t('volunteer.monthlyPlanning.cards.volunteers')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.totalVoluntarios ?? 0}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">{t('volunteer.monthlyPlanning.cards.allocations')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.totalEscalas ?? 0}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">{t('volunteer.monthlyPlanning.cards.withoutSchedule')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.voluntariosSemEscala ?? 0}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">{t('volunteer.monthlyPlanning.cards.overloaded')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.voluntariosComMaisDeDuasEscalas ?? 0}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">{t('volunteer.monthlyPlanning.cards.consecutive')}</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.voluntariosComDomingosConsecutivos ?? 0}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('volunteer.monthlyPlanning.gridTitle')}</CardTitle>
            <Badge variant="outline" className="gap-1">
              <CalendarDays className="h-3.5 w-3.5" />
              {ocorrencias.length} {t('volunteer.monthlyPlanning.occurrences')}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          {ocorrencias.length === 0 ? (
            <PageEmptyState
              title={t('volunteer.monthlyPlanning.emptyOccurrencesTitle')}
              description={t('volunteer.monthlyPlanning.emptyOccurrencesDescription')}
              action={<Button variant="outline" asChild><Link to="/eventos/ocorrencias">{t('volunteer.schedules.goToOccurrences')}</Link></Button>}
            />
          ) : voluntarios.length === 0 ? (
            <PageEmptyState
              title={t('volunteer.monthlyPlanning.emptyVolunteersTitle')}
              description={t('volunteer.monthlyPlanning.emptyVolunteersDescription')}
            />
          ) : (
            <div className="overflow-auto border rounded-md">
              <table className="w-full min-w-[980px] text-sm">
                <thead className="bg-muted/50">
                  <tr>
                    <th className="sticky left-0 z-10 bg-muted text-left p-3 min-w-[240px]">{t('volunteer.monthlyPlanning.table.volunteer')}</th>
                    <th className="text-right p-3 whitespace-nowrap">{t('volunteer.monthlyPlanning.table.total')}</th>
                    {ocorrencias.map((ocorrencia) => (
                      <th key={ocorrencia.ocorrenciaId} className="text-left p-3 min-w-[170px]">
                        <div className="font-semibold">{getShortDate(ocorrencia.dataHoraInicio)}</div>
                        <div className="text-xs font-normal text-muted-foreground truncate">{ocorrencia.eventoTitulo}</div>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {voluntarios.map((voluntario) => (
                    <tr key={voluntario.pessoaId} className="border-t">
                      <td className="sticky left-0 z-10 bg-background p-3 align-top">
                        <div className="font-medium">{voluntario.nome}</div>
                        <div className="mt-1 flex flex-wrap gap-1 text-xs text-muted-foreground">
                          <span className="inline-flex items-center gap-1">
                            <Users className="h-3 w-3" />
                            {(voluntario.equipes || []).join(', ') || '-'}
                          </span>
                        </div>
                        {voluntario.temDomingosConsecutivos && (
                          <Badge className="mt-2 bg-amber-100 text-amber-800 hover:bg-amber-100">
                            {t('volunteer.monthlyPlanning.alerts.consecutive')}
                          </Badge>
                        )}
                      </td>
                      <td className="p-3 text-right align-top font-semibold">{voluntario.totalEscalas}</td>
                      {ocorrencias.map((ocorrencia) => {
                        const alocacoes = alocacoesByPessoaOcorrencia.get(voluntario.pessoaId)?.get(ocorrencia.ocorrenciaId) || [];
                        return (
                          <td key={ocorrencia.ocorrenciaId} className="p-3 align-top">
                            {alocacoes.length === 0 ? (
                              equipeSelecionada ? (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="h-7 px-2"
                                  onClick={() => handleScheduleHere(ocorrencia.ocorrenciaId, voluntario)}
                                  disabled={saving}
                                >
                                  {t('volunteer.monthlyPlanning.actions.scheduleHere')}
                                </Button>
                              ) : (
                                <span className="text-muted-foreground">-</span>
                              )
                            ) : (
                              <div className="space-y-2">
                                {alocacoes.map((alocacao) => {
                                  const meta = getStatusMeta(alocacao.status, t);
                                  const StatusIcon = meta.icon;
                                  return (
                                    <div key={alocacao.escalaItemId} className="space-y-1">
                                      <div className="font-medium">{alocacao.equipeNome}</div>
                                      <div className="text-xs text-muted-foreground">{alocacao.cargoNome || '-'}</div>
                                      <Badge className={`${meta.className} gap-1`}>
                                        <StatusIcon className="h-3 w-3" />
                                        {meta.label}
                                      </Badge>
                                      <div>
                                        <Button variant="ghost" size="sm" className="h-7 px-2" asChild>
                                          <Link to={`/voluntariado/escalas/ocorrencia/${alocacao.ocorrenciaId}/equipe/${alocacao.equipeId}`}>
                                            <Eye className="h-3.5 w-3.5 mr-1" />
                                            {t('actions.see')}
                                          </Link>
                                        </Button>
                                        <Button
                                          variant="ghost"
                                          size="sm"
                                          className="h-7 px-2 text-destructive"
                                          onClick={() => handleRemoveAllocation(alocacao)}
                                          disabled={saving}
                                        >
                                          {t('actions.remove')}
                                        </Button>
                                      </div>
                                    </div>
                                  );
                                })}
                              </div>
                            )}
                          </td>
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
