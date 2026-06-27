import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { fornecedoresApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

const EMAIL_REGEX = /.+@.+\..+/;

export default function FornecedorForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const [formData, setFormData] = useState({
    nome: '',
    razaoSocial: '',
    cnpjCpf: '',
    inscricaoEstadual: '',
    endereco: '',
    telefone: '',
    site: '',
    contatoNome: '',
    contatoCpf: '',
    contatoWhatsApp: '',
    contatoEmail: '',
  });

  const load = async () => {
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await fornecedoresApi.getById(id);
      const f = res.data || {};
      setFormData({
        nome: f.nome || '',
        razaoSocial: f.razaoSocial || '',
        cnpjCpf: f.cnpjCpf || '',
        inscricaoEstadual: f.inscricaoEstadual || '',
        endereco: f.endereco || '',
        telefone: f.telefone || '',
        site: f.site || '',
        contatoNome: f.contatoNome || '',
        contatoCpf: f.contatoCpf || '',
        contatoWhatsApp: f.contatoWhatsApp || '',
        contatoEmail: f.contatoEmail || '',
      });
    } catch (err) {
      setError(t('suppliers.saveError'));
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
      toast.error(t('suppliers.nameRequired'));
      return;
    }
    if (formData.contatoEmail && !EMAIL_REGEX.test(formData.contatoEmail)) {
      toast.error(t('suppliers.invalidEmail'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        razaoSocial: formData.razaoSocial?.trim() || null,
        cnpjCpf: formData.cnpjCpf?.trim() || null,
        inscricaoEstadual: formData.inscricaoEstadual?.trim() || null,
        endereco: formData.endereco?.trim() || null,
        telefone: formData.telefone?.trim() || null,
        site: formData.site?.trim() || null,
        contatoNome: formData.contatoNome?.trim() || null,
        contatoCpf: formData.contatoCpf?.trim() || null,
        contatoWhatsApp: formData.contatoWhatsApp?.trim() || null,
        contatoEmail: formData.contatoEmail?.trim() || null,
      };
      if (isEditing) await fornecedoresApi.update(id, payload);
      else await fornecedoresApi.create(payload);
      toast.success(isEditing ? t('suppliers.saveSuccessEdit') : t('suppliers.saveSuccessCreate'));
      navigate('/financeiro/fornecedores');
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('suppliers.saveError')));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('suppliers.loadingForm')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/fornecedores">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('suppliers.editTitle') : t('suppliers.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('suppliers.editSubtitle') : t('suppliers.newSubtitle')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>{t('suppliers.companyData')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('suppliers.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder={t('suppliers.placeholderName')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="razaoSocial">{t('suppliers.razaoSocial')}</Label>
                <Input id="razaoSocial" name="razaoSocial" value={formData.razaoSocial} onChange={handleChange} placeholder={t('suppliers.razaoSocial')} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="cnpjCpf">{t('suppliers.cnpjCpf')}</Label>
                <Input id="cnpjCpf" name="cnpjCpf" value={formData.cnpjCpf} onChange={handleChange} placeholder="00.000.000/0000-00 ou 000.000.000-00" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="inscricaoEstadual">{t('suppliers.inscricaoEstadual')}</Label>
                <Input id="inscricaoEstadual" name="inscricaoEstadual" value={formData.inscricaoEstadual} onChange={handleChange} placeholder={t('suppliers.inscricaoEstadual')} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="endereco">{t('suppliers.address')}</Label>
                <Input id="endereco" name="endereco" value={formData.endereco} onChange={handleChange} placeholder={t('suppliers.address')} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="telefone">{t('suppliers.phone')}</Label>
                <Input id="telefone" name="telefone" value={formData.telefone} onChange={handleChange} placeholder="(11) 99999-9999" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="site">{t('suppliers.site')}</Label>
                <Input id="site" name="site" value={formData.site} onChange={handleChange} placeholder="https://" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('suppliers.contactData')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="contatoNome">{t('suppliers.contactName')}</Label>
                <Input id="contatoNome" name="contatoNome" value={formData.contatoNome} onChange={handleChange} placeholder={t('suppliers.contactName')} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="contatoCpf">{t('suppliers.cpf')}</Label>
                <Input id="contatoCpf" name="contatoCpf" value={formData.contatoCpf} onChange={handleChange} placeholder="000.000.000-00" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="contatoWhatsApp">{t('suppliers.whatsapp')}</Label>
                <Input id="contatoWhatsApp" name="contatoWhatsApp" value={formData.contatoWhatsApp} onChange={handleChange} placeholder="11999998888" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="contatoEmail">{t('suppliers.email')}</Label>
                <Input id="contatoEmail" name="contatoEmail" type="email" value={formData.contatoEmail} onChange={handleChange} placeholder="email@exemplo.com" />
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex items-center space-x-4">
          <Button type="submit" disabled={loading}>
            <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/financeiro/fornecedores">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
