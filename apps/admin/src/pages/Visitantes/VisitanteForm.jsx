import { useState, useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save, RefreshCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { visitantesApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function VisitanteForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = !!id;
  const { t } = useTranslation();

  const [formData, setFormData] = useState({
    nome: '',
    telefone: '',
    whatsApp: '',
    email: '',
    dataNascimento: '',
    dataVisita: new Date().toISOString().split('T')[0],
    observacoes: ''
  });
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [regenerando, setRegenerando] = useState(false);
  const [error, setError] = useState(null);

  const loadVisitante = async () => {
    if (!isEditing) return;

    try {
      setLoading(true);
      setError(null);
      const response = await visitantesApi.getById(id);
      const visitante = response.data;
      setFormData({
        nome: visitante.nome || '',
        telefone: visitante.telefone || '',
        whatsApp: visitante.whatsApp || '',
        email: visitante.email || '',
        dataNascimento: visitante.dataNascimento ? visitante.dataNascimento.split('T')[0] : '',
        dataVisita: visitante.dataVisita ? visitante.dataVisita.split('T')[0] : new Date().toISOString().split('T')[0],
        observacoes: visitante.observacoes || ''
      });
    } catch (err) {
      setError(t('visitors.form.errorLoad'));
      console.error('Erro ao carregar visitante:', err);
    } finally {
      setLoading(false);
    }
  };

  const normalizePhone = (phone) => {
    return phone ? phone.replace(/\D/g, '') : null;
  };

  const toLocalDateTime = (date) => {
    return date ? `${date}T00:00:00` : null;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.nome || !formData.dataVisita) {
      toast.error(t('visitors.form.validation.nameAndVisitDateRequired'));
      return;
    }

    if (formData.email && !/.+@.+\..+/.test(formData.email)) {
      toast.error(t('visitors.form.validation.emailInvalid'));
      return;
    }

    try {
      setSaving(true);
      
      const submitData = {
        nome: formData.nome.trim(),
        email: formData.email?.trim() || null,
        telefone: normalizePhone(formData.telefone),
        whatsApp: normalizePhone(formData.whatsApp),
        dataNascimento: toLocalDateTime(formData.dataNascimento),
        dataVisita: toLocalDateTime(formData.dataVisita),
        observacoes: formData.observacoes?.trim() || null
      };

      if (isEditing) {
        // Para edição, só atualiza campos da visita
        await visitantesApi.update(id, {
          dataVisita: submitData.dataVisita,
          observacoes: submitData.observacoes
        });
        toast.success(t('visitors.form.updateSuccess'));
      } else {
        await visitantesApi.create(submitData);
        toast.success(t('visitors.form.createSuccess'));
      }

      navigate('/visitantes');
    } catch (err) {
      const errorMessage = err.response?.data?.detail ||
                          err.response?.data?.error ||
                          err.response?.data?.message ||
                          t(isEditing ? 'visitors.form.updateError' : 'visitors.form.createError');
      toast.error(errorMessage);
      console.error(`Erro ao ${isEditing ? 'atualizar' : 'cadastrar'} visita:`, err);
    } finally {
      setSaving(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleRegerarMensagens = async () => {
    if (!isEditing) return;
    try {
      setRegenerando(true);
      const res = await visitantesApi.regerarMensagens(id);
      toast.success(
        t('visitors.form.regenerateSuccess', {
          created: res.data?.mensagensCriadas ?? 0,
          canceled: res.data?.mensagensCanceladas ?? 0,
        })
      );
    } catch (err) {
      const msg = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || err.response?.data?.error || t('visitors.form.regenerateError'));
      toast.error(msg);
      console.error(err);
    } finally {
      setRegenerando(false);
    }
  };

  useEffect(() => {
    loadVisitante();
  }, [id]);

  if (loading) {
    return <LoadingPage text={t('visitors.form.loading')} />;
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadVisitante} />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/visitantes">
            <ArrowLeft className="h-4 w-4 mr-2" />
            {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">
            {isEditing ? t('visitors.edit') : t('visitors.new')}
          </h1>
          <p className="text-muted-foreground">
            {isEditing ? t('visitors.form.editSubtitle') : t('visitors.form.createSubtitle')}
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>
            {isEditing ? t('visitors.edit') : t('visitors.create')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('visitors.form.fields.name')} *</Label>
                <Input
                  id="nome"
                  name="nome"
                  value={formData.nome}
                  onChange={handleChange}
                  placeholder={t('visitors.form.placeholders.name')}
                  required
                  disabled={isEditing}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">{t('visitors.form.fields.email')}</Label>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder={t('visitors.form.placeholders.email')}
                  disabled={isEditing}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="telefone">{t('visitors.form.fields.phone')}</Label>
                <Input
                  id="telefone"
                  name="telefone"
                  value={formData.telefone}
                  onChange={handleChange}
                  placeholder={t('visitors.form.placeholders.phone')}
                  disabled={isEditing}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="whatsApp">{t('visitors.form.fields.whatsapp')}</Label>
                <Input
                  id="whatsApp"
                  name="whatsApp"
                  value={formData.whatsApp}
                  onChange={handleChange}
                  placeholder={t('visitors.form.placeholders.whatsapp')}
                  disabled={isEditing}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="dataNascimento">{t('visitors.form.fields.birthDate')}</Label>
                <Input
                  id="dataNascimento"
                  name="dataNascimento"
                  type="date"
                  value={formData.dataNascimento}
                  onChange={handleChange}
                  disabled={isEditing}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="dataVisita">{t('visitors.form.fields.visitDate')} *</Label>
                <Input
                  id="dataVisita"
                  name="dataVisita"
                  type="date"
                  value={formData.dataVisita}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="observacoes">{t('visitors.form.fields.notes')}</Label>
              <Textarea
                id="observacoes"
                name="observacoes"
                value={formData.observacoes}
                onChange={handleChange}
                placeholder={t('visitors.form.placeholders.notes')}
                rows={3}
              />
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={saving}>
                <Save className="h-4 w-4 mr-2" />
                {saving ? t('actions.saving') : (isEditing ? t('visitors.update') : t('visitors.create'))}
              </Button>
              {isEditing && (
                <Button type="button" variant="outline" onClick={handleRegerarMensagens} disabled={regenerando}>
                  <RefreshCcw className="h-4 w-4 mr-2" />
                  {regenerando ? t('visitors.form.regenerating') : t('visitors.form.regenerateMessages')}
                </Button>
              )}
              <Button type="button" variant="outline" asChild>
                <Link to="/visitantes">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
