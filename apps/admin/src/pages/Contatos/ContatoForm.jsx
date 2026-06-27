import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { contatosApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

export default function ContatoForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [formData, setFormData] = useState({
    nome: '',
    whatsApp: '',
    email: '',
    membro: false,
    mensagem: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const load = async () => {
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await contatosApi.getById(id);
      const c = res.data;
      setFormData({
        nome: c.nome || '',
        whatsApp: c.whatsApp || '',
        email: c.email || '',
        membro: c.membro || false,
        mensagem: c.mensagem || '',
      });
    } catch (err) {
      setError(t('contactsForm.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSwitchChange = (checked) => {
    setFormData((prev) => ({ ...prev, membro: checked }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error(t('contactsForm.validation.nameRequired'));
      return;
    }
    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        whatsApp: formData.whatsApp.trim() || null,
        email: formData.email.trim() || null,
        membro: formData.membro,
        mensagem: formData.mensagem.trim() || null,
      };
      if (isEditing) await contatosApi.update(id, payload);
      else await contatosApi.create(payload);
      toast.success(isEditing ? t('contactsForm.updateSuccess') : t('contactsForm.createSuccess'));
      navigate('/contatos');
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('contactsForm.saveError')));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('contactsForm.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/contatos">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('contactsForm.editTitle') : t('contactsForm.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('contactsForm.editSubtitle') : t('contactsForm.createSubtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('contactsForm.editCardTitle') : t('contactsForm.createCardTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('contactsForm.fields.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder={t('contactsForm.placeholders.name')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="whatsApp">{t('contactsForm.fields.whatsapp')}</Label>
                <Input id="whatsApp" name="whatsApp" value={formData.whatsApp} onChange={handleChange} placeholder={t('contactsForm.placeholders.whatsapp')} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">{t('contactsForm.fields.email')}</Label>
                <Input id="email" name="email" type="email" value={formData.email} onChange={handleChange} placeholder={t('contactsForm.placeholders.email')} />
              </div>
              <div className="space-y-2 flex items-center space-x-3 pt-6">
                <Switch id="membro" checked={formData.membro} onCheckedChange={handleSwitchChange} />
                <Label htmlFor="membro" className="cursor-pointer">{t('contactsForm.fields.isMember')}</Label>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="mensagem">{t('contactsForm.fields.message')}</Label>
              <Textarea id="mensagem" name="mensagem" value={formData.mensagem} onChange={handleChange} placeholder={t('contactsForm.placeholders.message')} rows={4} />
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/contatos">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}






