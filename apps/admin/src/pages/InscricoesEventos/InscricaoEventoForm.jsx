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
import { inscricoesEventosApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

const STATUS_OPTIONS = (t) => [
  { value: 1, label: t('eventRegistrations.status.pending') },
  { value: 2, label: t('eventRegistrations.status.confirmed') },
  { value: 3, label: t('eventRegistrations.status.canceled') },
  { value: 4, label: t('eventRegistrations.status.present') },
];

export default function InscricaoEventoForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [formData, setFormData] = useState({
    status: 1,
    quantidadeAcompanhantes: 0,
    observacoes: '',
    observacoesInternas: '',
  });
  const [inscricao, setInscricao] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const load = async () => {
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await inscricoesEventosApi.getById(id);
      const i = res.data;
      setInscricao(i);
      setFormData({
        status: i.status || 1,
        quantidadeAcompanhantes: i.quantidadeAcompanhantes || 0,
        observacoes: i.observacoes || '',
        observacoesInternas: i.observacoesInternas || '',
      });
    } catch (err) {
      setError('Erro ao carregar inscrição');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: name === 'quantidadeAcompanhantes' || name === 'status' ? Number(value) : value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      setLoading(true);
      const payload = {
        status: formData.status,
        quantidadeAcompanhantes: formData.quantidadeAcompanhantes || 0,
        observacoes: formData.observacoes.trim() || null,
        observacoesInternas: formData.observacoesInternas.trim() || null,
      };
      await inscricoesEventosApi.update(id, payload);
      toast.success(t('eventRegistrations.saveSuccess', 'Inscrição salva com sucesso'));
      navigate(`/inscricoes-eventos/${id}`);
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao salvar inscrição'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing && !inscricao) {
    return <LoadingPage text={t('eventRegistrations.loading', 'Carregando inscrição...')} />;
  }
  if (error) return <ErrorPage message={error} onRetry={load} />;
  if (isEditing && !inscricao) return <div>{t('eventRegistrations.notFound', 'Inscrição não encontrada')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to={isEditing ? `/inscricoes-eventos/${id}` : '/inscricoes-eventos'}>
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('eventRegistrations.backToRegistrations')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">
            {isEditing ? t('eventRegistrations.edit') : t('eventRegistrations.new')}
          </h1>
          <p className="text-muted-foreground">
            {isEditing
              ? t('eventRegistrations.editSubtitle', 'Atualize as informações da inscrição')
              : t('eventRegistrations.createSubtitle', 'Cadastre uma nova inscrição')}
          </p>
        </div>
      </div>

      {isEditing && inscricao && (
        <Card>
          <CardHeader>
            <CardTitle>{t('eventRegistrations.participantCardTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="text-sm font-medium text-muted-foreground">
                  {t('eventRegistrations.participantNameLabel')}
                </label>
                <p className="text-base">{inscricao.nome}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">
                  {t('eventRegistrations.participantWhatsappLabel')}
                </label>
                <p className="text-base">{inscricao.whatsApp}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">
                  {t('eventRegistrations.participantEmailLabel')}
                </label>
                <p className="text-base">{inscricao.email || '-'}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">
                  {t('eventRegistrations.eventTitleLabel')}
                </label>
                <p className="text-base">{inscricao.eventoTitulo}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle>{isEditing ? t('eventRegistrations.edit') : t('eventRegistrations.create')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="status">{t('eventRegistrations.form.statusLabel')} *</Label>
                <select id="status" name="status" value={formData.status} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  {STATUS_OPTIONS(t).map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="quantidadeAcompanhantes">
                  {t('eventRegistrations.form.companionsLabel')}
                </Label>
                <Input
                  id="quantidadeAcompanhantes"
                  name="quantidadeAcompanhantes"
                  type="number"
                  min="0"
                  value={formData.quantidadeAcompanhantes}
                  onChange={handleChange}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="observacoes">
                {t('eventRegistrations.form.participantNotesLabel')}
              </Label>
              <Textarea
                id="observacoes"
                name="observacoes"
                value={formData.observacoes}
                onChange={handleChange}
                placeholder={t('eventRegistrations.form.participantNotesPlaceholder')}
                rows={3}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="observacoesInternas">
                {t('eventRegistrations.form.internalNotesLabel')}
              </Label>
              <Textarea
                id="observacoesInternas"
                name="observacoesInternas"
                value={formData.observacoesInternas}
                onChange={handleChange}
                placeholder={t('eventRegistrations.form.internalNotesPlaceholder')}
                rows={3}
              />
            </div>

            <div className="flex items-center space-x-4">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : t('actions.save')}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to={isEditing ? `/inscricoes-eventos/${id}` : '/inscricoes-eventos'}>
                  {t('actions.cancel')}
                </Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}







