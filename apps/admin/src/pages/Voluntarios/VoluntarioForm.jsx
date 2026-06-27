import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { voluntariosApi, equipesApi, cargosApi, pessoasApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

const WHATSAPP_REGEX = /^\d{10,13}$/;

export default function VoluntarioForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [pessoas, setPessoas] = useState([]);
  const [pessoaBusca, setPessoaBusca] = useState('');
  const [equipes, setEquipes] = useState([]);
  const [cargos, setCargos] = useState([]);
  const [vinculosExistentes, setVinculosExistentes] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const [formData, setFormData] = useState({
    pessoaId: '',
    whatsApp: '',
    email: '',
    telefone: '',
    vinculos: [{ equipeId: '', cargoId: '', id: null }],
  });

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const [p, e, c] = await Promise.all([
        pessoasApi.getAll(),
        equipesApi.getAll(),
        cargosApi.getAll(),
      ]);
      setPessoas(p.data || []);
      setEquipes(e.data || []);
      setCargos(c.data || []);

      if (isEditing) {
        const res = await voluntariosApi.getById(id);
        const v = res.data;
        setFormData({
          pessoaId: String(v.pessoaId || ''),
          whatsApp: v.whatsApp || '',
          email: v.email || '',
          telefone: v.telefone || '',
          vinculos: [{ equipeId: String(v.equipeId || ''), cargoId: String(v.cargoId || ''), id: v.id }],
        });
        if (v.pessoaId) {
          const vinculosRes = await voluntariosApi.getByPessoa(v.pessoaId);
          const vinculos = vinculosRes.data || [];
          setVinculosExistentes(vinculos);
          setFormData((prev) => ({
            ...prev,
            vinculos: vinculos.length > 0
              ? vinculos.map((item) => ({
                  equipeId: String(item.equipeId || ''),
                  cargoId: String(item.cargoId || ''),
                  id: item.id,
                }))
              : prev.vinculos,
          }));
        }
      }
    } catch (err) {
      setError(t('volunteer.volunteers.form.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const handleChange = async (e) => {
    const { name, value } = e.target;
    setFormData((prev) => {
      const next = { ...prev, [name]: value };
      if (name === 'pessoaId' && value) {
        const pessoa = pessoas.find((p) => String(p.id) === String(value));
        if (pessoa) {
          next.whatsApp = pessoa.whatsApp || prev.whatsApp || '';
          next.email = pessoa.email || prev.email || '';
          next.telefone = pessoa.telefone || prev.telefone || '';
        }
      }
      return next;
    });
    if (name === 'pessoaId' && value) {
      try {
        const res = await voluntariosApi.getByPessoa(value);
        setVinculosExistentes(res.data || []);
      } catch {
        setVinculosExistentes([]);
      }
    } else if (name === 'pessoaId' && !value) {
      setVinculosExistentes([]);
    }
  };

  const pessoasFiltradas = pessoas.filter((p) => {
    if (!pessoaBusca) return true;
    const b = pessoaBusca.toLowerCase();
    const nome = (p.nome || '').toLowerCase();
    const email = (p.email || '').toLowerCase();
    const whats = String(p.whatsApp || '').toLowerCase();
    return nome.includes(b) || email.includes(b) || whats.includes(b);
  });

  const pessoaSelecionada = pessoas.find((p) => String(p.id) === String(formData.pessoaId));

  const addVinculo = () => {
    setFormData((prev) => ({
      ...prev,
      vinculos: [...prev.vinculos, { equipeId: '', cargoId: '', id: null }],
    }));
  };

  const removeVinculo = (idx) => {
    setFormData((prev) => {
      if (prev.vinculos.length <= 1) return prev;
      return { ...prev, vinculos: prev.vinculos.filter((_, i) => i !== idx) };
    });
  };

  const updateVinculo = (idx, field, value) => {
    setFormData((prev) => ({
      ...prev,
      vinculos: prev.vinculos.map((v, i) => (i === idx ? { ...v, [field]: value } : v)),
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.pessoaId) {
      toast.error(t('volunteer.volunteers.form.selectPersonError'));
      return;
    }
    const onlyDigits = String(formData.whatsApp).replace(/\D/g, '');
    const validVinculos = formData.vinculos.filter((v) => v.equipeId && v.cargoId);
    if (validVinculos.length === 0) {
      toast.error(t('volunteer.volunteers.form.addTeamRoleError'));
      return;
    }
    if (formData.email && !/.+@.+\..+/.test(formData.email)) {
      toast.error(t('volunteer.volunteers.form.invalidEmail'));
      return;
    }
    if (formData.whatsApp && !WHATSAPP_REGEX.test(onlyDigits)) {
      toast.error(t('volunteer.volunteers.form.invalidWhatsapp'));
      return;
    }
    try {
      setLoading(true);
      const basePayload = {
        pessoaId: Number(formData.pessoaId),
        whatsApp: formData.whatsApp ? onlyDigits : null,
        email: formData.email?.trim() || null,
        telefone: formData.telefone?.trim() || null,
      };
      if (isEditing) {
        const validIds = new Set(validVinculos.filter((v) => v.id).map((v) => Number(v.id)));
        const removed = vinculosExistentes.filter((v) => !validIds.has(Number(v.id)));
        const toUpdate = validVinculos.filter((v) => v.id);
        const toCreate = validVinculos.filter((v) => !v.id);

        for (const v of removed) {
          await voluntariosApi.delete(v.id);
        }
        for (const v of toUpdate) {
          await voluntariosApi.update(v.id, {
            ...basePayload,
            equipeId: Number(v.equipeId),
            cargoId: Number(v.cargoId),
          });
        }
        for (const v of toCreate) {
          await voluntariosApi.create({
            ...basePayload,
            equipeId: Number(v.equipeId),
            cargoId: Number(v.cargoId),
          });
        }
        toast.success(t('volunteer.volunteers.form.updateSuccess'));
      } else {
        for (const v of validVinculos) {
          await voluntariosApi.create({
            ...basePayload,
            equipeId: Number(v.equipeId),
            cargoId: Number(v.cargoId),
          });
        }
        toast.success(
          validVinculos.length > 1
            ? t('volunteer.volunteers.form.createMultiSuccess')
            : t('volunteer.volunteers.form.createSuccess')
        );
      }
      navigate('/voluntarios');
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('volunteer.volunteers.form.errorSave')));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('volunteer.volunteers.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/voluntarios">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('volunteer.volunteers.edit') : t('volunteer.volunteers.new')}</h1>
          <p className="text-muted-foreground">
            {isEditing ? t('volunteer.volunteers.form.editSubtitle') : t('volunteer.volunteers.form.createSubtitle')}
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('volunteer.volunteers.edit') : t('volunteer.volunteers.create')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="pessoaBusca">{t('volunteer.volunteers.form.fields.person')} *</Label>
                <Input
                  id="pessoaBusca"
                  name="pessoaBusca"
                  value={pessoaBusca}
                  onChange={(e) => setPessoaBusca(e.target.value)}
                  placeholder={t('volunteer.volunteers.form.fields.personSearchPlaceholder')}
                />
                <select
                  id="pessoaId"
                  name="pessoaId"
                  value={formData.pessoaId}
                  onChange={handleChange}
                  className="w-full px-3 py-2 border rounded"
                  required
                >
                  <option value="">{t('volunteer.volunteers.form.selectOption')}</option>
                  {pessoasFiltradas.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nome}{p.email ? ` — ${p.email}` : ''}{p.whatsApp ? ` — ${p.whatsApp}` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="whatsApp">{t('volunteer.volunteers.form.fields.whatsapp')}</Label>
                <Input
                  id="whatsApp"
                  name="whatsApp"
                  value={formData.whatsApp}
                  onChange={handleChange}
                  placeholder={
                    pessoaSelecionada?.whatsApp
                      ? t('volunteer.volunteers.form.currentValue', { value: pessoaSelecionada.whatsApp })
                      : t('volunteer.volunteers.form.fields.whatsappPlaceholder')
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">{t('volunteer.volunteers.form.fields.email')}</Label>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder={
                    pessoaSelecionada?.email
                      ? t('volunteer.volunteers.form.currentValue', { value: pessoaSelecionada.email })
                      : t('volunteer.volunteers.form.fields.emailPlaceholder')
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="telefone">{t('volunteer.volunteers.form.fields.phone')}</Label>
                <Input
                  id="telefone"
                  name="telefone"
                  value={formData.telefone}
                  onChange={handleChange}
                  placeholder={
                    pessoaSelecionada?.telefone
                      ? t('volunteer.volunteers.form.currentValue', { value: pessoaSelecionada.telefone })
                      : t('volunteer.volunteers.form.fields.phonePlaceholder')
                  }
                />
              </div>
              {vinculosExistentes.length > 0 && (
                <div className="md:col-span-2 p-3 rounded-md bg-muted/50 text-sm">
                  <p className="font-medium text-muted-foreground mb-1">{t('volunteer.volunteers.form.existingLinksTitle')}</p>
                  <p className="text-foreground">
                    {vinculosExistentes.map((v) => `${v.nomeEquipe || t('volunteer.volunteers.form.fallbackTeam')} (${v.nomeCargo || t('volunteer.volunteers.form.fallbackRole')})`).join(' • ')}
                  </p>
                  <p className="text-muted-foreground mt-1 text-xs">
                    {t('volunteer.volunteers.form.existingLinksHint')}
                  </p>
                </div>
              )}
              <div className="md:col-span-2 space-y-3">
                <div className="flex items-center justify-between">
                  <Label>{t('volunteer.volunteers.form.linksTitle')}</Label>
                  <Button type="button" variant="outline" size="sm" onClick={addVinculo}>
                    <Plus className="h-4 w-4 mr-1" /> {t('volunteer.volunteers.form.addTeamAction')}
                  </Button>
                </div>
                {formData.vinculos.map((vinculo, idx) => (
                  <div key={idx} className="flex gap-2 items-end flex-wrap">
                    <div className="flex-1 min-w-[140px] space-y-1">
                      <Label className="text-xs">{t('volunteer.volunteers.form.fields.team')}</Label>
                      <select
                        value={vinculo.equipeId}
                        onChange={(e) => updateVinculo(idx, 'equipeId', e.target.value)}
                        className="w-full px-3 py-2 border rounded"
                      >
                        <option value="">{t('volunteer.volunteers.form.selectOption')}</option>
                        {equipes.map((e) => (
                          <option key={e.id} value={e.id}>{e.nome}</option>
                        ))}
                      </select>
                    </div>
                    <div className="flex-1 min-w-[140px] space-y-1">
                      <Label className="text-xs">{t('volunteer.volunteers.form.fields.role')}</Label>
                      <select
                        value={vinculo.cargoId}
                        onChange={(e) => updateVinculo(idx, 'cargoId', e.target.value)}
                        className="w-full px-3 py-2 border rounded"
                      >
                        <option value="">{t('volunteer.volunteers.form.selectOption')}</option>
                        {cargos.map((c) => (
                          <option key={c.id} value={c.id}>{c.nome}</option>
                        ))}
                      </select>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeVinculo(idx)}
                      disabled={formData.vinculos.length === 1}
                      title={t('actions.remove')}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                ))}
              </div>
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('volunteer.volunteers.update') : t('volunteer.volunteers.create'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/voluntarios">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
