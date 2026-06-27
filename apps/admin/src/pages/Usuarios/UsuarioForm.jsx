import { useEffect, useState } from 'react';
import { X, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { usuariosApi, perfisAcessoApi, pessoasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function UsuarioForm({ id, onClose, onSuccess, pessoaIdInicial = null }) {
  const isEditing = Boolean(id);
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    modoPessoa: pessoaIdInicial ? 'existente' : 'nova',
    pessoaId: pessoaIdInicial ? String(pessoaIdInicial) : '',
    nome: '',
    email: '',
    senha: '',
    confirmarSenha: '',
    tipoUsuario: 1,
    ativo: true,
    perfilAcessoId: '',
  });
  const [perfis, setPerfis] = useState([]);
  const [pessoas, setPessoas] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);

      if (isEditing) {
        const [res, perfisRes] = await Promise.all([
          usuariosApi.getById(id),
          perfisAcessoApi.getAll(),
        ]);
        const u = res.data;
        setPerfis(perfisRes.data || []);
        setFormData({
          modoPessoa: 'existente',
          pessoaId: '',
          nome: u.nome || '',
          email: u.email || '',
          senha: '',
          confirmarSenha: '',
          tipoUsuario: u.tipoUsuario || 1,
          ativo: u.ativo !== undefined ? u.ativo : true,
          perfilAcessoId: String(u.perfilAcessoId || ''),
        });
        return;
      }

      const [perfisRes, pessoasRes, usuariosRes] = await Promise.all([
        perfisAcessoApi.getAll(),
        pessoasApi.getAll(),
        usuariosApi.getAll(),
      ]);
      setPerfis(perfisRes.data || []);
      const pessoaIdsComUsuario = new Set((usuariosRes.data || []).map((u) => u.pessoaId));
      const pessoaIdInicialNumero = pessoaIdInicial ? Number(pessoaIdInicial) : null;
      const pessoasFiltradas = (pessoasRes.data || []).filter((p) => {
        if (!p.ativo) return false;
        if (pessoaIdInicialNumero && p.id === pessoaIdInicialNumero) return true;
        return !pessoaIdsComUsuario.has(p.id);
      });
      setPessoas(pessoasFiltradas);

      if (pessoaIdInicial) {
        setFormData((prev) => ({
          ...prev,
          modoPessoa: 'existente',
          pessoaId: String(pessoaIdInicial),
        }));
      }
    } catch (err) {
      setError(t('usersManagement.form.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id, pessoaIdInicial]);

  const pessoaSelecionada = !isEditing && formData.pessoaId
    ? pessoas.find((p) => String(p.id) === String(formData.pessoaId))
    : null;

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : name === 'tipoUsuario' ? Number(value) : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!isEditing && formData.modoPessoa === 'existente' && !formData.pessoaId) {
      toast.error(t('usersManagement.form.validation.selectPerson'));
      return;
    }

    if (!isEditing && formData.modoPessoa === 'nova') {
      if (!formData.nome.trim()) {
        toast.error(t('usersManagement.form.validation.nameRequired'));
        return;
      }

      if (!formData.email.trim()) {
        toast.error(t('usersManagement.form.validation.emailRequired'));
        return;
      }

      if (!/.+@.+\..+/.test(formData.email)) {
        toast.error(t('usersManagement.form.validation.emailInvalid'));
        return;
      }
    }

    if (!isEditing && !formData.senha) {
      toast.error(t('usersManagement.form.validation.passwordRequired'));
      return;
    }

    if (!isEditing && formData.senha.length < 6) {
      toast.error(t('usersManagement.form.validation.passwordMin'));
      return;
    }

    if (!isEditing && formData.senha !== formData.confirmarSenha) {
      toast.error(t('usersManagement.form.validation.passwordMismatch'));
      return;
    }

    if (!formData.perfilAcessoId) {
      toast.error(t('usersManagement.form.validation.profileRequired'));
      return;
    }

    try {
      setLoading(true);
      if (isEditing) {
        await usuariosApi.update(id, {
          nome: formData.nome.trim(),
          email: formData.email.trim(),
          tipoUsuario: formData.tipoUsuario,
          ativo: formData.ativo,
          perfilAcessoId: Number(formData.perfilAcessoId),
        });
        toast.success(t('usersManagement.form.updateSuccess'));
      } else {
        await usuariosApi.create({
          pessoaId: formData.modoPessoa === 'existente' ? Number(formData.pessoaId) : null,
          nome: formData.modoPessoa === 'nova' ? formData.nome.trim() : '',
          email: formData.modoPessoa === 'nova' ? formData.email.trim() : null,
          senha: formData.senha,
          tipoUsuario: formData.tipoUsuario,
          perfilAcessoId: Number(formData.perfilAcessoId),
        });
        toast.success(t('usersManagement.form.createSuccess'));
      }
      if (onSuccess) onSuccess();
    } catch (err) {
      const errorMessage = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('usersManagement.form.saveError'));
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const userTypeOptions = [
    { value: 1, label: t('usersManagement.userTypes.administrator') },
    { value: 2, label: t('usersManagement.userTypes.portal') },
    { value: 3, label: t('usersManagement.userTypes.both') },
  ];

  if (loading && isEditing && !formData.nome) return <LoadingPage text={t('usersManagement.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <Card className="w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>{isEditing ? t('usersManagement.form.editTitle') : t('usersManagement.form.createTitle')}</CardTitle>
          <Button variant="ghost" size="sm" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {!isEditing && (
              <div className="space-y-2">
                <Label htmlFor="modoPessoa">{t('usersManagement.form.linkMode')} *</Label>
                <select
                  id="modoPessoa"
                  name="modoPessoa"
                  value={formData.modoPessoa}
                  onChange={handleChange}
                  className="w-full px-3 py-2 border rounded"
                >
                  <option value="existente">{t('usersManagement.form.linkModeExisting')}</option>
                  <option value="nova">{t('usersManagement.form.linkModeNew')}</option>
                </select>
              </div>
            )}

            {!isEditing && formData.modoPessoa === 'existente' && (
              <div className="space-y-2">
                <Label htmlFor="pessoaId">{t('usersManagement.form.person')} *</Label>
                <select
                  id="pessoaId"
                  name="pessoaId"
                  value={formData.pessoaId}
                  onChange={handleChange}
                  className="w-full px-3 py-2 border rounded"
                  required
                >
                  <option value="">{t('actions.select')}</option>
                  {pessoas.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nome} {p.email ? `(${p.email})` : ''}
                    </option>
                  ))}
                </select>
                {pessoaSelecionada && (
                  <p className="text-xs text-muted-foreground">
                    {t('usersManagement.form.personHint', { name: pessoaSelecionada.nome })}
                  </p>
                )}
              </div>
            )}

            {(isEditing || formData.modoPessoa === 'nova') && (
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="nome">{t('usersManagement.table.name')} *</Label>
                  <Input
                    id="nome"
                    name="nome"
                    value={formData.nome}
                    onChange={handleChange}
                    placeholder={t('usersManagement.form.placeholders.name')}
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="email">{t('usersManagement.table.email')} *</Label>
                  <Input
                    id="email"
                    name="email"
                    type="email"
                    value={formData.email}
                    onChange={handleChange}
                    placeholder={t('usersManagement.form.placeholders.email')}
                    required
                  />
                </div>
              </div>
            )}

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="tipoUsuario">{t('usersManagement.filters.userType')} *</Label>
                <select
                  id="tipoUsuario"
                  name="tipoUsuario"
                  value={formData.tipoUsuario}
                  onChange={handleChange}
                  className="w-full px-3 py-2 border rounded"
                  required
                >
                  {userTypeOptions.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="perfilAcessoId">{t('usersManagement.form.accessProfile')} *</Label>
                <select
                  id="perfilAcessoId"
                  name="perfilAcessoId"
                  value={formData.perfilAcessoId}
                  onChange={handleChange}
                  className="w-full px-3 py-2 border rounded"
                  required
                >
                  <option value="">{t('actions.select')}</option>
                  {perfis.map((p) => (
                    <option key={p.id} value={p.id}>{p.nome}</option>
                  ))}
                </select>
              </div>
              {isEditing && (
                <div className="space-y-2 flex items-center space-x-3 pt-6">
                  <input
                    type="checkbox"
                    id="ativo"
                    name="ativo"
                    checked={formData.ativo}
                    onChange={handleChange}
                    className="h-4 w-4"
                  />
                  <Label htmlFor="ativo" className="cursor-pointer">{t('usersManagement.form.activeUser')}</Label>
                </div>
              )}
            </div>

            {!isEditing && (
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="senha">{t('usersManagement.form.password')} *</Label>
                  <Input
                    id="senha"
                    name="senha"
                    type="password"
                    value={formData.senha}
                    onChange={handleChange}
                    placeholder={t('usersManagement.form.placeholders.password')}
                    required={!isEditing}
                    minLength={6}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="confirmarSenha">{t('usersManagement.form.confirmPassword')} *</Label>
                  <Input
                    id="confirmarSenha"
                    name="confirmarSenha"
                    type="password"
                    value={formData.confirmarSenha}
                    onChange={handleChange}
                    placeholder={t('usersManagement.form.placeholders.confirmPassword')}
                    required={!isEditing}
                  />
                </div>
              </div>
            )}

            <div className="flex items-center space-x-4 pt-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : t('actions.save')}
              </Button>
              <Button type="button" variant="outline" onClick={onClose}>
                {t('actions.cancel')}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}





