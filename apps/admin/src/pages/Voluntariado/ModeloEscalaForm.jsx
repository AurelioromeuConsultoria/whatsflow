import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link, useSearchParams } from 'react-router-dom';
import { ArrowLeft, Save, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { escalasModelosApi, equipesApi, eventosApi, cargosApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function ModeloEscalaForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const equipeIdFromQuery = searchParams.get('equipeId');
  const isEditing = Boolean(id);

  const [formData, setFormData] = useState({
    eventoId: '',
    equipeId: equipeIdFromQuery || '',
    nome: '',
    diasFolgaAposEscala: '',
    ativo: true,
  });
  const [itens, setItens] = useState([{ cargoId: '', quantidade: 1, ordem: 0 }]);
  const [equipes, setEquipes] = useState([]);
  const [eventos, setEventos] = useState([]);
  const [cargos, setCargos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const [equipesRes, eventosRes, cargosRes] = await Promise.all([
        equipesApi.getAll(),
        eventosApi.getAll(),
        cargosApi.getAll(),
      ]);
      setEquipes(equipesRes.data || []);
      setEventos(eventosRes.data || []);
      setCargos(cargosRes.data || []);

      if (isEditing) {
        const res = await escalasModelosApi.getById(id);
        const m = res.data;
        setFormData({
          eventoId: m.eventoId != null ? String(m.eventoId) : '',
          equipeId: String(m.equipeId),
          nome: m.nome || '',
          diasFolgaAposEscala: m.diasFolgaAposEscala != null ? String(m.diasFolgaAposEscala) : '',
          ativo: !!m.ativo,
        });
        setItens(
          m.itens?.length
            ? m.itens.map((i) => ({
                cargoId: i.cargoId != null ? String(i.cargoId) : '',
                quantidade: i.quantidade ?? 1,
                ordem: i.ordem ?? 0,
              }))
            : [{ cargoId: '', quantidade: 1, ordem: 0 }]
        );
      } else if (equipeIdFromQuery) {
        setFormData((p) => ({ ...p, equipeId: equipeIdFromQuery }));
      }
    } catch (err) {
      setError(isEditing ? t('volunteer.schedules.models.form.errorLoadModel') : t('volunteer.schedules.models.form.errorLoadData'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id, equipeIdFromQuery]);

  const addItem = () => {
    setItens((prev) => [...prev, { cargoId: '', quantidade: 1, ordem: prev.length }]);
  };

  const removeItem = (index) => {
    setItens((prev) => prev.filter((_, i) => i !== index));
  };

  const updateItem = (index, field, value) => {
    setItens((prev) => {
      const next = [...prev];
      next[index] = { ...next[index], [field]: value };
      return next;
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.equipeId) {
      toast.error(t('volunteer.schedules.models.form.selectTeamError'));
      return;
    }
    const payload = {
      eventoId: formData.eventoId ? Number(formData.eventoId) : null,
      equipeId: Number(formData.equipeId),
      nome: formData.nome.trim() || null,
      diasFolgaAposEscala: formData.diasFolgaAposEscala ? Number(formData.diasFolgaAposEscala) : null,
      ativo: formData.ativo,
      itens: itens
        .filter((i) => i.quantidade > 0)
        .map((i, idx) => ({
          cargoId: i.cargoId ? Number(i.cargoId) : null,
          quantidade: Number(i.quantidade) || 1,
          ordem: idx,
        })),
    };
    if (!payload.itens.length) {
      toast.error(t('volunteer.schedules.models.form.addItemError'));
      return;
    }
    try {
      setSaving(true);
      if (isEditing) {
        await escalasModelosApi.update(id, {
          nome: payload.nome,
          diasFolgaAposEscala: payload.diasFolgaAposEscala,
          ativo: payload.ativo,
          itens: payload.itens,
        });
        toast.success(t('volunteer.schedules.models.form.updateSuccess'));
      } else {
        await escalasModelosApi.create(payload);
        toast.success(t('volunteer.schedules.models.form.createSuccess'));
      }
      navigate('/voluntariado/modelos-escala');
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data || t('volunteer.schedules.models.form.errorSave');
      toast.error(typeof msg === 'string' ? msg : t('volunteer.schedules.models.form.errorSave'));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingPage text={t('common.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" asChild>
          <Link to="/voluntariado/modelos-escala">
            <ArrowLeft className="h-4 w-4 mr-2" />
            {t('actions.back')}
          </Link>
        </Button>
        <h1 className="text-3xl font-bold">{isEditing ? t('volunteer.schedules.models.form.editTitle') : t('volunteer.schedules.models.form.newTitle')}</h1>
      </div>

      <form onSubmit={handleSubmit}>
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>{t('volunteer.schedules.models.form.dataTitle')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>{t('volunteer.schedules.models.form.fields.team')} *</Label>
                <Select
                  value={formData.equipeId || 'all'}
                  onValueChange={(v) => setFormData((p) => ({ ...p, equipeId: v === 'all' ? '' : v }))}
                  disabled={isEditing}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('volunteer.schedules.models.form.fields.selectTeam')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">{t('volunteer.schedules.models.form.fields.select')}</SelectItem>
                    {equipes.map((e) => (
                      <SelectItem key={e.id} value={String(e.id)}>{e.nome}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>{t('volunteer.schedules.models.form.fields.eventOptional')}</Label>
                <Select
                  value={formData.eventoId || 'none'}
                  onValueChange={(v) => setFormData((p) => ({ ...p, eventoId: v === 'none' ? '' : v }))}
                  disabled={isEditing}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('volunteer.schedules.models.form.fields.defaultAnyEvent')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">{t('volunteer.schedules.models.form.fields.defaultAnyEventOption')}</SelectItem>
                    {eventos.map((ev) => (
                      <SelectItem key={ev.id} value={String(ev.id)}>{ev.titulo}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-xs text-muted-foreground">{t('volunteer.schedules.models.form.fields.eventHint')}</p>
              </div>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>{t('volunteer.schedules.models.form.fields.nameOptional')}</Label>
                <Input
                  value={formData.nome}
                  onChange={(e) => setFormData((p) => ({ ...p, nome: e.target.value }))}
                  placeholder={t('volunteer.schedules.models.form.fields.namePlaceholder')}
                />
              </div>
              <div className="space-y-2">
                <Label>{t('volunteer.schedules.models.form.fields.daysOff')}</Label>
                <Input
                  type="number"
                  min="0"
                  value={formData.diasFolgaAposEscala}
                  onChange={(e) => setFormData((p) => ({ ...p, diasFolgaAposEscala: e.target.value }))}
                  placeholder={t('volunteer.schedules.models.form.fields.daysOffPlaceholder')}
                />
                <p className="text-xs text-muted-foreground">{t('volunteer.schedules.models.form.fields.daysOffHint')}</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <input
                id="ativo"
                type="checkbox"
                checked={formData.ativo}
                onChange={(e) => setFormData((p) => ({ ...p, ativo: e.target.checked }))}
              />
              <Label htmlFor="ativo">{t('volunteer.schedules.models.form.fields.active')}</Label>
            </div>
          </CardContent>
        </Card>

        <Card className="mb-6">
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>{t('volunteer.schedules.models.form.itemsTitle')}</CardTitle>
              <Button type="button" variant="outline" size="sm" onClick={addItem}>
                <Plus className="h-4 w-4 mr-2" /> {t('volunteer.schedules.models.form.add')}
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              {t('volunteer.schedules.models.form.itemsHint')}
            </p>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {itens.map((item, index) => (
                <div key={index} className="flex flex-wrap items-center gap-2 p-2 border rounded">
                  <Select
                    value={item.cargoId || 'any'}
                    onValueChange={(v) => updateItem(index, 'cargoId', v === 'any' ? '' : v)}
                  >
                    <SelectTrigger className="w-[200px]">
                      <SelectValue placeholder={t('volunteer.schedules.models.form.fields.role')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="any">{t('volunteer.schedules.models.form.fields.anyRole')}</SelectItem>
                      {cargos.map((c) => (
                        <SelectItem key={c.id} value={String(c.id)}>{c.nome}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <span className="text-muted-foreground">×</span>
                  <Input
                    type="number"
                    min="1"
                    className="w-20"
                    value={item.quantidade}
                    onChange={(e) => updateItem(index, 'quantidade', Number(e.target.value) || 1)}
                  />
                  <span className="text-muted-foreground">{t('volunteer.schedules.models.form.fields.people')}</span>
                  <Button type="button" variant="ghost" size="sm" onClick={() => removeItem(index)}>
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <div className="flex gap-2">
          <Button type="submit" disabled={saving}>
            <Save className="h-4 w-4 mr-2" />
            {saving ? t('actions.saving') : t('actions.save')}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/voluntariado/modelos-escala">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
