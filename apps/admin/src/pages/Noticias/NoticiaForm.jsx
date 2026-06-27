import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save, Link as LinkIcon, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { ImageUpload } from '@/components/ImageUpload';
import { RichTextEditor } from '@/components/RichTextEditor';
import { noticiasApi, categoriasNoticiasApi, uploadApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function NoticiaForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [categorias, setCategorias] = useState([]);
  const [formData, setFormData] = useState({
    titulo: '',
    descricao: '',
    texto: '',
    data: '',
    url: '',
    imagem: '',
    categoriaNoticiaId: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [urlImportar, setUrlImportar] = useState('');
  const [extraindo, setExtraindo] = useState(false);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const categoriasRes = await categoriasNoticiasApi.getAll();
      setCategorias(categoriasRes.data || []);

      if (isEditing) {
        const res = await noticiasApi.getById(id);
        const n = res.data;
        setFormData({
          titulo: n.titulo || '',
          descricao: n.descricao || '',
          texto: n.texto || '',
          data: n.data ? new Date(n.data).toISOString().slice(0, 16) : '',
          url: n.url || '',
          imagem: n.imagem || '',
          categoriaNoticiaId: String(n.categoriaNoticiaId || ''),
        });
      }
    } catch (err) {
      setError(t('news.form.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleExtrairDeUrl = async () => {
    const url = urlImportar?.trim();
    if (!url) {
      toast.error(t('news.form.import.validation.urlRequired'));
      return;
    }
    try {
      setExtraindo(true);
      const res = await noticiasApi.extrairDeUrl(url);
      const d = res.data;
      let imagemPath = '';
      if (d.imagemUrl) {
        try {
          const imgRes = await uploadApi.uploadImageFromUrl(d.imagemUrl);
          imagemPath = imgRes.data?.url || imgRes.data?.path || '';
          // Se o backend tiver ProductionUploadSync configurado, a imagem já foi enviada para produção
        } catch (_) {
          toast.info(t('news.form.import.imageWarning'));
        }
      }
      setFormData((prev) => ({
        ...prev,
        titulo: d.titulo || prev.titulo,
        descricao: d.descricao || prev.descricao,
        texto: d.texto || prev.texto,
        data: d.data ? new Date(d.data).toISOString().slice(0, 16) : prev.data,
        url: d.url || url || prev.url,
        imagem: imagemPath || prev.imagem,
      }));
      toast.success(imagemPath ? t('news.form.import.successWithImage') : t('news.form.import.success'));
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data || t('news.form.import.extractUnavailable');
      toast.error(typeof msg === 'string' ? msg : t('news.form.import.extractError'));
      console.error(err);
    } finally {
      setExtraindo(false);
    }
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
    if (!formData.categoriaNoticiaId) {
      toast.error(t('news.form.validation.categoryRequired'));
      return;
    }
    try {
      setLoading(true);
      const payload = {
        titulo: formData.titulo.trim() || null,
        descricao: formData.descricao.trim() || null,
        texto: formData.texto.trim() || null,
        data: formData.data ? new Date(formData.data).toISOString() : null,
        url: normalizeUrl(formData.url),
        imagem: formData.imagem.trim() || null,
        categoriaNoticiaId: Number(formData.categoriaNoticiaId),
      };
      if (isEditing) await noticiasApi.update(id, payload);
      else await noticiasApi.create(payload);
      toast.success(isEditing ? t('news.form.updateSuccess') : t('news.form.createSuccess'));
      navigate('/noticias');
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('news.form.saveError');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('news.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/noticias">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('news.form.editPageTitle') : t('news.form.createPageTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('news.form.editSubtitle') : t('news.form.createSubtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('news.form.editCardTitle') : t('news.form.createCardTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          {!isEditing && (
            <div className="mb-6 p-4 rounded-lg border bg-muted/40 space-y-3">
              <Label className="text-sm font-medium">{t('news.form.import.title')}</Label>
              <p className="text-sm text-muted-foreground">
                {t('news.form.import.description')}
              </p>
              <div className="flex gap-2 flex-wrap">
                <Input
                  type="url"
                  placeholder={t('news.form.import.placeholder')}
                  value={urlImportar}
                  onChange={(e) => setUrlImportar(e.target.value)}
                  className="flex-1 min-w-[200px]"
                />
                <Button
                  type="button"
                  variant="secondary"
                  onClick={handleExtrairDeUrl}
                  disabled={extraindo}
                >
                  {extraindo ? <Loader2 className="h-4 w-4 animate-spin" /> : <LinkIcon className="h-4 w-4" />}
                  {extraindo ? ` ${t('news.form.import.extracting')}` : ` ${t('news.form.import.extract')}`}
                </Button>
              </div>
            </div>
          )}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="titulo">{t('news.table.title')} *</Label>
                <Input id="titulo" name="titulo" value={formData.titulo} onChange={handleChange} placeholder={t('news.form.placeholders.title')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="categoriaNoticiaId">{t('news.table.category')} *</Label>
                <select id="categoriaNoticiaId" name="categoriaNoticiaId" value={formData.categoriaNoticiaId} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="">{t('actions.select')}</option>
                  {categorias.map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="descricao">{t('news.table.description')}</Label>
              <Textarea id="descricao" name="descricao" value={formData.descricao} onChange={handleChange} placeholder={t('news.form.placeholders.description')} rows={3} />
            </div>

            <div className="space-y-2">
              <RichTextEditor
                label={t('news.form.fields.content')}
                name="texto"
                value={formData.texto}
                onChange={handleChange}
                placeholder={t('news.form.placeholders.content')}
              />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="data">{t('news.table.date')}</Label>
                <Input id="data" name="data" type="datetime-local" value={formData.data} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="url">{t('news.form.fields.url')}</Label>
                <Input 
                  id="url" 
                  name="url" 
                  type="text" 
                  value={formData.url} 
                  onChange={handleChange} 
                  placeholder={t('news.form.placeholders.url')} 
                />
                {formData.url && !formData.url.match(/^https?:\/\//i) && (
                  <p className="text-xs text-muted-foreground">
                    {t('news.form.urlHint')}
                  </p>
                )}
              </div>
            </div>

            <div className="space-y-2">
              <ImageUpload
                label={t('news.form.fields.image')}
                value={formData.imagem}
                onChange={(url) => setFormData((prev) => ({ ...prev, imagem: url }))}
                accept="image/*"
                type="image"
              />
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/noticias">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

