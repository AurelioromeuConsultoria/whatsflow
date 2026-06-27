import React, { useState } from 'react';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { comunicacaoCampanhasApi, comunicacaoDiagnosticoApi, comunicacaoSegmentosApi, comunicacaoTemplatesApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';

const healthCheckMap = {
  1: 'evolution_api_configuration',
  2: 'email_configuration',
  3: 'push_configuration',
};

const sampleRecipient = {
  Nome: 'Maria Souza',
  PrimeiroNome: 'Maria',
  PublicoAlvo: 'visitantes',
  Campanha: 'Boas-vindas',
  Link: 'https://appigreja.local/convite',
};

function renderPreview(template, formData) {
  const values = {
    ...sampleRecipient,
    PublicoAlvo: formData.publicoAlvo.trim() || sampleRecipient.PublicoAlvo,
    Campanha: formData.nome.trim() || sampleRecipient.Campanha,
  };

  const interpolate = (text) => {
    if (!text) return '';

    return text
      .replaceAll('{Nome}', values.Nome)
      .replaceAll('{PrimeiroNome}', values.PrimeiroNome)
      .replaceAll('{PublicoAlvo}', values.PublicoAlvo)
      .replaceAll('{Campanha}', values.Campanha)
      .replaceAll('{Link}', values.Link);
  };

  return {
    assunto: interpolate(template?.assunto) || values.Campanha,
    corpo: interpolate(template?.corpo) || values.Campanha,
    corpoHtml: interpolate(template?.corpoHtml),
  };
}

function getEstimativaPorCanal(estimativa, canal) {
  if (!estimativa) return null;

  switch (Number(canal)) {
    case 1: return estimativa.comWhatsApp;
    case 2: return estimativa.comEmail;
    case 3: return estimativa.comPush;
    case 4: return estimativa.comNotificacaoInterna;
    default: return null;
  }
}

export default function ComunicacaoCampanhaForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const canaisDisponiveis = [
    { value: 1, label: t('communicationCampaignForm.channels.whatsapp') },
    { value: 2, label: t('communicationCampaignForm.channels.email') },
    { value: 3, label: t('communicationCampaignForm.channels.push') },
    { value: 4, label: t('communicationCampaignForm.channels.internalNotification') },
  ];
  const [loading, setLoading] = useState(false);
  const [templates, setTemplates] = useState([]);
  const [segmentos, setSegmentos] = useState([]);
  const [healthChecks, setHealthChecks] = useState({});
  const [estimativa, setEstimativa] = useState(null);
  const [formData, setFormData] = useState({
    nome: '',
    objetivo: '',
    segmentoId: '',
    publicoAlvo: '',
    dataAgendamento: '',
    observacao: '',
    canais: [{ canal: 1, templateId: '' }],
  });

  useEffect(() => {
    const loadDependencies = async () => {
      try {
        const [templatesResponse, segmentosResponse, healthResponse] = await Promise.all([
          comunicacaoTemplatesApi.getAll(),
          comunicacaoSegmentosApi.getAll(),
          comunicacaoDiagnosticoApi.getHealth(),
        ]);

        setTemplates(templatesResponse.data || []);
        setSegmentos(segmentosResponse.data || []);
        setHealthChecks(healthResponse.data?.checks || {});
      } catch {
        setTemplates([]);
        setSegmentos([]);
        setHealthChecks({});
      }
    };

    loadDependencies();
  }, []);

  useEffect(() => {
    const loadEstimativa = async () => {
      if (!formData.segmentoId && !formData.publicoAlvo.trim()) {
        setEstimativa(null);
        return;
      }

      try {
        const response = await comunicacaoSegmentosApi.getEstimativa({
          segmentoId: formData.segmentoId || undefined,
          publicoAlvo: formData.segmentoId ? undefined : formData.publicoAlvo.trim(),
        });
        setEstimativa(response.data || null);
      } catch {
        setEstimativa(null);
      }
    };

    loadEstimativa();
  }, [formData.segmentoId, formData.publicoAlvo]);

  const toggleCanal = (value) => {
    setFormData((prev) => ({
      ...prev,
      canais: prev.canais.some((item) => item.canal === value)
        ? prev.canais.filter((item) => item.canal !== value)
        : [...prev.canais, { canal: value, templateId: '' }],
    }));
  };

  const updateCanalTemplate = (canal, templateId) => {
    setFormData((prev) => ({
      ...prev,
      canais: prev.canais.map((item) => item.canal === canal ? { ...item, templateId } : item),
    }));
  };

  const getSelectedTemplate = (canal, templateId) => templates.find((template) => Number(template.canal) === canal && Number(template.id) === Number(templateId));

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.nome.trim() || !formData.objetivo.trim() || !formData.publicoAlvo.trim() || formData.canais.length === 0) {
      toast.error(t('communicationCampaignForm.validation.requiredFields'));
      return;
    }

    try {
      setLoading(true);
      await comunicacaoCampanhasApi.create({
        nome: formData.nome.trim(),
        objetivo: formData.objetivo.trim(),
        publicoAlvo: formData.publicoAlvo.trim(),
        dataAgendamento: formData.dataAgendamento || null,
        canais: formData.canais.map((item, index) => ({
          canal: item.canal,
          templateId: item.templateId ? Number(item.templateId) : null,
          prioridade: index + 1,
        })),
      });
      toast.success(t('communicationCampaignForm.createSuccess'));
      navigate('/comunicacao/campanhas');
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationCampaignForm.createError')));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/comunicacao/campanhas">
            <ArrowLeft className="w-4 h-4" />
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold text-foreground">{t('communicationCampaignForm.title')}</h1>
          <p className="text-muted-foreground mt-1">{t('communicationCampaignForm.subtitle')}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('communicationCampaignForm.cardTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('communicationCampaignForm.fields.name')}</Label>
                <Input id="nome" value={formData.nome} onChange={(e) => setFormData((prev) => ({ ...prev, nome: e.target.value }))} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="objetivo">{t('communicationCampaignForm.fields.objective')}</Label>
                <Input id="objetivo" value={formData.objetivo} onChange={(e) => setFormData((prev) => ({ ...prev, objetivo: e.target.value }))} placeholder={t('communicationCampaignForm.fields.objectivePlaceholder')} />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="segmentoId">{t('communicationCampaignForm.fields.savedSegment')}</Label>
              <select
                id="segmentoId"
                value={formData.segmentoId}
                onChange={(e) => {
                  const segmentoId = e.target.value;
                  const segmento = segmentos.find((item) => String(item.id) === segmentoId);
                  setFormData((prev) => ({
                    ...prev,
                    segmentoId,
                    publicoAlvo: segmento?.publicoAlvo || prev.publicoAlvo,
                  }));
                }}
                className="w-full rounded-md border border-input bg-background px-3 py-2"
              >
                <option value="">{t('communicationCampaignForm.fields.noSavedSegment')}</option>
                {segmentos.filter((item) => item.ativo).map((segmento) => (
                  <option key={segmento.id} value={segmento.id}>{segmento.nome}</option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="publicoAlvo">{t('communicationCampaignForm.fields.targetAudience')}</Label>
              <Input id="publicoAlvo" value={formData.publicoAlvo} onChange={(e) => setFormData((prev) => ({ ...prev, publicoAlvo: e.target.value }))} placeholder={t('communicationCampaignForm.fields.targetAudiencePlaceholder')} />
            </div>

            {estimativa && (
              <div className="rounded-xl border border-border bg-muted/20 p-4 space-y-3">
                <div className="font-medium">{t('communicationCampaignForm.audienceEstimate.title')}</div>
                <div className="grid grid-cols-2 md:grid-cols-5 gap-3 text-sm">
                  <div className="rounded-lg border border-border bg-background p-3">
                    <div className="text-muted-foreground">{t('communicationCampaignForm.audienceEstimate.total')}</div>
                    <div className="text-xl font-semibold mt-1">{estimativa.totalDestinatarios}</div>
                  </div>
                  <div className="rounded-lg border border-border bg-background p-3">
                    <div className="text-muted-foreground">{t('communicationCampaignForm.channels.whatsapp')}</div>
                    <div className="text-xl font-semibold mt-1">{estimativa.comWhatsApp}</div>
                  </div>
                  <div className="rounded-lg border border-border bg-background p-3">
                    <div className="text-muted-foreground">{t('communicationCampaignForm.channels.email')}</div>
                    <div className="text-xl font-semibold mt-1">{estimativa.comEmail}</div>
                  </div>
                  <div className="rounded-lg border border-border bg-background p-3">
                    <div className="text-muted-foreground">{t('communicationCampaignForm.channels.push')}</div>
                    <div className="text-xl font-semibold mt-1">{estimativa.comPush}</div>
                  </div>
                  <div className="rounded-lg border border-border bg-background p-3">
                    <div className="text-muted-foreground">{t('communicationCampaignForm.channels.internalNotificationShort')}</div>
                    <div className="text-xl font-semibold mt-1">{estimativa.comNotificacaoInterna}</div>
                  </div>
                </div>
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="dataAgendamento">{t('communicationCampaignForm.fields.scheduling')}</Label>
              <Input type="datetime-local" id="dataAgendamento" value={formData.dataAgendamento} onChange={(e) => setFormData((prev) => ({ ...prev, dataAgendamento: e.target.value }))} />
            </div>

            <div className="space-y-3">
              <Label>{t('communicationCampaignForm.fields.channels')}</Label>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {canaisDisponiveis.map((canal) => (
                  <label key={canal.value} className="flex items-center gap-3 rounded-lg border border-border p-3 cursor-pointer">
                    <input type="checkbox" checked={formData.canais.some((item) => item.canal === canal.value)} onChange={() => toggleCanal(canal.value)} />
                    <span className="text-sm font-medium">{canal.label}</span>
                  </label>
                ))}
              </div>
            </div>

            {formData.canais.length > 0 && (
              <div className="space-y-3">
                <Label>{t('communicationCampaignForm.fields.templateByChannel')}</Label>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {formData.canais.map((item) => {
                    const options = templates.filter((template) => Number(template.canal) === item.canal);
                    const canalLabel = canaisDisponiveis.find((canal) => canal.value === item.canal)?.label || t('communicationCampaignForm.channels.fallback', { canal: item.canal });

                    return (
                      <div key={item.canal} className="space-y-2">
                        <Label>{canalLabel}</Label>
                        <select
                          value={item.templateId}
                          onChange={(e) => updateCanalTemplate(item.canal, e.target.value)}
                          className="w-full rounded-md border border-input bg-background px-3 py-2"
                        >
                          <option value="">{t('communicationCampaignForm.fields.noTemplate')}</option>
                          {options.map((template) => (
                            <option key={template.id} value={template.id}>{template.nome}</option>
                          ))}
                        </select>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}

            {formData.canais.length > 0 && (
              <div className="space-y-3">
                <Label>{t('communicationCampaignForm.diagnostics.title')}</Label>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {formData.canais.map((item) => {
                    const canalLabel = canaisDisponiveis.find((canal) => canal.value === item.canal)?.label || t('communicationCampaignForm.channels.fallback', { canal: item.canal });
                    const health = healthChecks[healthCheckMap[item.canal]];
                    const ok = !health || health.status === 'Healthy';
                    const volumeCanal = getEstimativaPorCanal(estimativa, item.canal);
                    const semAudiencia = volumeCanal === 0;

                    return (
                      <div key={`diagnostico-${item.canal}`} className="rounded-xl border border-border bg-muted/20 p-4 space-y-2">
                        <div className="flex items-center justify-between gap-3">
                          <div className="font-medium">{canalLabel}</div>
                          <div className="flex items-center gap-2">
                            {semAudiencia && <Badge variant="destructive">{t('communicationCampaignForm.diagnostics.noAudience')}</Badge>}
                            <Badge variant={ok ? 'secondary' : 'destructive'}>
                              {ok ? t('communicationCampaignForm.diagnostics.configurationOk') : t('communicationCampaignForm.diagnostics.configurationPending')}
                            </Badge>
                          </div>
                        </div>
                        <p className="text-sm text-muted-foreground">
                          {health?.description || t('communicationCampaignForm.diagnostics.noDetailedDiagnosis')}
                        </p>
                        {volumeCanal !== null && (
                          <p className="text-xs text-muted-foreground">
                            {t('communicationCampaignForm.diagnostics.estimatedAudience', { count: volumeCanal })}
                          </p>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
            )}

            {formData.canais.length > 0 && (
              <div className="space-y-3">
                <div className="space-y-1">
                  <Label>{t('communicationCampaignForm.preview.title')}</Label>
                  <p className="text-xs text-muted-foreground">
                    {t('communicationCampaignForm.preview.description')}
                  </p>
                </div>
                <div className="grid grid-cols-1 gap-4">
                  {formData.canais.map((item) => {
                    const canalLabel = canaisDisponiveis.find((canal) => canal.value === item.canal)?.label || t('communicationCampaignForm.channels.fallback', { canal: item.canal });
                    const template = getSelectedTemplate(item.canal, item.templateId);
                    const preview = renderPreview(template, formData);

                    return (
                      <div key={`preview-${item.canal}`} className="rounded-xl border border-border bg-card p-4 space-y-3">
                        <div className="flex items-center justify-between gap-3">
                          <div>
                            <div className="font-medium">{canalLabel}</div>
                            <div className="text-sm text-muted-foreground">{template?.nome || t('communicationCampaignForm.preview.noTemplateFallback')}</div>
                          </div>
                          <Badge variant="outline">{template ? t('communicationCampaignForm.preview.withTemplate') : t('communicationCampaignForm.preview.fallback')}</Badge>
                        </div>

                        {Number(item.canal) === 2 && (
                          <div className="space-y-1">
                            <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('communicationCampaignForm.preview.subject')}</div>
                            <div className="rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">{preview.assunto}</div>
                          </div>
                        )}

                        {[3, 4].includes(Number(item.canal)) && (
                          <div className="space-y-1">
                            <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('communicationCampaignForm.preview.titleLabel')}</div>
                            <div className="rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">{preview.assunto}</div>
                          </div>
                        )}

                        <div className="space-y-1">
                          <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('communicationCampaignForm.preview.message')}</div>
                          <div className="rounded-md border border-border bg-muted/30 px-3 py-3 text-sm whitespace-pre-wrap">{preview.corpo}</div>
                        </div>

                        {Number(item.canal) === 2 && preview.corpoHtml && (
                          <div className="space-y-1">
                            <div className="text-xs uppercase tracking-wide text-muted-foreground">{t('communicationCampaignForm.preview.html')}</div>
                            <div className="rounded-md border border-border bg-muted/30 px-3 py-3 text-sm whitespace-pre-wrap">{preview.corpoHtml}</div>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="observacao">{t('communicationCampaignForm.fields.operationalNote')}</Label>
              <Textarea id="observacao" rows={4} value={formData.observacao} onChange={(e) => setFormData((prev) => ({ ...prev, observacao: e.target.value }))} />
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t border-border">
              <Button type="button" variant="outline" asChild>
                <Link to="/comunicacao/campanhas">{t('actions.cancel')}</Link>
              </Button>
              <Button type="submit" disabled={loading}>
                <Save className="w-4 h-4 mr-2" />
                {loading ? t('actions.saving') : t('communicationCampaignForm.actions.create')}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
