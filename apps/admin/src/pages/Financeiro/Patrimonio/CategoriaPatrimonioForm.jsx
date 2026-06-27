import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { categoriasPatrimonioApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function CategoriaPatrimonioForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [formData, setFormData] = useState({
    nome: '',
    descricao: '',
    ativo: true,
  });

  const load = async () => {
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await categoriasPatrimonioApi.getById(id);
      const item = res.data || {};
      setFormData({
        nome: item.nome || '',
        descricao: item.descricao || '',
        ativo: item.ativo !== undefined ? item.ativo : true,
      });
    } catch (err) {
      setError(t('finance.patrimonyCategories.saveError'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error(t('finance.patrimonyCategories.nameRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        descricao: formData.descricao?.trim() || null,
        ativo: formData.ativo,
      };

      if (isEditing) await categoriasPatrimonioApi.update(id, payload);
      else await categoriasPatrimonioApi.create(payload);

      toast.success(isEditing ? t('finance.patrimonyCategories.saveSuccessEdit') : t('finance.patrimonyCategories.saveSuccessCreate'));
      navigate('/financeiro/patrimonio/categorias');
    } catch (err) {
      toast.error(err.response?.data?.message || t('finance.patrimonyCategories.saveError'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('finance.patrimonyCategories.loadingForm')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/patrimonio/categorias">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('finance.patrimonyCategories.editTitle') : t('finance.patrimonyCategories.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('finance.patrimonyCategories.editSubtitle') : t('finance.patrimonyCategories.newSubtitle')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimonyCategories.cardTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="nome">{t('finance.common.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder={t('finance.common.name')} required />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="descricao">{t('finance.common.description')}</Label>
                <Textarea id="descricao" name="descricao" value={formData.descricao} onChange={handleChange} placeholder={t('finance.common.description')} rows={3} />
              </div>
              <div className="space-y-2 flex items-center space-x-3">
                <input type="checkbox" id="ativo" name="ativo" checked={formData.ativo} onChange={handleChange} className="h-4 w-4" />
                <Label htmlFor="ativo" className="cursor-pointer">{t('finance.patrimonyCategories.activeLabel')}</Label>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex items-center space-x-4">
          <Button type="submit" disabled={loading}>
            <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/financeiro/patrimonio/categorias">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
