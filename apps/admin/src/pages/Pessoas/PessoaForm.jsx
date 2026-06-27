import { useState, useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { useFormValidation } from '@/hooks/useFormValidation';
import { pessoasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function PessoaForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const validationRules = {
    nome: {
      required: true,
      requiredMessage: t('people.form.validation.nameRequired'),
      minLength: 2,
      minLengthMessage: t('people.form.validation.nameMin'),
      maxLength: 100,
    },
    email: {
      email: true,
      emailMessage: t('people.form.validation.emailInvalid'),
    },
    telefone: {
      maxLength: 20,
    },
    whatsApp: {
      maxLength: 20,
    },
  };

  const {
    values: formData,
    errors,
    touched,
    handleChange: handleValidationChange,
    handleBlur,
    validate,
    reset: resetValidation,
    setValues: setFormData,
  } = useFormValidation(validationRules, {
    nome: '',
    email: '',
    telefone: '',
    whatsApp: '',
    dataNascimento: '',
    tipoPessoa: 'Adulto',
    ativo: true,
  });

  const loadPessoa = async () => {
    if (!isEditing) return;

    try {
      setLoading(true);
      setError(null);
      const response = await pessoasApi.getById(id);
      const pessoa = response.data;
      
      const loadedData = {
        nome: pessoa.nome || '',
        email: pessoa.email || '',
        telefone: pessoa.telefone || '',
        whatsApp: pessoa.whatsApp || '',
        dataNascimento: pessoa.dataNascimento 
          ? pessoa.dataNascimento.split('T')[0] 
          : '',
        tipoPessoa: pessoa.tipoPessoa || 'Adulto',
        ativo: pessoa.ativo !== undefined ? pessoa.ativo : true,
      };
      setFormData(loadedData);
      resetValidation(loadedData);
    } catch (err) {
      setError(t('people.form.errorLoad'));
      console.error('Erro ao carregar pessoa:', err);
      toast.error(t('people.form.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPessoa();
  }, [id]);

  const normalizePhone = (phone) => {
    return phone.replace(/\D/g, '');
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    const finalValue = type === 'checkbox' ? checked : value;
    handleValidationChange(name, finalValue);
  };

  const handlePhoneChange = (name, value) => {
    const normalized = normalizePhone(value);
    handleValidationChange(name, normalized);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validate()) {
      const firstError = Object.values(errors)[0];
      if (firstError) {
        toast.error(firstError);
      }
      return;
    }

    try {
      setLoading(true);
      
      const payload = {
        nome: formData.nome.trim(),
        email: formData.email?.trim() || null,
        telefone: formData.telefone ? normalizePhone(formData.telefone) : null,
        whatsApp: formData.whatsApp ? normalizePhone(formData.whatsApp) : null,
        dataNascimento: formData.dataNascimento 
          ? new Date(formData.dataNascimento + 'T00:00:00').toISOString()
          : null,
        tipoPessoa: formData.tipoPessoa,
        ativo: formData.ativo,
      };

      if (isEditing) {
        await pessoasApi.update(id, payload);
        toast.success(t('people.form.updateSuccess'));
      } else {
        await pessoasApi.create(payload);
        toast.success(t('people.form.createSuccess'));
      }

      navigate('/pessoas');
    } catch (err) {
      const errorMessage = err.response?.data?.message || 
                          err.response?.data?.error ||
                          t('people.form.saveError');
      toast.error(errorMessage);
      console.error('Erro ao salvar pessoa:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) {
    return <LoadingPage text={t('people.form.loading')} />;
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadPessoa} />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/pessoas">
            <ArrowLeft className="h-4 w-4 mr-2" />
            {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">
            {isEditing ? t('people.edit') : t('people.new')}
          </h1>
          <p className="text-muted-foreground">
            {isEditing ? t('people.form.editSubtitle') : t('people.form.createSubtitle')}
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>
            {isEditing ? t('people.edit') : t('people.create')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('people.form.fields.name')} *</Label>
                <Input
                  id="nome"
                  name="nome"
                  value={formData.nome}
                  onChange={handleChange}
                  onBlur={() => handleBlur('nome')}
                  placeholder={t('people.form.placeholders.name')}
                  className={touched.nome && errors.nome ? 'border-destructive' : ''}
                />
                {touched.nome && errors.nome && (
                  <p className="text-sm text-destructive mt-1">{errors.nome}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">{t('people.form.fields.email')}</Label>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  onBlur={() => handleBlur('email')}
                  placeholder={t('people.form.placeholders.email')}
                  className={touched.email && errors.email ? 'border-destructive' : ''}
                />
                {touched.email && errors.email && (
                  <p className="text-sm text-destructive mt-1">{errors.email}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="telefone">{t('people.form.fields.phone')}</Label>
                <Input
                  id="telefone"
                  name="telefone"
                  value={formData.telefone}
                  onChange={(e) => handlePhoneChange('telefone', e.target.value)}
                  onBlur={() => handleBlur('telefone')}
                  placeholder={t('people.form.placeholders.phone')}
                  className={touched.telefone && errors.telefone ? 'border-destructive' : ''}
                />
                {touched.telefone && errors.telefone && (
                  <p className="text-sm text-destructive mt-1">{errors.telefone}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="whatsApp">{t('people.form.fields.whatsapp')}</Label>
                <Input
                  id="whatsApp"
                  name="whatsApp"
                  value={formData.whatsApp}
                  onChange={(e) => handlePhoneChange('whatsApp', e.target.value)}
                  onBlur={() => handleBlur('whatsApp')}
                  placeholder={t('people.form.placeholders.whatsapp')}
                  className={touched.whatsApp && errors.whatsApp ? 'border-destructive' : ''}
                />
                {touched.whatsApp && errors.whatsApp && (
                  <p className="text-sm text-destructive mt-1">{errors.whatsApp}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="dataNascimento">{t('people.form.fields.birthDate')}</Label>
                <Input
                  id="dataNascimento"
                  name="dataNascimento"
                  type="date"
                  value={formData.dataNascimento}
                  onChange={handleChange}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="tipoPessoa">{t('people.form.fields.personType')}</Label>
                <Select
                  value={formData.tipoPessoa}
                  onValueChange={(value) => setFormData(prev => ({ ...prev, tipoPessoa: value }))}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Adulto">{t('people.form.personType.adult')}</SelectItem>
                    <SelectItem value="Crianca">{t('people.form.personType.child')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2 flex items-center space-x-2">
                <Switch
                  id="ativo"
                  checked={formData.ativo}
                  onCheckedChange={(checked) => 
                    setFormData(prev => ({ ...prev, ativo: checked }))
                  }
                />
                <Label htmlFor="ativo" className="cursor-pointer">
                  {t('people.form.fields.active')}
                </Label>
              </div>
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" />
                {loading ? t('actions.saving') : (isEditing ? t('people.update') : t('people.create'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/pessoas">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}



