import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Bell, CheckCheck, Clock3 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { notificacoesApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';

function getTipoLabel(tipo, t) {
  const value = Number(tipo);
  if (value === 2) return t('notifications.types.schedule');
  if (value === 3) return t('notifications.types.swap');
  return t('notifications.types.general');
}

export default function NotificacoesList() {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [notificacoes, setNotificacoes] = useState([]);

  const load = useCallback(async (options = {}) => {
    const silent = options.silent ?? false;
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const res = await notificacoesApi.getMinhas();
      setNotificacoes(res.data || []);
    } catch (err) {
      console.error(err);
      setError(t('notifications.errorLoad'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [t]);

  useEffect(() => {
    load();
  }, [load]);

  const marcarComoLida = async (id) => {
    try {
      await notificacoesApi.marcarComoLida(id);
      setNotificacoes((current) =>
        current.map((item) => (item.id === id ? { ...item, dataLeitura: new Date().toISOString() } : item))
      );
    } catch (err) {
      console.error(err);
      toast.error(t('notifications.markReadError'));
    }
  };

  const marcarTodas = async () => {
    try {
      await notificacoesApi.marcarTodasComoLidas();
      setNotificacoes((current) => current.map((item) => ({ ...item, dataLeitura: item.dataLeitura || new Date().toISOString() })));
      toast.success(t('notifications.markAllSuccess'));
    } catch (err) {
      console.error(err);
      toast.error(t('notifications.markAllError'));
    }
  };

  if (loading) return <LoadingPage text={t('notifications.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('notifications.title')}</h1>
          <p className="text-muted-foreground">
            {t('notifications.subtitle')}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button variant="outline" onClick={marcarTodas}>
            <CheckCheck className="h-4 w-4 mr-2" />
            {t('notifications.readAll')}
          </Button>
        </div>
      </div>

      {notificacoes.length === 0 ? (
        <Card>
          <CardContent>
            <PageEmptyState
              title={t('notifications.emptyTitle')}
              description={t('notifications.emptyDescription')}
            />
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {notificacoes.map((item) => (
            <Card key={item.id} className={item.dataLeitura ? 'opacity-80' : 'border-primary/30'}>
              <CardHeader>
                <CardTitle className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-2">
                    <Bell className="h-5 w-5" />
                    <span>{item.titulo}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant={item.dataLeitura ? 'secondary' : 'default'}>
                      {item.dataLeitura ? t('notifications.status.read') : t('notifications.status.new')}
                    </Badge>
                    <Badge variant="outline">{getTipoLabel(item.tipo, t)}</Badge>
                  </div>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <p className="text-sm">{item.mensagem}</p>
                <div className="flex items-center justify-between gap-3 text-sm text-muted-foreground">
                  <div className="flex items-center gap-2">
                    <Clock3 className="h-4 w-4" />
                    {formatDateTime(item.dataCriacao)}
                  </div>
                  {!item.dataLeitura && (
                    <Button variant="outline" size="sm" onClick={() => marcarComoLida(item.id)}>
                      {t('notifications.markAsRead')}
                    </Button>
                  )}
                </div>
                {item.link && (
                  <div>
                    <Button size="sm" asChild>
                      <Link to={item.link}>{t('notifications.open')}</Link>
                    </Button>
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
