import { useEffect, useState } from 'react';
import { CalendarOff, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { indisponibilidadesVoluntariosApi, voluntariosApi } from '@/lib/api';
import { toast } from 'sonner';
import { formatDate } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';

export default function IndisponibilidadesList() {
  const { t } = useTranslation();
  const [voluntarios, setVoluntarios] = useState([]);
  const [voluntarioId, setVoluntarioId] = useState('');
  const [itens, setItens] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadingItens, setLoadingItens] = useState(false);
  const [refreshingItens, setRefreshingItens] = useState(false);
  const [error, setError] = useState(null);
  const [novaData, setNovaData] = useState('');
  const [novoMotivo, setNovoMotivo] = useState('');
  const [saving, setSaving] = useState(false);
  const confirmDialog = useConfirmDialog();

  const loadVoluntarios = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await voluntariosApi.getAll();
      setVoluntarios(res.data || []);
      if (res.data?.length && !voluntarioId) setVoluntarioId(String(res.data[0].id));
    } catch (err) {
      setError(t('volunteer.schedules.unavailabilities.errorLoadVolunteers'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadItens = async ({ silent = false } = {}) => {
    if (!voluntarioId) {
      setItens([]);
      return;
    }
    try {
      if (silent) {
        setRefreshingItens(true);
      } else {
        setLoadingItens(true);
      }
      const res = await indisponibilidadesVoluntariosApi.getByVoluntario(voluntarioId);
      setItens(res.data || []);
    } catch (err) {
      console.error(err);
      toast.error(t('volunteer.schedules.unavailabilities.errorLoadItems'));
      setItens([]);
    } finally {
      if (silent) {
        setRefreshingItens(false);
      } else {
        setLoadingItens(false);
      }
    }
  };

  useEffect(() => {
    loadVoluntarios();
  }, []);

  useEffect(() => {
    loadItens();
  }, [voluntarioId]);

  const handleAdd = async (e) => {
    e.preventDefault();
    if (!voluntarioId || !novaData) {
      toast.error(t('volunteer.schedules.unavailabilities.selectVolunteerAndDate'));
      return;
    }
    try {
      setSaving(true);
      await indisponibilidadesVoluntariosApi.create({
        voluntarioId: Number(voluntarioId),
        data: novaData,
        motivo: novoMotivo.trim() || null,
      });
      toast.success(t('volunteer.schedules.unavailabilities.createSuccess'));
      setNovaData('');
      setNovoMotivo('');
      await loadItens();
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data || t('volunteer.schedules.unavailabilities.errorSave');
      toast.error(typeof msg === 'string' ? msg : t('volunteer.schedules.unavailabilities.errorSave'));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (item) => {
    confirmDialog.show({
      title: t('volunteer.schedules.unavailabilities.deleteTitle'),
      description: t('volunteer.schedules.unavailabilities.deleteDescription', {
        date: formatDate(item.data),
        reason: item.motivo ? ` (${item.motivo})` : '',
      }),
      confirmText: t('volunteer.schedules.unavailabilities.deleteConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await indisponibilidadesVoluntariosApi.delete(item.id);
          toast.success(t('volunteer.schedules.unavailabilities.deleteSuccess'));
          await loadItens();
        } catch (err) {
          toast.error(t('volunteer.schedules.unavailabilities.deleteError'));
          throw err;
        }
      },
    });
  };

  if (loading) return <LoadingPage text={t('volunteer.schedules.unavailabilities.loadingVolunteers')} />;
  if (error) return <ErrorPage message={error} onRetry={loadVoluntarios} />;

  const voluntario = voluntarios.find((v) => String(v.id) === voluntarioId);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('volunteer.schedules.unavailabilities.title')}</h1>
        <p className="text-muted-foreground">
          {t('volunteer.schedules.unavailabilities.subtitle')}
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.schedules.unavailabilities.volunteerTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Select value={voluntarioId || 'all'} onValueChange={setVoluntarioId}>
            <SelectTrigger className="max-w-md">
              <SelectValue placeholder={t('volunteer.schedules.unavailabilities.selectVolunteer')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t('volunteer.schedules.unavailabilities.select')}</SelectItem>
              {voluntarios.map((v) => (
                <SelectItem key={v.id} value={String(v.id)}>
                  {v.nome} {v.nomeEquipe ? `— ${v.nomeEquipe}` : ''}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      {voluntarioId && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>{t('volunteer.schedules.unavailabilities.addTitle')}</CardTitle>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleAdd} className="flex flex-wrap items-end gap-4">
                <div className="space-y-2">
                  <Label>{t('volunteer.schedules.unavailabilities.fields.date')} *</Label>
                  <Input
                    type="date"
                    value={novaData}
                    onChange={(e) => setNovaData(e.target.value)}
                    required
                  />
                </div>
                <div className="space-y-2 flex-1 min-w-[200px]">
                  <Label>{t('volunteer.schedules.unavailabilities.fields.reasonOptional')}</Label>
                  <Input
                    value={novoMotivo}
                    onChange={(e) => setNovoMotivo(e.target.value)}
                    placeholder={t('volunteer.schedules.unavailabilities.fields.reasonPlaceholder')}
                  />
                </div>
                <Button type="submit" disabled={saving}>
                  <Plus className="h-4 w-4 mr-2" />
                  {saving ? t('actions.saving') : t('volunteer.schedules.unavailabilities.addAction')}
                </Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <div className="flex items-center justify-between gap-3">
                <CardTitle>{t('volunteer.schedules.unavailabilities.listTitle', { name: voluntario ? `— ${voluntario.nome}` : '' })}</CardTitle>
                {voluntarioId ? (
                  <PageRefreshButton onClick={() => loadItens({ silent: true })} refreshing={refreshingItens} />
                ) : null}
              </div>
            </CardHeader>
            <CardContent>
              {loadingItens ? (
                <LoadingPage text={t('common.loading')} />
              ) : !itens.length ? (
                <PageEmptyState
                  title={t('volunteer.schedules.unavailabilities.emptyTitle')}
                  description={t('volunteer.schedules.unavailabilities.emptyDescription')}
                />
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t('volunteer.schedules.unavailabilities.table.date')}</TableHead>
                      <TableHead>{t('volunteer.schedules.unavailabilities.table.reason')}</TableHead>
                      <TableHead className="text-right">{t('volunteer.schedules.unavailabilities.table.actions')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {itens.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>{formatDate(item.data)}</TableCell>
                        <TableCell>{item.motivo || '-'}</TableCell>
                        <TableCell className="text-right">
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(item)}>
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
        </>
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
