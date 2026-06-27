import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Users, CheckCircle, Clock, XCircle, UserCheck, Eye, Edit, Trash2, Phone, Mail, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState } from '@/components/ui/page-state';
import { RowIconButtonAction, RowIconLinkAction, TableRowActions } from '@/components/ui/list-actions';
import { StatusBadge } from '@/components/ui/status-badge';
import { inscricoesEventosApi, eventosApi } from '@/lib/api';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import InscricaoEventoPublicForm from '@/components/InscricaoEvento/InscricaoEventoPublicForm';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDate } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';

const STATUS_LABELS = (t) => ({
  1: t('eventRegistrations.status.pending'),
  2: t('eventRegistrations.status.confirmed'),
  3: t('eventRegistrations.status.canceled'),
  4: t('eventRegistrations.status.present'),
});

const STATUS_TONES = {
  1: 'warning',
  2: 'success',
  3: 'danger',
  4: 'info',
};

export default function EventoInscricoes() {
  const { t } = useTranslation();
  const { eventoId } = useParams();
  const [inscricoes, setInscricoes] = useState([]);
  const [evento, setEvento] = useState(null);
  const [estatisticas, setEstatisticas] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('');
  const [showForm, setShowForm] = useState(false);
  const confirmDialog = useConfirmDialog();

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const [inscricoesRes, eventoRes, statsRes] = await Promise.all([
        inscricoesEventosApi.getByEvento(eventoId),
        eventosApi.getById(eventoId),
        inscricoesEventosApi.getEstatisticas(eventoId),
      ]);
      setInscricoes(inscricoesRes.data || []);
      setEvento(eventoRes.data);
      setEstatisticas(statsRes.data);
    } catch (err) {
      setError(t('eventRegistrations.errorLoadEvent'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [eventoId]);

  const handleDelete = async (id) => {
    confirmDialog.show({
      title: t('eventRegistrations.confirmDialog.deleteTitle'),
      description: t('eventRegistrations.confirmDialog.deleteDescription'),
      confirmText: t('eventRegistrations.confirmDialog.confirmDelete'),
      cancelText: t('common.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await inscricoesEventosApi.delete(id);
          toast.success(t('eventRegistrations.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('eventRegistrations.errorDelete')));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const handleConfirmar = async (id) => {
    try {
      await inscricoesEventosApi.confirmar(id);
      toast.success(t('eventRegistrations.confirmSuccess'));
      await load();
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('eventRegistrations.errorConfirm')));
      console.error(err);
    }
  };

  const handleCancelar = async (id) => {
    confirmDialog.show({
      title: t('eventRegistrations.confirmDialog.cancelTitle'),
      description: t('eventRegistrations.confirmDialog.cancelDescription'),
      confirmText: t('eventRegistrations.confirmDialog.confirmCancel'),
      cancelText: t('common.back'),
      variant: 'default',
      onConfirm: async () => {
        try {
          await inscricoesEventosApi.cancelar(id);
          toast.success(t('eventRegistrations.cancelSuccess'));
          await load();
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('eventRegistrations.errorCancel')));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const handleMarcaPresente = async (id) => {
    try {
      await inscricoesEventosApi.update(id, { status: 4 });
      toast.success(t('eventRegistrations.markPresentSuccess'));
      await load();
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('eventRegistrations.errorMarkPresent')));
      console.error(err);
    }
  };

  const filtered = inscricoes.filter((i) => {
    if (statusFilter && String(i.status) !== statusFilter) return false;
    return true;
  });

  const podeInscrever = evento && new Date(evento.dataInicio) > new Date();

  if (loading) return <LoadingPage text={t('eventRegistrations.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" asChild>
            <Link to="/eventos">
              <ArrowLeft className="h-4 w-4 mr-2" /> {t('eventRegistrations.backToEvents')}
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold">
              {t('eventRegistrations.titleWithEvent', {
                event: evento?.titulo || t('eventRegistrations.eventFallback'),
              })}
            </h1>
            <p className="text-muted-foreground">{t('eventRegistrations.manageEventSubtitle')}</p>
          </div>
        </div>
        {podeInscrever && (
          <Button onClick={() => setShowForm(true)}>
            <Plus className="h-4 w-4 mr-2" /> {t('eventRegistrations.new')}
          </Button>
        )}
      </div>

      {estatisticas && (
        <div className="grid gap-4 md:grid-cols-5">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center space-x-2">
                <Users className="h-4 w-4 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('eventRegistrations.stats.total')}</p>
                  <p className="text-2xl font-bold">{estatisticas.totalInscricoes}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center space-x-2">
                <CheckCircle className="h-4 w-4 text-green-600" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('eventRegistrations.stats.confirmed')}</p>
                  <p className="text-2xl font-bold">{estatisticas.inscricoesConfirmadas}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center space-x-2">
                <Clock className="h-4 w-4 text-yellow-600" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('eventRegistrations.stats.pending')}</p>
                  <p className="text-2xl font-bold">{estatisticas.inscricoesPendentes}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center space-x-2">
                <XCircle className="h-4 w-4 text-red-600" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('eventRegistrations.stats.canceled')}</p>
                  <p className="text-2xl font-bold">{estatisticas.inscricoesCanceladas}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center space-x-2">
                <Users className="h-4 w-4 text-blue-600" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('eventRegistrations.stats.participants')}</p>
                  <p className="text-2xl font-bold">{estatisticas.totalParticipantes}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>{t('eventRegistrations.listTitle')} ({filtered.length})</CardTitle>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('eventRegistrations.filterByStatusLabel')}</label>
              <select className="px-3 py-2 border rounded" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">{t('eventRegistrations.statusAllOption')}</option>
                <option value="1">{t('eventRegistrations.status.pending')}</option>
                <option value="2">{t('eventRegistrations.status.confirmed')}</option>
                <option value="3">{t('eventRegistrations.status.canceled')}</option>
                <option value="4">{t('eventRegistrations.status.present')}</option>
              </select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {filtered.length === 0 ? (
            <PageEmptyState
              title={t('eventRegistrations.emptyMessage')}
              description={t('eventRegistrations.eventEmptyDescription')}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('eventRegistrations.table.name')}</TableHead>
                  <TableHead>{t('eventRegistrations.table.whatsapp')}</TableHead>
                  <TableHead>{t('eventRegistrations.table.email')}</TableHead>
                  <TableHead>{t('eventRegistrations.table.status')}</TableHead>
                  <TableHead>{t('eventRegistrations.table.companions')}</TableHead>
                  <TableHead>{t('eventRegistrations.table.registrationDate')}</TableHead>
                  <TableHead className="text-right">{t('eventRegistrations.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filtered.map((inscricao) => (
                  <TableRow key={inscricao.id}>
                    <TableCell className="font-medium">{inscricao.nome}</TableCell>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        <span>{inscricao.whatsApp}</span>
                        {inscricao.whatsApp && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => window.open(`https://wa.me/55${inscricao.whatsApp.replace(/\D/g, '')}`)}
                          >
                            <Phone className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        <span>{inscricao.email || '-'}</span>
                        {inscricao.email && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => window.open(`mailto:${inscricao.email}`)}
                          >
                            <Mail className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <StatusBadge tone={STATUS_TONES[inscricao.status] || 'neutral'}>
                        {STATUS_LABELS(t)[inscricao.status] || inscricao.statusDescricao}
                      </StatusBadge>
                    </TableCell>
                    <TableCell>{inscricao.quantidadeAcompanhantes || 0}</TableCell>
                    <TableCell>{formatDate(inscricao.dataInscricao)}</TableCell>
                    <TableCell className="text-right">
                      <TableRowActions className="space-x-1">
                        <RowIconLinkAction>
                          <Link to={`/inscricoes-eventos/${inscricao.id}`}>
                            <Eye className="h-4 w-4" />
                          </Link>
                        </RowIconLinkAction>
                        {inscricao.status === 1 && (
                          <RowIconButtonAction onClick={() => handleConfirmar(inscricao.id)} title={t('eventRegistrations.confirmAction')}>
                            <CheckCircle className="h-4 w-4 text-green-600" />
                          </RowIconButtonAction>
                        )}
                        {(inscricao.status === 1 || inscricao.status === 2) && (
                          <RowIconButtonAction onClick={() => handleCancelar(inscricao.id)} title={t('eventRegistrations.cancelAction')}>
                            <XCircle className="h-4 w-4 text-red-600" />
                          </RowIconButtonAction>
                        )}
                        {inscricao.status === 2 && (
                          <RowIconButtonAction onClick={() => handleMarcaPresente(inscricao.id)} title={t('eventRegistrations.markPresentAction')}>
                            <UserCheck className="h-4 w-4 text-blue-600" />
                          </RowIconButtonAction>
                        )}
                        <RowIconLinkAction>
                          <Link to={`/inscricoes-eventos/${inscricao.id}/editar`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </RowIconLinkAction>
                        <RowIconButtonAction onClick={() => handleDelete(inscricao.id)}>
                          <Trash2 className="h-4 w-4" />
                        </RowIconButtonAction>
                      </TableRowActions>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t('eventRegistrations.newInlineTitle')}</DialogTitle>
          </DialogHeader>
          <InscricaoEventoPublicForm
            eventoId={eventoId}
            onSuccess={() => {
              setShowForm(false);
              load();
            }}
            onCancel={() => setShowForm(false)}
          />
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={(open) => {
          if (!open) confirmDialog.hide();
        }}
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



