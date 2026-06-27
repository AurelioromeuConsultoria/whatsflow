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
import { categoriasDespesasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function CategoriaDespesaForm() {
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
      const res = await categoriasDespesasApi.getById(id);
      const c = res.data || {};
      setFormData({
        nome: c.nome || '',
        descricao: c.descricao || '',
        ativo: c.ativo !== undefined ? c.ativo : true,
      });
    } catch (err) {
      setError(t('finance.expenseCategories.saveError'));
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

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error(t('finance.projects.nameRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        descricao: formData.descricao?.trim() || null,
        ativo: formData.ativo,
      };
      if (isEditing) await categoriasDespesasApi.update(id, payload);
      else await categoriasDespesasApi.create(payload);
      toast.success(isEditing ? t('finance.expenseCategories.saveSuccessEdit') : t('finance.expenseCategories.saveSuccessCreate'));
      navigate('/financeiro/categorias-despesas');
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('finance.expenseCategories.saveError');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('finance.expenseCategories.loadingForm')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/categorias-despesas">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('finance.expenseCategories.editTitle') : t('finance.expenseCategories.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('finance.expenseCategories.editSubtitle') : t('finance.expenseCategories.newSubtitle')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.expenseCategories.cardTitle')}</CardTitle>
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
                <input
                  type="checkbox"
                  id="ativo"
                  name="ativo"
                  checked={formData.ativo}
                  onChange={handleChange}
                  className="h-4 w-4"
                />
                <Label htmlFor="ativo" className="cursor-pointer">{t('finance.expenseCategories.activeLabel')}</Label>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex items-center space-x-4">
          <Button type="submit" disabled={loading}>
            <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/financeiro/categorias-despesas">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
