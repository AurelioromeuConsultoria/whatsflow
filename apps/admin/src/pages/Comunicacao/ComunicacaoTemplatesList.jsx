import React, { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Plus, Mail, MessageSquare, Bell, LayoutTemplate } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { comunicacaoTemplatesApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { toast } from 'sonner';

const getCanalIcon = (canal) => {
  switch (Number(canal)) {
    case 1: return <MessageSquare className="w-4 h-4" />;
    case 2: return <Mail className="w-4 h-4" />;
    case 3: return <Bell className="w-4 h-4" />;
    default: return <LayoutTemplate className="w-4 h-4" />;
  }
};

const getCanalLabel = (canal, t) => {
  switch (Number(canal)) {
    case 1: return t('communicationTemplates.channels.whatsapp');
    case 2: return t('communicationTemplates.channels.email');
    case 3: return t('communicationTemplates.channels.push');
    case 4: return t('communicationTemplates.channels.internalNotification');
    default: return t('communicationTemplates.channels.fallback', { canal });
  }
};

const STATUS_LABEL = {
  1: 'Rascunho',
  2: 'Ativo',
  3: 'Arquivado',
  4: 'Pendente de aprovação',
  5: 'Aprovado',
  6: 'Rejeitado',
};

const STATUS_VARIANT = {
  1: 'secondary',
  2: 'default',
  3: 'outline',
  4: 'secondary',
  5: 'default',
  6: 'destructive',
};

export default function ComunicacaoTemplatesList() {
  const { t } = useTranslation();
  const [templates, setTemplates] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);

  const load = useCallback(async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const response = await comunicacaoTemplatesApi.getAll();
      setTemplates(response.data || []);
    } catch (err) {
      const msg = getApiErrorMessage(err, t('communicationTemplates.errorLoad'));
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [t]);

  useEffect(() => {
    load();
  }, [load]);

  if (loading) return <LoadingPage text={t('communicationTemplates.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-foreground">{t('communicationTemplates.title')}</h1>
          <p className="text-muted-foreground mt-1">{t('communicationTemplates.subtitle')}</p>
        </div>

        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button variant="outline" asChild>
            <Link to="/comunicacao/campanhas">{t('communicationTemplates.actions.campaigns')}</Link>
          </Button>
          <Button asChild>
            <Link to="/comunicacao/templates/novo">
              <Plus className="w-4 h-4 mr-2" />
              {t('communicationTemplates.actions.new')}
            </Link>
          </Button>
        </div>
      </div>

      {templates.length === 0 ? (
        <PageEmptyState
          title={t('communicationTemplates.emptyTitle')}
          description={t('communicationTemplates.emptyDescription')}
          action={(
            <Button asChild>
              <Link to="/comunicacao/templates/novo">{t('communicationTemplates.actions.createFirst')}</Link>
            </Button>
          )}
        />
      ) : (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
          {templates.map((template) => (
            <Card key={template.id}>
              <CardContent className="p-6 flex items-start justify-between gap-4">
                <div className="space-y-2">
                  <div className="flex items-center gap-2 text-muted-foreground">
                    {getCanalIcon(template.canal)}
                    <span className="text-sm">{getCanalLabel(template.canal, t)}</span>
                  </div>
                  <div>
                    <h2 className="text-lg font-semibold text-foreground">{template.nome}</h2>
                    <p className="text-sm text-muted-foreground">{template.objetivo}</p>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <Badge variant={STATUS_VARIANT[Number(template.status)] || 'secondary'}>
                    {STATUS_LABEL[Number(template.status)] || `Status ${template.status}`}
                  </Badge>
                  <Badge variant="secondary">v{template.versao}</Badge>
                  <Button variant="outline" size="sm" asChild>
                    <Link to={`/comunicacao/templates/${template.id}/editar`}>{t('actions.edit')}</Link>
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
