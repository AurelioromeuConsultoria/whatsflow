import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, PlusCircle, Save, Trash2, Pencil, ChevronUp, ChevronDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { ImageUpload } from '@/components/ImageUpload';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { eventosApi, eventosRecorrenciasApi, normalizeEvento } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

const TIPOS_EVENTO = [1, 2, 3, 4];
const DIAS_SEMANA = [0, 1, 2, 3, 4, 5, 6];
const PERIODICIDADE = [1, 2, 3];
const SEMANAS_MES = [1, 2, 3, 4, 5];
const CAMPOS_FORMULARIO_PADRAO = [
  { slug: 'nome', labelKey: 'name', tipo: 'texto', obrigatorio: true },
  { slug: 'whatsApp', labelKey: 'whatsapp', tipo: 'texto', obrigatorio: true },
  { slug: 'email', labelKey: 'email', tipo: 'texto', obrigatorio: false },
  { slug: 'observacoes', labelKey: 'notes', tipo: 'texto', obrigatorio: false },
];
const TIPOS_CAMPO = ['texto', 'numero', 'email', 'tel'];

const parseDateOnly = (value) => {
  if (!value) return null;
  const [year, month, day] = String(value).slice(0, 10).split('-').map(Number);
  if (!year || !month || !day) return null;
  return new Date(year, month - 1, day);
};

const formatDateOnly = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getWeekdayOrdinalFromDate = (value, diaSemana) => {
  const date = parseDateOnly(value);
  if (!date || date.getDay() !== Number(diaSemana)) return 1;
  return Math.floor((date.getDate() - 1) / 7) + 1;
};

const getDateForWeekdayOrdinal = (year, monthIndex, diaSemana, semanaDoMes) => {
  const first = new Date(year, monthIndex, 1);
  const offset = (Number(diaSemana) - first.getDay() + 7) % 7;
  const date = new Date(year, monthIndex, 1 + offset + (Number(semanaDoMes) - 1) * 7);
  return date.getMonth() === monthIndex ? date : null;
};

const getMonthlyAnchorDate = (dataInicioVigencia, diaSemana, semanaDoMes) => {
  const start = parseDateOnly(dataInicioVigencia) || new Date();
  let year = start.getFullYear();
  let monthIndex = start.getMonth();

  for (let i = 0; i < 24; i += 1) {
    const candidate = getDateForWeekdayOrdinal(year, monthIndex, diaSemana, semanaDoMes);
    if (candidate && candidate >= start) return formatDateOnly(candidate);
    monthIndex += 1;
    if (monthIndex > 11) {
      monthIndex = 0;
      year += 1;
    }
  }

  return formatDateOnly(start);
};

export default function EventoForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();
  const confirmDialog = useConfirmDialog();

  const [formData, setFormData] = useState({
    titulo: '',
    descricao: '',
    imagemDestaque: '',
    url: '',
    dataInicio: '',
    dataFim: '',
    tipo: 1,
    ehRecorrente: false,
    ativo: true,
    aceitaInscricoes: false,
  });
  /** Lista de campos do formulário de inscrição: { slug, label, tipo, obrigatorio } */
  const [camposFormulario, setCamposFormulario] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const [recorrencias, setRecorrencias] = useState([]);
  const [loadingRecorrencias, setLoadingRecorrencias] = useState(false);
  const [showRecorrenciaForm, setShowRecorrenciaForm] = useState(false);
  const [editingRecorrenciaId, setEditingRecorrenciaId] = useState(null);
  const [recorrenciaForm, setRecorrenciaForm] = useState({
    diaSemana: 0,
    horaInicio: '10:00',
    horaFim: '',
    periodicidade: 1,
    semanaDoMes: 1,
    dataInicioVigencia: new Date().toISOString().slice(0, 10),
    dataFimVigencia: '',
    ativo: true,
  });

  const tiposEvento = TIPOS_EVENTO.map((value) => ({
    value,
    label: t(`events.type.${value === 1 ? 'event' : value === 2 ? 'service' : value === 3 ? 'meeting' : 'other'}`),
  }));
  const diasSemana = DIAS_SEMANA.map((value) => ({
    value,
    label: t(`events.form.recurrence.days.${value}`),
  }));
  const periodicidades = PERIODICIDADE.map((value) => ({
    value,
    label: t(`events.form.recurrence.frequency.${value === 1 ? 'weekly' : value === 2 ? 'biweekly' : 'monthly'}`),
  }));
  const semanasMes = SEMANAS_MES.map((value) => ({
    value,
    label: t(`events.form.recurrence.monthWeek.${value}`),
  }));
  const tiposCampo = TIPOS_CAMPO.map((value) => ({
    value,
    label: t(`events.form.registration.fieldTypes.${value}`),
  }));

  const getCamposPadrao = () => CAMPOS_FORMULARIO_PADRAO.map((c) => ({
    slug: c.slug,
    label: t(`events.form.registration.defaultFields.${c.labelKey}`),
    tipo: c.tipo,
    obrigatorio: c.obrigatorio,
  }));

  // Considera data vazia se for null/undefined ou data default do backend (ex: 0001-01-01)
  const toDateTimeLocal = (value) => {
    if (!value) return '';
    const d = new Date(value);
    if (isNaN(d.getTime()) || d.getFullYear() < 1900) return '';
    return d.toISOString().slice(0, 16);
  };

  const load = async () => {
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await eventosApi.getById(id);
      const e = normalizeEvento(res.data);
      if (!e) {
        setError(t('events.form.notFound'));
        return;
      }
      setFormData({
        titulo: e.titulo || '',
        descricao: e.descricao || '',
        imagemDestaque: e.imagemDestaque || '',
        url: e.url || '',
        dataInicio: toDateTimeLocal(e.dataInicio),
        dataFim: toDateTimeLocal(e.dataFim),
        tipo: e.tipo ?? 1,
        ehRecorrente: e.ehRecorrente ?? false,
        ativo: e.ativo ?? true,
        aceitaInscricoes: e.aceitaInscricoes ?? false,
      });
      // Campos do formulário de inscrição (JSON)
      let campos = getCamposPadrao();
      if (e.configuracaoFormularioInscricao && typeof e.configuracaoFormularioInscricao === 'string') {
        try {
          const parsed = JSON.parse(e.configuracaoFormularioInscricao);
          if (Array.isArray(parsed) && parsed.length > 0) campos = parsed;
        } catch (_) { /* mantém padrão */ }
      }
      setCamposFormulario(campos.map((c) => ({
        slug: c.slug || '',
        label: c.label || '',
        tipo: c.tipo || 'texto',
        obrigatorio: Boolean(c.obrigatorio),
      })));
    } catch (err) {
      setError(t('events.form.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadRecorrencias = async () => {
    if (!id) return;
    try {
      setLoadingRecorrencias(true);
      const res = await eventosRecorrenciasApi.getByEvento(id);
      setRecorrencias(res.data || []);
    } catch (err) {
      console.error(err);
      setRecorrencias([]);
    } finally {
      setLoadingRecorrencias(false);
    }
  };

  useEffect(() => { load(); }, [id]);
  useEffect(() => {
    if (!isEditing) setCamposFormulario([]);
  }, [isEditing]);
  useEffect(() => {
    if (isEditing && formData.ehRecorrente) loadRecorrencias();
    else setRecorrencias([]);
  }, [id, isEditing, formData.ehRecorrente]);

  const handleChange = (e) => {
    const { name, value, type } = e.target;
    const next = type === 'checkbox' ? e.target.checked : value;
    setFormData((prev) => ({ ...prev, [name]: next }));
  };

  const usarCamposPadrao = () => {
    setCamposFormulario(getCamposPadrao());
  };

  const addCampoFormulario = () => {
    setCamposFormulario((prev) => [...prev, { slug: '', label: '', tipo: 'texto', obrigatorio: false }]);
  };

  const updateCampoFormulario = (index, field, value) => {
    setCamposFormulario((prev) => {
      const next = [...prev];
      next[index] = { ...next[index], [field]: value };
      if (field === 'label' && !next[index].slug) {
        next[index].slug = value.toLowerCase().replace(/\s+/g, '').replace(/[^a-z0-9]/gi, '') || '';
      }
      return next;
    });
  };

  const removeCampoFormulario = (index) => {
    setCamposFormulario((prev) => prev.filter((_, i) => i !== index));
  };

  const moveCampoFormulario = (index, dir) => {
    if (dir < 0 && index <= 0) return;
    if (dir > 0 && index >= camposFormulario.length - 1) return;
    const next = [...camposFormulario];
    const swap = index + dir;
    [next[index], next[swap]] = [next[swap], next[index]];
    setCamposFormulario(next);
  };

  const openNovaRecorrencia = () => {
    setEditingRecorrenciaId(null);
    setRecorrenciaForm({
      diaSemana: 0,
      horaInicio: '10:00',
      horaFim: '',
      periodicidade: 1,
      semanaDoMes: 1,
      dataInicioVigencia: new Date().toISOString().slice(0, 10),
      dataFimVigencia: '',
      ativo: true,
    });
    setShowRecorrenciaForm(true);
  };

  const openEditRecorrencia = (r) => {
    setEditingRecorrenciaId(r.id);
    setRecorrenciaForm({
      diaSemana: r.diaSemana,
      horaInicio: r.horaInicio || '10:00',
      horaFim: r.horaFim || '',
      periodicidade: r.periodicidade,
      semanaDoMes: getWeekdayOrdinalFromDate(r.dataInicioVigencia, r.diaSemana),
      dataInicioVigencia: r.dataInicioVigencia?.slice(0, 10) || new Date().toISOString().slice(0, 10),
      dataFimVigencia: r.dataFimVigencia?.slice(0, 10) || '',
      ativo: r.ativo ?? true,
    });
    setShowRecorrenciaForm(true);
  };

  const cancelRecorrenciaForm = () => {
    setShowRecorrenciaForm(false);
    setEditingRecorrenciaId(null);
  };

  const saveRecorrencia = async () => {
    const periodicidade = Number(recorrenciaForm.periodicidade);
    const dataInicioVigencia = periodicidade === 3
      ? getMonthlyAnchorDate(recorrenciaForm.dataInicioVigencia, recorrenciaForm.diaSemana, recorrenciaForm.semanaDoMes)
      : recorrenciaForm.dataInicioVigencia;
    const base = {
      diaSemana: Number(recorrenciaForm.diaSemana),
      horaInicio: recorrenciaForm.horaInicio || '10:00',
      horaFim: recorrenciaForm.horaFim || null,
      periodicidade,
      dataInicioVigencia: dataInicioVigencia ? new Date(`${dataInicioVigencia}T00:00:00`).toISOString() : new Date().toISOString(),
      dataFimVigencia: recorrenciaForm.dataFimVigencia ? new Date(recorrenciaForm.dataFimVigencia).toISOString() : null,
      ativo: recorrenciaForm.ativo,
    };
    try {
      if (editingRecorrenciaId) {
        await eventosRecorrenciasApi.update(id, editingRecorrenciaId, base);
        toast.success(t('events.form.recurrence.updated'));
      } else {
        await eventosRecorrenciasApi.create(id, { ...base, eventoId: Number(id) });
        toast.success(t('events.form.recurrence.created'));
      }
      cancelRecorrenciaForm();
      await loadRecorrencias();
    } catch (err) {
      toast.error(err.response?.data || t('events.form.recurrence.errorSave'));
    }
  };

  const getPeriodicidadeRecorrenciaLabel = (recorrencia) => {
    if (Number(recorrencia.periodicidade) !== 3) {
      return recorrencia.periodicidadeDescricao ?? periodicidades.find((p) => p.value === recorrencia.periodicidade)?.label;
    }

    const semanaDoMes = getWeekdayOrdinalFromDate(recorrencia.dataInicioVigencia, recorrencia.diaSemana);
    const diaSemana = recorrencia.diaSemanaDescricao ?? diasSemana.find((d) => d.value === recorrencia.diaSemana)?.label;
    return t('events.form.recurrence.frequency.monthlyOrdinal', {
      week: t(`events.form.recurrence.monthWeek.${semanaDoMes}`),
      day: diaSemana,
    });
  };

  const deleteRecorrencia = (recId) => {
    confirmDialog.show({
      title: t('events.form.recurrence.confirmDeleteTitle'),
      description: t('events.form.recurrence.confirmDelete'),
      confirmText: t('actions.remove'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await eventosRecorrenciasApi.delete(id, recId);
          toast.success(t('events.form.recurrence.deleted'));
          await loadRecorrencias();
        } catch (err) {
          toast.error(err.response?.data || t('events.form.recurrence.errorDelete'));
          throw err;
        }
      },
    });
  };

  // Função para normalizar URL (adiciona https:// se não tiver protocolo, mas preserva URLs relativas)
  const normalizeUrl = (url) => {
    if (!url || !url.trim()) return null;
    const trimmed = url.trim();
    // Se já tiver protocolo, retorna como está
    if (trimmed.match(/^https?:\/\//i)) {
      return trimmed;
    }
    // Se começar com /, é URL relativa interna - não adicionar protocolo
    if (trimmed.startsWith('/')) {
      return trimmed;
    }
    // Se não tiver protocolo e não for relativa, adiciona https://
    return `https://${trimmed}`;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      setLoading(true);
      const dataInicio = formData.dataInicio ? new Date(formData.dataInicio).toISOString() : null;
      const dataFim = formData.dataFim ? new Date(formData.dataFim).toISOString() : dataInicio;
      const payload = {
        titulo: formData.titulo.trim() || '',
        descricao: formData.descricao.trim() || null,
        imagemDestaque: formData.imagemDestaque.trim() || null,
        url: normalizeUrl(formData.url),
        dataInicio,
        dataFim,
        tipo: Number(formData.tipo),
        ehRecorrente: Boolean(formData.ehRecorrente),
        ativo: Boolean(formData.ativo),
        aceitaInscricoes: Boolean(formData.aceitaInscricoes),
      };
      const camposValidos = camposFormulario.filter((c) => (c.slug || '').trim() && (c.label || '').trim());
      if (formData.aceitaInscricoes && camposValidos.length > 0) {
        payload.configuracaoFormularioInscricao = JSON.stringify(camposValidos.map((c) => ({
          slug: String(c.slug).trim(),
          label: String(c.label).trim(),
          tipo: c.tipo || 'texto',
          obrigatorio: Boolean(c.obrigatorio),
        })));
      } else {
        payload.configuracaoFormularioInscricao = null;
      }
      // Backend espera o body direto (não dentro de "dto"); DataFim obrigatório → usa dataInicio quando vazio
      if (isEditing) await eventosApi.update(id, payload);
      else await eventosApi.create(payload);
      toast.success(isEditing ? t('events.form.updated') : t('events.form.created'));
      navigate('/eventos');
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('events.form.errorSave');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('events.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/eventos">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('events.edit') : t('events.new')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('events.form.editSubtitle') : t('events.form.createSubtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('events.edit') : t('events.create')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="titulo">{t('events.fields.title')} *</Label>
                <Input id="titulo" name="titulo" value={formData.titulo} onChange={handleChange} placeholder={t('events.form.placeholders.title')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="url">{t('events.fields.url')}</Label>
                <Input 
                  id="url" 
                  name="url" 
                  type="text" 
                  value={formData.url} 
                  onChange={handleChange} 
                  placeholder={t('events.form.placeholders.url')} 
                />
                {formData.url && !formData.url.match(/^https?:\/\//i) && (
                  <p className="text-xs text-muted-foreground">
                    {t('events.form.urlHint')}
                  </p>
                )}
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="descricao">{t('events.fields.description')}</Label>
              <Textarea id="descricao" name="descricao" value={formData.descricao} onChange={handleChange} placeholder={t('events.form.placeholders.description')} rows={4} />
            </div>

            <div className="space-y-2">
              <ImageUpload
                label={t('events.form.featuredImage')}
                value={formData.imagemDestaque}
                onChange={(url) => setFormData((prev) => ({ ...prev, imagemDestaque: url }))}
                accept="image/*"
                type="image"
              />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="dataInicio">{t('events.fields.startDate')} *</Label>
                <Input id="dataInicio" name="dataInicio" type="datetime-local" value={formData.dataInicio} onChange={handleChange} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataFim">{t('events.fields.endDate')}</Label>
                <Input id="dataFim" name="dataFim" type="datetime-local" value={formData.dataFim} onChange={handleChange} />
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-3">
              <div className="space-y-2">
                <Label>{t('events.fields.type')}</Label>
                <Select value={String(formData.tipo)} onValueChange={(v) => setFormData((p) => ({ ...p, tipo: Number(v) }))}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {tiposEvento.map((opt) => (
                      <SelectItem key={opt.value} value={String(opt.value)}>{opt.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-center gap-2 pt-8">
                <input type="checkbox" id="ehRecorrente" name="ehRecorrente" checked={formData.ehRecorrente} onChange={handleChange} className="rounded border-input" />
                <Label htmlFor="ehRecorrente" className="cursor-pointer">{t('events.form.recurringToggle')}</Label>
              </div>
              <div className="flex items-center gap-2 pt-8">
                <input type="checkbox" id="ativo" name="ativo" checked={formData.ativo} onChange={handleChange} className="rounded border-input" />
                <Label htmlFor="ativo" className="cursor-pointer">{t('events.form.activeToggle')}</Label>
              </div>
              <div className="flex items-center gap-2 pt-8">
                <input type="checkbox" id="aceitaInscricoes" name="aceitaInscricoes" checked={formData.aceitaInscricoes} onChange={handleChange} className="rounded border-input" />
                <Label htmlFor="aceitaInscricoes" className="cursor-pointer">{t('events.form.acceptRegistrations')}</Label>
              </div>
            </div>

            {formData.aceitaInscricoes && (
              <div className="rounded-lg border p-4 space-y-3 bg-muted/20">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <h4 className="font-medium">{t('events.form.registration.title')}</h4>
                    <p className="text-sm text-muted-foreground">
                      {t('events.form.registration.description')}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Button type="button" variant="outline" size="sm" onClick={usarCamposPadrao}>
                      {t('events.form.registration.useDefaults')}
                    </Button>
                    <Button type="button" variant="outline" size="sm" onClick={addCampoFormulario}>
                      <PlusCircle className="h-4 w-4 mr-1" /> {t('events.form.registration.addField')}
                    </Button>
                  </div>
                </div>
                {camposFormulario.length === 0 ? (
                  <p className="text-sm text-muted-foreground">{t('events.form.registration.empty')}</p>
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-10" />
                        <TableHead>{t('events.form.registration.table.slug')}</TableHead>
                        <TableHead>{t('events.form.registration.table.label')}</TableHead>
                        <TableHead>{t('events.form.registration.table.type')}</TableHead>
                        <TableHead className="w-24">{t('events.form.registration.table.required')}</TableHead>
                        <TableHead className="w-20">{t('events.form.registration.table.actions')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {camposFormulario.map((campo, idx) => (
                        <TableRow key={idx}>
                          <TableCell className="p-1">
                            <div className="flex flex-col gap-0">
                              <Button type="button" variant="ghost" size="icon" className="h-6 w-6" onClick={() => moveCampoFormulario(idx, -1)} disabled={idx === 0} title={t('events.form.registration.moveUp')}>
                                <ChevronUp className="h-4 w-4" />
                              </Button>
                              <Button type="button" variant="ghost" size="icon" className="h-6 w-6" onClick={() => moveCampoFormulario(idx, 1)} disabled={idx === camposFormulario.length - 1} title={t('events.form.registration.moveDown')}>
                                <ChevronDown className="h-4 w-4" />
                              </Button>
                            </div>
                          </TableCell>
                          <TableCell>
                            <Input
                              className="h-8 font-mono text-sm"
                              placeholder={t('events.form.registration.slugPlaceholder')}
                              value={campo.slug}
                              onChange={(e) => updateCampoFormulario(idx, 'slug', e.target.value)}
                            />
                          </TableCell>
                          <TableCell>
                            <Input
                              className="h-8"
                              placeholder={t('events.form.registration.labelPlaceholder')}
                              value={campo.label}
                              onChange={(e) => updateCampoFormulario(idx, 'label', e.target.value)}
                            />
                          </TableCell>
                          <TableCell>
                            <Select value={campo.tipo} onValueChange={(v) => updateCampoFormulario(idx, 'tipo', v)}>
                              <SelectTrigger className="h-8"><SelectValue /></SelectTrigger>
                              <SelectContent>
                                {tiposCampo.map((fieldType) => (
                                  <SelectItem key={fieldType.value} value={fieldType.value}>{fieldType.label}</SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          </TableCell>
                          <TableCell>
                            <input
                              type="checkbox"
                              checked={campo.obrigatorio}
                              onChange={(e) => updateCampoFormulario(idx, 'obrigatorio', e.target.checked)}
                              className="rounded border-input"
                            />
                          </TableCell>
                          <TableCell>
                            <Button type="button" variant="ghost" size="icon" className="h-8 w-8 text-destructive" onClick={() => removeCampoFormulario(idx)} title={t('events.form.registration.remove')}>
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </div>
            )}

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/eventos">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {isEditing && formData.ehRecorrente && (
        <Card>
          <CardHeader>
            <CardTitle>{t('events.form.recurrence.sectionTitle')}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t('events.form.recurrence.sectionDescription')}
            </p>
            <div className="pt-2">
              <Button type="button" variant="outline" size="sm" onClick={openNovaRecorrencia}>
                <PlusCircle className="h-4 w-4 mr-2" /> {t('events.form.recurrence.new')}
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {showRecorrenciaForm && (
              <div className="rounded-lg border p-4 space-y-4 bg-muted/30">
                <h4 className="font-medium">{editingRecorrenciaId ? t('events.form.recurrence.editTitle') : t('events.form.recurrence.newTitle')}</h4>
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                  <div className="space-y-2">
                    <Label>{t('events.form.recurrence.dayOfWeek')}</Label>
                    <Select value={String(recorrenciaForm.diaSemana)} onValueChange={(v) => setRecorrenciaForm((p) => ({ ...p, diaSemana: Number(v) }))}>
                      <SelectTrigger><SelectValue /></SelectTrigger>
                      <SelectContent>
                        {diasSemana.map((d) => (
                          <SelectItem key={d.value} value={String(d.value)}>{d.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label>{t('events.form.recurrence.startTime')}</Label>
                    <Input type="time" value={recorrenciaForm.horaInicio} onChange={(e) => setRecorrenciaForm((p) => ({ ...p, horaInicio: e.target.value }))} />
                  </div>
                  <div className="space-y-2">
                    <Label>{t('events.form.recurrence.endTime')}</Label>
                    <Input type="time" value={recorrenciaForm.horaFim} onChange={(e) => setRecorrenciaForm((p) => ({ ...p, horaFim: e.target.value }))} />
                  </div>
                  <div className="space-y-2">
                    <Label>{t('events.form.recurrence.frequencyLabel')}</Label>
                    <Select value={String(recorrenciaForm.periodicidade)} onValueChange={(v) => setRecorrenciaForm((p) => ({ ...p, periodicidade: Number(v) }))}>
                      <SelectTrigger><SelectValue /></SelectTrigger>
                      <SelectContent>
                        {periodicidades.map((frequency) => (
                          <SelectItem key={frequency.value} value={String(frequency.value)}>{frequency.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  {Number(recorrenciaForm.periodicidade) === 3 && (
                    <div className="space-y-2">
                      <Label>{t('events.form.recurrence.monthWeekLabel')}</Label>
                      <Select value={String(recorrenciaForm.semanaDoMes)} onValueChange={(v) => setRecorrenciaForm((p) => ({ ...p, semanaDoMes: Number(v) }))}>
                        <SelectTrigger><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {semanasMes.map((week) => (
                            <SelectItem key={week.value} value={String(week.value)}>{week.label}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  )}
                  <div className="space-y-2">
                    <Label>{t('events.form.recurrence.validFrom')}</Label>
                    <Input type="date" value={recorrenciaForm.dataInicioVigencia} onChange={(e) => setRecorrenciaForm((p) => ({ ...p, dataInicioVigencia: e.target.value }))} />
                  </div>
                  <div className="space-y-2">
                    <Label>{t('events.form.recurrence.validUntil')}</Label>
                    <Input type="date" value={recorrenciaForm.dataFimVigencia} onChange={(e) => setRecorrenciaForm((p) => ({ ...p, dataFimVigencia: e.target.value }))} />
                  </div>
                  <div className="flex items-center gap-2 pt-8">
                    <input type="checkbox" checked={recorrenciaForm.ativo} onChange={(e) => setRecorrenciaForm((p) => ({ ...p, ativo: e.target.checked }))} className="rounded border-input" />
                    <Label>{t('events.form.activeToggle')}</Label>
                  </div>
                </div>
                <div className="flex gap-2">
                  <Button type="button" size="sm" onClick={saveRecorrencia}>{t('actions.save')}</Button>
                  <Button type="button" size="sm" variant="outline" onClick={cancelRecorrenciaForm}>{t('actions.cancel')}</Button>
                </div>
              </div>
            )}

            {loadingRecorrencias ? (
              <p className="text-sm text-muted-foreground">{t('events.form.recurrence.loading')}</p>
            ) : recorrencias.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t('events.form.recurrence.empty')}</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('events.form.recurrence.table.day')}</TableHead>
                    <TableHead>{t('events.form.recurrence.table.startTime')}</TableHead>
                    <TableHead>{t('events.form.recurrence.table.endTime')}</TableHead>
                    <TableHead>{t('events.form.recurrence.table.frequency')}</TableHead>
                    <TableHead>{t('events.form.recurrence.table.validity')}</TableHead>
                    <TableHead>{t('events.form.recurrence.table.active')}</TableHead>
                    <TableHead className="w-[100px]">{t('events.form.recurrence.table.actions')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {recorrencias.map((r) => (
                    <TableRow key={r.id}>
                      <TableCell>{r.diaSemanaDescricao ?? diasSemana.find((d) => d.value === r.diaSemana)?.label}</TableCell>
                      <TableCell>{r.horaInicio}</TableCell>
                      <TableCell>{r.horaFim || t('events.form.recurrence.noEndTime')}</TableCell>
                      <TableCell>{getPeriodicidadeRecorrenciaLabel(r)}</TableCell>
                      <TableCell>
                        {r.dataInicioVigencia?.slice(0, 10)} {r.dataFimVigencia ? t('events.form.recurrence.validityUntil', { date: r.dataFimVigencia.slice(0, 10) }) : t('events.form.recurrence.noEndDate')}
                      </TableCell>
                      <TableCell>{r.ativo ? t('events.form.recurrence.yes') : t('events.form.recurrence.no')}</TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button type="button" variant="ghost" size="icon" onClick={() => openEditRecorrencia(r)} title={t('actions.edit')}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button type="button" variant="ghost" size="icon" onClick={() => deleteRecorrencia(r.id)} title={t('events.deleteConfirm')}>
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}

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
    </div>
  );
}
