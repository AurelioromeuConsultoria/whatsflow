import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { hubCasasApi, usuariosApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

export default function CasaForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [usuarios, setUsuarios] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const [formData, setFormData] = useState({
    nome: '',
    abertoPorId: '',
    liderId: '',
    timoteoId: '',
    enderecoCompleto: '',
    anfitriao: '',
  });

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const usersRes = await usuariosApi.getAll();
      setUsuarios(usersRes.data || []);

      if (isEditing) {
        const res = await hubCasasApi.getById(id);
        const casa = res.data || {};
        setFormData({
          nome: casa.nome || '',
          abertoPorId: String(casa.abertoPorId ?? casa.abertoPor?.id ?? ''),
          liderId: String(casa.liderId ?? casa.lider?.id ?? ''),
          timoteoId: String(casa.timoteoId ?? casa.timoteo?.id ?? ''),
          enderecoCompleto: casa.enderecoCompleto || casa.endereco || '',
          anfitriao: casa.anfitriao || '',
        });
      }
    } catch (err) {
      setError(t('housesForm.errorLoad'));
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

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error(t('housesForm.validation.nameRequired'));
      return;
    }
    if (!formData.enderecoCompleto.trim()) {
      toast.error(t('housesForm.validation.addressRequired'));
      return;
    }
    if (!formData.anfitriao.trim()) {
      toast.error(t('housesForm.validation.hostRequired'));
      return;
    }
    if (!formData.abertoPorId || !formData.liderId || !formData.timoteoId) {
      toast.error(t('housesForm.validation.usersRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        enderecoCompleto: formData.enderecoCompleto.trim(),
        anfitriao: formData.anfitriao.trim(),
        abertoPorId: Number(formData.abertoPorId),
        liderId: Number(formData.liderId),
        timoteoId: Number(formData.timoteoId),
      };
      if (isEditing) await hubCasasApi.update(id, payload);
      else await hubCasasApi.create(payload);
      toast.success(isEditing ? t('housesForm.updateSuccess') : t('housesForm.createSuccess'));
      navigate('/hub/casas');
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('housesForm.saveError')));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('housesForm.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/hub/casas">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('housesForm.editTitle') : t('housesForm.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('housesForm.editSubtitle') : t('housesForm.createSubtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('housesForm.editCardTitle') : t('housesForm.createCardTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('housesForm.fields.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder={t('housesForm.placeholders.name')} required />
              </div>

              <div className="space-y-2">
                <Label htmlFor="anfitriao">{t('housesForm.fields.host')} *</Label>
                <Input id="anfitriao" name="anfitriao" value={formData.anfitriao} onChange={handleChange} placeholder={t('housesForm.placeholders.host')} required />
              </div>

              <div className="space-y-2">
                <Label htmlFor="abertoPorId">{t('housesForm.fields.openedBy')} *</Label>
                <select id="abertoPorId" name="abertoPorId" value={formData.abertoPorId} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="">{t('actions.select')}</option>
                  {usuarios.map((u) => (
                    <option key={u.id} value={u.id}>{u.nome || u.email || u.id}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="liderId">{t('housesForm.fields.leader')} *</Label>
                <select id="liderId" name="liderId" value={formData.liderId} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="">{t('actions.select')}</option>
                  {usuarios.map((u) => (
                    <option key={u.id} value={u.id}>{u.nome || u.email || u.id}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="timoteoId">{t('housesForm.fields.timothy')} *</Label>
                <select id="timoteoId" name="timoteoId" value={formData.timoteoId} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="">{t('actions.select')}</option>
                  {usuarios.map((u) => (
                    <option key={u.id} value={u.id}>{u.nome || u.email || u.id}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="enderecoCompleto">{t('housesForm.fields.address')} *</Label>
                <Input id="enderecoCompleto" name="enderecoCompleto" value={formData.enderecoCompleto} onChange={handleChange} placeholder={t('housesForm.placeholders.address')} required />
              </div>
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/hub/casas">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
