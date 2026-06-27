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
import { galeriasFotosApi, eventosApi, categoriasMidiasApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

export default function GaleriaFotoForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [eventos, setEventos] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [formData, setFormData] = useState({
    nome: '',
    descricao: '',
    data: '',
    eventoId: '',
    categoriaMidiaId: '',
    ativo: true,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const [eventosRes, categoriasRes] = await Promise.all([
        eventosApi.getAll(),
        categoriasMidiasApi.getAll(),
      ]);
      setEventos(eventosRes.data || []);
      setCategorias(categoriasRes.data || []);

      if (isEditing) {
        const res = await galeriasFotosApi.getById(id);
        const g = res.data;
        setFormData({
          nome: g.nome || '',
          descricao: g.descricao || '',
          data: g.data ? new Date(g.data).toISOString().slice(0, 10) : '',
          eventoId: String(g.eventoId || ''),
          categoriaMidiaId: String(g.categoriaMidiaId || ''),
          ativo: g.ativo !== undefined ? g.ativo : true,
        });
      }
    } catch (err) {
      setError(t('photoGalleries.errorLoadData'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error(t('photoGalleries.nameRequired'));
      return;
    }
    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        descricao: formData.descricao.trim() || null,
        data: formData.data ? new Date(formData.data + 'T00:00:00').toISOString() : null,
        eventoId: formData.eventoId ? Number(formData.eventoId) : null,
        categoriaMidiaId: formData.categoriaMidiaId ? Number(formData.categoriaMidiaId) : null,
        ativo: formData.ativo,
      };
      
      let result;
      if (isEditing) {
        result = await galeriasFotosApi.update(id, payload);
      } else {
        result = await galeriasFotosApi.create(payload);
      }
      
      if (!isEditing && result.data?.id) {
        toast.success(t('photoGalleries.createSuccess'));
        navigate(`/galerias-fotos/${result.data.id}/fotos`);
      } else {
        toast.success(t('photoGalleries.saveSuccess'));
        navigate('/galerias-fotos');
      }
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('photoGalleries.saveError')));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing && !formData.nome) return <LoadingPage text={t('photoGalleries.loadingForm')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/galerias-fotos">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('photoGalleries.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('photoGalleries.editTitle') : t('photoGalleries.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('photoGalleries.editSubtitle') : t('photoGalleries.newSubtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('photoGalleries.editTitle') : t('photoGalleries.cardTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('photoGalleries.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder={t('photoGalleries.name')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="data">{t('photoGalleries.date')}</Label>
                <Input id="data" name="data" type="date" value={formData.data} onChange={handleChange} />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="descricao">{t('photoGalleries.description')}</Label>
              <Textarea id="descricao" name="descricao" value={formData.descricao} onChange={handleChange} placeholder={t('photoGalleries.description')} rows={3} />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="eventoId">{t('photoGalleries.eventOptional')}</Label>
                <select id="eventoId" name="eventoId" value={formData.eventoId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('photoGalleries.none')}</option>
                  {eventos.map((e) => (
                    <option key={e.id} value={e.id}>{e.titulo}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="categoriaMidiaId">{t('photoGalleries.categoryOptional')}</Label>
                <select id="categoriaMidiaId" name="categoriaMidiaId" value={formData.categoriaMidiaId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('photoGalleries.noneCategory')}</option>
                  {categorias.map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>
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
              <Label htmlFor="ativo" className="cursor-pointer">{t('photoGalleries.activeLabel')}</Label>
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('photoGalleries.saveAndAddPhotos'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/galerias-fotos">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}





