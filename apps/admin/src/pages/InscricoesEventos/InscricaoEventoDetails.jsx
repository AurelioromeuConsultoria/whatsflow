import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Edit, Trash2, CheckCircle, XCircle, Phone, Mail, Calendar, Users } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { inscricoesEventosApi } from '@/lib/api';
import { formatDateTime } from '@/lib/formatters';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';
import { useTranslation } from 'react-i18next';

const STATUS_LABELS = (t) => ({
  1: t('eventRegistrations.status.pending'),
  2: t('eventRegistrations.status.confirmed'),
  3: t('eventRegistrations.status.canceled'),
  4: t('eventRegistrations.status.present'),
});

const STATUS_COLORS = {
  1: 'bg-yellow-100 text-yellow-800',
  2: 'bg-green-100 text-green-800',
  3: 'bg-red-100 text-red-800',
  4: 'bg-blue-100 text-blue-800',
};

export default function InscricaoEventoDetails() {
  const { t } = useTranslation();
  const { id } = useParams();
  const navigate = useNavigate();
  const [inscricao, setInscricao] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const confirmDialog = useConfirmDialog();

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await inscricoesEventosApi.getById(id);
      setInscricao(res.data);
    } catch (err) {
      setError(t('eventRegistrations.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  const handleDelete = async () => {
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
          navigate('/inscricoes-eventos');
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('eventRegistrations.errorDelete')));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const handleConfirmar = async () => {
    try {
      await inscricoesEventosApi.confirmar(id);
      toast.success(t('eventRegistrations.confirmSuccess'));
      await load();
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('eventRegistrations.errorConfirm')));
      console.error(err);
    }
  };

  const handleCancelar = async () => {
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

  if (loading) return <LoadingPage text={t('eventRegistrations.loadingDetails')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;
  if (!inscricao) return <div>{t('eventRegistrations.notFound')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" asChild>
            <Link to="/inscricoes-eventos">
              <ArrowLeft className="h-4 w-4 mr-2" /> {t('eventRegistrations.backToRegistrations')}
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold">{t('eventRegistrations.detailsTitle')}</h1>
            <p className="text-muted-foreground">{t('eventRegistrations.detailsSubtitle')}</p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          {inscricao.status === 1 && (
            <Button onClick={handleConfirmar} variant="outline">
              <CheckCircle className="h-4 w-4 mr-2" /> {t('eventRegistrations.confirmAction')}
            </Button>
          )}
          {(inscricao.status === 1 || inscricao.status === 2) && (
            <Button onClick={handleCancelar} variant="outline">
              <XCircle className="h-4 w-4 mr-2" /> {t('eventRegistrations.cancelAction')}
            </Button>
          )}
          <Button variant="outline" asChild>
            <Link to={`/inscricoes-eventos/${id}/editar`}>
              <Edit className="h-4 w-4 mr-2" /> {t('eventRegistrations.edit')}
            </Link>
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            <Trash2 className="h-4 w-4 mr-2" /> {t('eventRegistrations.confirmDialog.confirmDelete')}
          </Button>
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('eventRegistrations.participantCardTitle')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.participantNameLabel')}</label>
              <p className="text-base font-medium">{inscricao.nome}</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.participantWhatsappLabel')}</label>
              <div className="flex items-center space-x-2">
                <p className="text-base">{inscricao.whatsApp}</p>
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
            </div>
            {inscricao.email && (
              <div>
                <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.participantEmailLabel')}</label>
                <div className="flex items-center space-x-2">
                  <p className="text-base">{inscricao.email}</p>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => window.open(`mailto:${inscricao.email}`)}
                  >
                    <Mail className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            )}
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.participantStatusLabel')}</label>
              <div>
                <span className={`px-3 py-1 rounded text-sm font-medium ${STATUS_COLORS[inscricao.status] || 'bg-gray-100 text-gray-800'}`}>
                  {STATUS_LABELS(t)[inscricao.status] || inscricao.statusDescricao}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('eventRegistrations.eventCardTitle')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.eventTitleLabel')}</label>
              <p className="text-base font-medium">{inscricao.eventoTitulo || '-'}</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.eventCompanionsLabel')}</label>
              <div className="flex items-center space-x-2">
                <Users className="h-4 w-4" />
                <p className="text-base">{inscricao.quantidadeAcompanhantes || 0}</p>
              </div>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.eventRegistrationDateLabel')}</label>
              <div className="flex items-center space-x-2">
                <Calendar className="h-4 w-4" />
                <p className="text-base">{inscricao.dataInscricao ? formatDateTime(inscricao.dataInscricao) : '-'}</p>
              </div>
            </div>
            {inscricao.dataConfirmacao && (
              <div>
                <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.eventConfirmationDateLabel')}</label>
                <p className="text-base">{formatDateTime(inscricao.dataConfirmacao)}</p>
              </div>
            )}
            {inscricao.dataCancelamento && (
              <div>
                <label className="text-sm font-medium text-muted-foreground">{t('eventRegistrations.eventCancellationDateLabel')}</label>
                <p className="text-base">{formatDateTime(inscricao.dataCancelamento)}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {inscricao.observacoes && (
        <Card>
          <CardHeader>
            <CardTitle>{t('eventRegistrations.participantNotesTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-base whitespace-pre-wrap">{inscricao.observacoes}</p>
          </CardContent>
        </Card>
      )}

      {inscricao.observacoesInternas && (
        <Card>
          <CardHeader>
            <CardTitle>{t('eventRegistrations.internalNotesTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-base whitespace-pre-wrap">{inscricao.observacoesInternas}</p>
          </CardContent>
        </Card>
      )}

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





