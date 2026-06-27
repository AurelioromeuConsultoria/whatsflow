import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { escalasModelosApi, equipesApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function ModelosEscalaList() {
  const { t } = useTranslation();
  const [equipes, setEquipes] = useState([]);
  const [equipeId, setEquipeId] = useState('');
  const [modelos, setModelos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadingModelos, setLoadingModelos] = useState(false);
  const [refreshingModelos, setRefreshingModelos] = useState(false);
  const [error, setError] = useState(null);
  const confirmDialog = useConfirmDialog();

  const loadEquipes = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await equipesApi.getAll();
      setEquipes(res.data || []);
      if (res.data?.length && !equipeId) setEquipeId(String(res.data[0].id));
    } catch (err) {
      setError(t('volunteer.schedules.models.errorLoadTeams'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadModelos = async ({ silent = false } = {}) => {
    if (!equipeId) {
      setModelos([]);
      return;
    }
    try {
      if (silent) {
        setRefreshingModelos(true);
      } else {
        setLoadingModelos(true);
      }
      const res = await escalasModelosApi.getByEquipe(equipeId);
      setModelos(res.data || []);
    } catch (err) {
      console.error(err);
      toast.error(t('volunteer.schedules.models.errorLoadModels'));
      setModelos([]);
    } finally {
      if (silent) {
        setRefreshingModelos(false);
      } else {
        setLoadingModelos(false);
      }
    }
  };

  useEffect(() => {
    loadEquipes();
  }, []);

  useEffect(() => {
    loadModelos();
  }, [equipeId]);

  const handleDelete = async (modelo) => {
    confirmDialog.show({
      title: t('volunteer.schedules.models.deleteTitle'),
      description: t('volunteer.schedules.models.deleteDescription', {
        name: modelo.nome || modelo.eventoNome || t('volunteer.schedules.models.noName'),
        team: modelo.equipeNome,
      }),
      confirmText: t('volunteer.schedules.models.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await escalasModelosApi.delete(modelo.id);
          toast.success(t('volunteer.schedules.models.deleteSuccess'));
          await loadModelos();
        } catch (err) {
          const msg = err.response?.data?.message || err.response?.data || t('volunteer.schedules.models.deleteError');
          toast.error(typeof msg === 'string' ? msg : t('volunteer.schedules.models.deleteError'));
          throw err;
        }
      },
    });
  };

  if (loading) return <LoadingPage text={t('volunteer.schedules.models.loadingTeams')} />;
  if (error) return <ErrorPage message={error} onRetry={loadEquipes} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.schedules.models.title')}</h1>
          <p className="text-muted-foreground">
            {t('volunteer.schedules.models.subtitle')}
          </p>
        </div>
        <Button asChild>
          <Link to={equipeId ? `/voluntariado/modelos-escala/novo?equipeId=${equipeId}` : '/voluntariado/modelos-escala/novo'}>
            <Plus className="h-4 w-4 mr-2" /> {t('volunteer.schedules.models.new')}
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.schedules.models.filterByTeam')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Select value={equipeId || 'all'} onValueChange={(v) => setEquipeId(v === 'all' ? '' : v)}>
            <SelectTrigger className="max-w-xs">
              <SelectValue placeholder={t('volunteer.schedules.models.selectTeam')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t('volunteer.schedules.models.allTeams')}</SelectItem>
              {equipes.map((e) => (
                <SelectItem key={e.id} value={String(e.id)}>{e.nome}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{t('volunteer.schedules.models.listTitle', { team: equipeId ? `— ${equipes.find((e) => String(e.id) === equipeId)?.nome || ''}` : '' })}</CardTitle>
            {equipeId ? (
              <PageRefreshButton onClick={() => loadModelos({ silent: true })} refreshing={refreshingModelos} />
            ) : null}
          </div>
        </CardHeader>
        <CardContent>
          {!equipeId ? (
            <PageEmptyState
              title={t('volunteer.schedules.models.emptySelectTitle')}
              description={t('volunteer.schedules.models.emptySelectDescription')}
            />
          ) : loadingModelos ? (
            <LoadingPage text={t('volunteer.schedules.models.loadingModels')} />
          ) : !modelos.length ? (
            <PageEmptyState
              title={t('volunteer.schedules.models.emptyTitle')}
              description={t('volunteer.schedules.models.emptyDescription')}
              action={(
                <Button asChild>
                  <Link to={equipeId ? `/voluntariado/modelos-escala/novo?equipeId=${equipeId}` : '/voluntariado/modelos-escala/novo'}>
                    <Plus className="h-4 w-4 mr-2" /> {t('volunteer.schedules.models.new')}
                  </Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('volunteer.schedules.models.table.event')}</TableHead>
                  <TableHead>{t('volunteer.schedules.models.table.name')}</TableHead>
                  <TableHead>{t('volunteer.schedules.models.table.team')}</TableHead>
                  <TableHead>{t('volunteer.schedules.models.table.daysOff')}</TableHead>
                  <TableHead>{t('volunteer.schedules.models.table.items')}</TableHead>
                  <TableHead className="text-right">{t('volunteer.schedules.models.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {modelos.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell>{m.eventoNome ?? t('volunteer.schedules.models.defaultAnyEvent')}</TableCell>
                    <TableCell>{m.nome || '-'}</TableCell>
                    <TableCell>{m.equipeNome}</TableCell>
                    <TableCell>{m.diasFolgaAposEscala ?? '-'}</TableCell>
                    <TableCell>
                      {m.itens?.length
                        ? m.itens.map((i) => `${i.cargoNome || t('volunteer.schedules.models.anyRole')} × ${i.quantidade}`).join(', ')
                        : '-'}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="ghost" size="sm" asChild>
                        <Link to={`/voluntariado/modelos-escala/${m.id}`}>
                          <Edit className="h-4 w-4" />
                        </Link>
                      </Button>
                      <Button variant="ghost" size="sm" onClick={() => handleDelete(m)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

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
