import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { equipesApi, voluntariosApi, pessoasApi, cargosApi, usuariosApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

export default function EquipeForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();
  const confirmDialog = useConfirmDialog();

  const [formData, setFormData] = useState({
    nome: '',
    area: '1',
    liderUsuarioId: '',
  });
  const [voluntarios, setVoluntarios] = useState([]);
  const [pessoas, setPessoas] = useState([]);
  const [usuarios, setUsuarios] = useState([]);
  const [cargos, setCargos] = useState([]);
  const [vinculoPessoaId, setVinculoPessoaId] = useState('');
  const [vinculoCargoId, setVinculoCargoId] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadingVinculo, setLoadingVinculo] = useState(false);
  const [error, setError] = useState(null);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      if (isEditing) {
        const [resEquipe, resPessoas, resCargos, resVol, resUsuarios] = await Promise.all([
          equipesApi.getById(id),
          pessoasApi.getAll(),
          cargosApi.getAll(),
          voluntariosApi.getByEquipe(id),
          usuariosApi.getAll(),
        ]);
        const e = resEquipe.data;
        setFormData({
          nome: e.nome || '',
          area: String(e.area || '1'),
          liderUsuarioId: e.liderUsuarioId ? String(e.liderUsuarioId) : '',
        });
        setPessoas(resPessoas.data || []);
        setCargos(resCargos.data || []);
        setVoluntarios(resVol.data || []);
        setUsuarios(resUsuarios.data || []);
      } else {
        const [resPessoas, resCargos, resUsuarios] = await Promise.all([
          pessoasApi.getAll(),
          cargosApi.getAll(),
          usuariosApi.getAll(),
        ]);
        setPessoas(resPessoas.data || []);
        setCargos(resCargos.data || []);
        setUsuarios(resUsuarios.data || []);
      }
    } catch (err) {
      setError(t('teamForm.errorLoad'));
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

  const handleVincular = async () => {
    if (!vinculoPessoaId || !vinculoCargoId) {
      toast.error(t('teamForm.links.validation.selectPersonAndRole'));
      return;
    }
    try {
      setLoadingVinculo(true);
      await voluntariosApi.create({
        pessoaId: Number(vinculoPessoaId),
        equipeId: Number(id),
        cargoId: Number(vinculoCargoId),
      });
      toast.success(t('teamForm.links.linkSuccess'));
      setVinculoPessoaId('');
      setVinculoCargoId('');
      const res = await voluntariosApi.getByEquipe(id);
      setVoluntarios(res.data || []);
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('teamForm.links.linkError')));
    } finally {
      setLoadingVinculo(false);
    }
  };

  const handleRemoverVinculo = (voluntarioId) => {
    confirmDialog.show({
      title: t('teamForm.links.removeTitle'),
      description: t('teamForm.links.removeConfirm'),
      confirmText: t('actions.remove'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await voluntariosApi.delete(voluntarioId);
          toast.success(t('teamForm.links.removeSuccess'));
          const res = await voluntariosApi.getByEquipe(id);
          setVoluntarios(res.data || []);
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('teamForm.links.removeError')));
          throw err;
        }
      },
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error(t('teamForm.validation.nameRequired'));
      return;
    }
    if (!['1', '2', '3'].includes(formData.area)) {
      toast.error(t('teamForm.validation.invalidArea'));
      return;
    }
    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        area: Number(formData.area),
        liderUsuarioId: formData.liderUsuarioId ? Number(formData.liderUsuarioId) : null,
      };
      if (isEditing) await equipesApi.update(id, payload);
      else await equipesApi.create(payload);
      toast.success(isEditing ? t('teamForm.updateSuccess') : t('teamForm.createSuccess'));
      navigate('/equipes');
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('teamForm.saveError')));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('teamForm.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/equipes">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('volunteer.teams.edit') : t('volunteer.teams.new')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('teamForm.editSubtitle') : t('teamForm.createSubtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('volunteer.teams.edit') : t('volunteer.teams.create')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('teamForm.fields.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder={t('teamForm.placeholders.name')} required />
              </div>

              <div className="space-y-2">
                <Label htmlFor="area">{t('teamForm.fields.area')} *</Label>
                <select id="area" name="area" value={formData.area} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="1">{t('teamForm.area.green')}</option>
                  <option value="2">{t('teamForm.area.red')}</option>
                  <option value="3">{t('teamForm.area.orange')}</option>
                </select>
              </div>

              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="liderUsuarioId">{t('teamForm.fields.teamLeader')}</Label>
                <select
                  id="liderUsuarioId"
                  name="liderUsuarioId"
                  value={formData.liderUsuarioId}
                  onChange={handleChange}
                  className="w-full px-3 py-2 border rounded"
                >
                  <option value="">{t('teamForm.noLeader')}</option>
                  {usuarios.map((usuario) => (
                    <option key={usuario.id} value={usuario.id}>
                      {usuario.nome}{usuario.emailLogin ? ` — ${usuario.emailLogin}` : ''}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('volunteer.teams.update') : t('volunteer.teams.create'))}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/equipes">{t('actions.cancel')}</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {isEditing && (
        <Card>
          <CardHeader>
            <CardTitle>{t('teamForm.links.title')}</CardTitle>
          </CardHeader>
          <CardContent>
              <div className="space-y-4">
                <div className="flex flex-wrap gap-2 items-end">
                  <div className="space-y-1 min-w-[200px]">
                    <Label htmlFor="vinculoPessoa">{t('teamForm.links.person')}</Label>
                    <select
                      id="vinculoPessoa"
                      value={vinculoPessoaId}
                      onChange={(e) => setVinculoPessoaId(e.target.value)}
                      className="w-full px-3 py-2 border rounded"
                    >
                      <option value="">{t('actions.select')}</option>
                      {pessoas.map((p) => (
                        <option key={p.id} value={p.id}>{p.nome}{p.email ? ` — ${p.email}` : ''}</option>
                      ))}
                    </select>
                  </div>
                  <div className="space-y-1 min-w-[160px]">
                    <Label htmlFor="vinculoCargo">{t('teamForm.links.role')}</Label>
                    <select
                      id="vinculoCargo"
                      value={vinculoCargoId}
                      onChange={(e) => setVinculoCargoId(e.target.value)}
                      className="w-full px-3 py-2 border rounded"
                    >
                      <option value="">{t('actions.select')}</option>
                      {cargos.map((c) => (
                        <option key={c.id} value={c.id}>{c.nome}</option>
                      ))}
                    </select>
                  </div>
                  <Button type="button" onClick={handleVincular} disabled={loadingVinculo}>
                    <Plus className="h-4 w-4 mr-2" /> {t('teamForm.links.link')}
                  </Button>
                </div>
                {voluntarios.length > 0 ? (
                  <div className="border rounded overflow-hidden">
                    <table className="w-full text-sm">
                      <thead className="bg-muted/50">
                        <tr>
                          <th className="text-left p-2">{t('teamForm.links.table.name')}</th>
                          <th className="text-left p-2">{t('teamForm.links.table.role')}</th>
                          <th className="w-20 p-2"></th>
                        </tr>
                      </thead>
                      <tbody>
                        {voluntarios.map((v) => (
                          <tr key={v.id} className="border-t">
                            <td className="p-2">{v.nome}</td>
                            <td className="p-2">{v.nomeCargo}</td>
                            <td className="p-2">
                              <Button type="button" variant="ghost" size="sm" onClick={() => handleRemoverVinculo(v.id)}>
                                <Trash2 className="h-4 w-4 text-destructive" />
                              </Button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <p className="text-muted-foreground text-sm">{t('teamForm.links.empty')}</p>
                )}
              </div>
          </CardContent>
        </Card>
      )}

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={confirmDialog.hide}
        onConfirm={confirmDialog.handleConfirm}
        title={confirmDialog.config.title}
        description={confirmDialog.config.description}
        confirmText={confirmDialog.config.confirmText}
        cancelText={confirmDialog.config.cancelText}
        variant={confirmDialog.config.variant}
        loading={confirmDialog.loading}
      />
    </div>
  );
}
