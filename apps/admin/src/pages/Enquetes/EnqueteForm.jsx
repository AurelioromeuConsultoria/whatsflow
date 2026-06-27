import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save, Plus, Trash2, GripVertical } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { enquetesApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function EnqueteForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [formData, setFormData] = useState({
    titulo: '',
    descricao: '',
    dataInicio: '',
    dataFim: '',
    ativo: true,
    permitirMultiplaEscolha: false,
    permitirVotoAnonimo: true,
    opcoes: [{ texto: '', ordem: 0 }],
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const load = async () => {
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await enquetesApi.getById(id);
      const e = res.data;
      setFormData({
        titulo: e.titulo || '',
        descricao: e.descricao || '',
        dataInicio: e.dataInicio ? new Date(e.dataInicio).toISOString().slice(0, 16) : '',
        dataFim: e.dataFim ? new Date(e.dataFim).toISOString().slice(0, 16) : '',
        ativo: e.ativo !== undefined ? e.ativo : true,
        permitirMultiplaEscolha: e.permitirMultiplaEscolha || false,
        permitirVotoAnonimo: e.permitirVotoAnonimo !== undefined ? e.permitirVotoAnonimo : true,
        opcoes: e.opcoes && e.opcoes.length > 0
          ? e.opcoes.map((op, idx) => ({ id: op.id, texto: op.texto, ordem: op.ordem || idx }))
          : [{ texto: '', ordem: 0 }],
      });
    } catch (err) {
      setError(t('pollsManagement.form.errorLoad'));
      console.error(err);
      toast.error(t('pollsManagement.form.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSwitchChange = (name, checked) => {
    setFormData((prev) => ({ ...prev, [name]: checked }));
  };

  const addOpcao = () => {
    setFormData((prev) => ({
      ...prev,
      opcoes: [
        ...prev.opcoes,
        { texto: '', ordem: prev.opcoes.length },
      ],
    }));
  };

  const removeOpcao = (index) => {
    if (formData.opcoes.length <= 1) {
      toast.error(t('pollsManagement.form.validation.minOneOption'));
      return;
    }
    setFormData((prev) => ({
      ...prev,
      opcoes: prev.opcoes.filter((_, i) => i !== index).map((op, idx) => ({ ...op, ordem: idx })),
    }));
  };

  const updateOpcao = (index, field, value) => {
    setFormData((prev) => ({
      ...prev,
      opcoes: prev.opcoes.map((op, i) =>
        i === index ? { ...op, [field]: value } : op
      ),
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Validações
    if (!formData.titulo.trim()) {
      toast.error(t('pollsManagement.form.validation.titleRequired'));
      return;
    }

    if (formData.opcoes.length < 2) {
      toast.error(t('pollsManagement.form.validation.minTwoOptions'));
      return;
    }

    const opcoesValidas = formData.opcoes.filter((op) => op.texto.trim());
    if (opcoesValidas.length < 2) {
      toast.error(t('pollsManagement.form.validation.optionsTextRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        titulo: formData.titulo.trim(),
        descricao: formData.descricao.trim() || null,
        dataInicio: formData.dataInicio ? new Date(formData.dataInicio).toISOString() : null,
        dataFim: formData.dataFim ? new Date(formData.dataFim).toISOString() : null,
        ativo: formData.ativo,
        permitirMultiplaEscolha: formData.permitirMultiplaEscolha,
        permitirVotoAnonimo: formData.permitirVotoAnonimo,
        opcoes: opcoesValidas.map((op, idx) => ({
          id: op.id || null,
          texto: op.texto.trim(),
          ordem: idx,
        })),
      };

      if (isEditing) {
        await enquetesApi.update(id, payload);
        toast.success(t('pollsManagement.form.updateSuccess'));
      } else {
        await enquetesApi.create(payload);
        toast.success(t('pollsManagement.form.createSuccess'));
      }
      navigate('/enquetes');
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('pollsManagement.form.saveError');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('pollsManagement.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/enquetes">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold text-foreground">
            {isEditing ? t('pollsManagement.form.editPageTitle') : t('pollsManagement.form.createPageTitle')}
          </h1>
          <p className="text-muted-foreground">
            {isEditing ? t('pollsManagement.form.editSubtitle') : t('pollsManagement.form.createSubtitle')}
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>{t('pollsManagement.form.sections.basic')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="titulo">{t('pollsManagement.table.title')} *</Label>
              <Input
                id="titulo"
                name="titulo"
                value={formData.titulo}
                onChange={handleChange}
                placeholder={t('pollsManagement.form.placeholders.title')}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="descricao">{t('pollsManagement.table.description')}</Label>
              <Textarea
                id="descricao"
                name="descricao"
                value={formData.descricao}
                onChange={handleChange}
                placeholder={t('pollsManagement.form.placeholders.description')}
                rows={3}
              />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="dataInicio">{t('pollsManagement.form.fields.startDate')} *</Label>
                <Input
                  id="dataInicio"
                  name="dataInicio"
                  type="datetime-local"
                  value={formData.dataInicio}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataFim">{t('pollsManagement.form.fields.endDate')} *</Label>
                <Input
                  id="dataFim"
                  name="dataFim"
                  type="datetime-local"
                  value={formData.dataFim}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('pollsManagement.form.sections.options')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {formData.opcoes.map((opcao, index) => (
              <div key={index} className="flex items-start gap-2 p-3 border border-border rounded-lg">
                <div className="flex items-center pt-2 text-muted-foreground">
                  <GripVertical className="h-5 w-5" />
                </div>
                <div className="flex-1 space-y-2">
                  <Label>{t('pollsManagement.form.optionLabel', { index: index + 1 })}</Label>
                  <Input
                    value={opcao.texto}
                    onChange={(e) => updateOpcao(index, 'texto', e.target.value)}
                    placeholder={t('pollsManagement.form.optionPlaceholder', { index: index + 1 })}
                    required
                  />
                </div>
                {formData.opcoes.length > 1 && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => removeOpcao(index)}
                    className="mt-7 text-destructive hover:text-destructive"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
            ))}
            <Button type="button" variant="outline" onClick={addOpcao} className="w-full">
              <Plus className="h-4 w-4 mr-2" />
              {t('pollsManagement.form.addOption')}
            </Button>
            <p className="text-sm text-muted-foreground">
              {t('pollsManagement.form.minOptionsHint')}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('pollsManagement.form.sections.settings')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="ativo">{t('pollsManagement.form.settings.active')}</Label>
                <p className="text-sm text-muted-foreground">
                  {t('pollsManagement.form.settings.activeHint')}
                </p>
              </div>
              <Switch
                id="ativo"
                checked={formData.ativo}
                onCheckedChange={(checked) => handleSwitchChange('ativo', checked)}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="permitirMultiplaEscolha">{t('pollsManagement.form.settings.multipleChoice')}</Label>
                <p className="text-sm text-muted-foreground">
                  {t('pollsManagement.form.settings.multipleChoiceHint')}
                </p>
              </div>
              <Switch
                id="permitirMultiplaEscolha"
                checked={formData.permitirMultiplaEscolha}
                onCheckedChange={(checked) => handleSwitchChange('permitirMultiplaEscolha', checked)}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="permitirVotoAnonimo">{t('pollsManagement.form.settings.anonymousVote')}</Label>
                <p className="text-sm text-muted-foreground">
                  {t('pollsManagement.form.settings.anonymousVoteHint')}
                </p>
              </div>
              <Switch
                id="permitirVotoAnonimo"
                checked={formData.permitirVotoAnonimo}
                onCheckedChange={(checked) => handleSwitchChange('permitirVotoAnonimo', checked)}
              />
            </div>
          </CardContent>
        </Card>

        <div className="flex items-center space-x-4">
          <Button type="submit" disabled={loading}>
            <Save className="h-4 w-4 mr-2" />
            {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/enquetes">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
