import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { comunicacaoSegmentosApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function ComunicacaoSegmentoForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(isEditing);
  const [error, setError] = useState(null);
  const [formData, setFormData] = useState({
    nome: '',
    descricao: '',
    publicoAlvo: 'visitantes',
    ativo: true,
  });

  const publicosOptions = [
    { value: 'visitantes', label: t('communicationSegmentsManagement.form.audiences.visitors') },
    { value: 'membros', label: t('communicationSegmentsManagement.form.audiences.members') },
    { value: 'voluntarios', label: t('communicationSegmentsManagement.form.audiences.volunteers') },
    { value: 'responsaveis-kids', label: t('communicationSegmentsManagement.form.audiences.kidsGuardians') },
    { value: 'pessoas', label: t('communicationSegmentsManagement.form.audiences.people') },
  ];

  useEffect(() => {
    if (!isEditing) return;

    const load = async () => {
      try {
        const response = await comunicacaoSegmentosApi.getById(id);
        const data = response.data;
        setFormData({
          nome: data.nome || '',
          descricao: data.descricao || '',
          publicoAlvo: data.publicoAlvo || 'visitantes',
          ativo: data.ativo ?? true,
        });
      } catch (err) {
        setError(getApiErrorMessage(err, t('communicationSegmentsManagement.form.errorLoad')));
      } finally {
        setPageLoading(false);
      }
    };

    load();
  }, [id, isEditing]);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.nome.trim() || !formData.publicoAlvo.trim()) {
      toast.error(t('communicationSegmentsManagement.form.validation.requiredFields'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        descricao: formData.descricao.trim() || null,
        publicoAlvo: formData.publicoAlvo,
        ...(isEditing ? { ativo: formData.ativo } : {}),
      };

      if (isEditing) await comunicacaoSegmentosApi.update(id, payload);
      else await comunicacaoSegmentosApi.create(payload);

      toast.success(isEditing
        ? t('communicationSegmentsManagement.form.updateSuccess')
        : t('communicationSegmentsManagement.form.createSuccess'));
      navigate('/comunicacao/segmentos');
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationSegmentsManagement.form.errorSave')));
    } finally {
      setLoading(false);
    }
  };

  if (pageLoading) return <LoadingPage text={t('communicationSegmentsManagement.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={() => window.location.reload()} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/comunicacao/segmentos">
            <ArrowLeft className="w-4 h-4" />
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold text-foreground">
            {isEditing ? t('communicationSegmentsManagement.form.editTitle') : t('communicationSegmentsManagement.form.newTitle')}
          </h1>
          <p className="text-muted-foreground mt-1">{t('communicationSegmentsManagement.form.subtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('communicationSegmentsManagement.form.cardTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="nome">{t('communicationSegmentsManagement.form.fields.name')}</Label>
              <Input
                id="nome"
                value={formData.nome}
                placeholder={t('communicationSegmentsManagement.form.fields.namePlaceholder')}
                onChange={(e) => setFormData((prev) => ({ ...prev, nome: e.target.value }))}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="descricao">{t('communicationSegmentsManagement.form.fields.description')}</Label>
              <Input
                id="descricao"
                value={formData.descricao}
                placeholder={t('communicationSegmentsManagement.form.fields.descriptionPlaceholder')}
                onChange={(e) => setFormData((prev) => ({ ...prev, descricao: e.target.value }))}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="publicoAlvo">{t('communicationSegmentsManagement.form.fields.targetAudience')}</Label>
              <select id="publicoAlvo" value={formData.publicoAlvo} onChange={(e) => setFormData((prev) => ({ ...prev, publicoAlvo: e.target.value }))} className="w-full rounded-md border border-input bg-background px-3 py-2">
                {publicosOptions.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </div>

            {isEditing && (
              <label className="flex items-center gap-3 rounded-lg border border-border p-3 cursor-pointer">
                <input type="checkbox" checked={formData.ativo} onChange={(e) => setFormData((prev) => ({ ...prev, ativo: e.target.checked }))} />
                <span className="text-sm font-medium">{t('communicationSegmentsManagement.form.fields.active')}</span>
              </label>
            )}

            <div className="flex justify-end gap-3 pt-4 border-t border-border">
              <Button type="button" variant="outline" asChild>
                <Link to="/comunicacao/segmentos">{t('actions.cancel')}</Link>
              </Button>
              <Button type="submit" disabled={loading}>
                <Save className="w-4 h-4 mr-2" />
                {loading ? t('actions.saving') : t('actions.save')}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
