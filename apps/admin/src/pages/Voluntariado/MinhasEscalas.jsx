import { useEffect, useState } from 'react';
import { CalendarDays, CheckCircle2, Clock3, XCircle } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { PromptDialog } from '@/components/ui/prompt-dialog';
import { usePromptDialog } from '@/hooks/usePromptDialog';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { escalasApi, solicitacoesTrocasEscalasApi } from '@/lib/api';
import { toast } from 'sonner';
import { formatDateTime } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';

function getEscalaItemStatusLabel(status, t) {
  const value = Number(status);
  if (value === 1) return t('volunteer.mySchedules.status.pending');
  if (value === 2) return t('volunteer.mySchedules.status.confirmed');
  if (value === 3) return t('volunteer.mySchedules.status.declined');
  if (value === 4) return t('volunteer.mySchedules.status.replaced');
  if (value === 5) return t('volunteer.mySchedules.status.served');
  if (value === 6) return t('volunteer.mySchedules.status.missed');
  return t('volunteer.mySchedules.status.unknown');
}

function getActionButtonProps(item, action, t) {
  const status = Number(item.status);

  if (action === 'confirmar') {
    return status === 2
      ? {
          label: t('volunteer.mySchedules.confirmedButton'),
          className: '!border-emerald-600 !bg-emerald-600 !text-white hover:!bg-emerald-700 hover:!text-white',
        }
      : {
          label: t('volunteer.mySchedules.confirmAction'),
          className: '',
        };
  }

  if (action === 'recusar') {
    return status === 3
      ? {
          label: t('volunteer.mySchedules.declinedButton'),
          className: '!border-rose-600 !bg-rose-600 !text-white hover:!bg-rose-700 hover:!text-white',
        }
      : {
          label: t('volunteer.mySchedules.declineAction'),
          className: '',
        };
  }

  return {
    label: '',
    className: '',
  };
}

export default function MinhasEscalas() {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [somenteFuturas, setSomenteFuturas] = useState('true');
  const [escalas, setEscalas] = useState([]);
  const [solicitacoes, setSolicitacoes] = useState([]);
  const [trocaModalOpen, setTrocaModalOpen] = useState(false);
  const [trocaItem, setTrocaItem] = useState(null);
  const [trocaMotivo, setTrocaMotivo] = useState('');
  const promptDialog = usePromptDialog();

  const load = async (futureOnly = somenteFuturas) => {
    try {
      setLoading(true);
      setError(null);
      const [escalasRes, solicitacoesRes] = await Promise.all([
        escalasApi.getMinhas({ somenteFuturas: futureOnly === 'true' }),
        solicitacoesTrocasEscalasApi.getMinhas(),
      ]);
      setEscalas(escalasRes.data || []);
      setSolicitacoes(solicitacoesRes.data || []);
    } catch (err) {
      console.error(err);
      setError(t('volunteer.mySchedules.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [somenteFuturas]);

  const handleConfirmar = async (escalaId, itemId) => {
    try {
      await escalasApi.confirmarItem(escalaId, itemId);
      toast.success(t('volunteer.mySchedules.confirmSuccess'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.mySchedules.errorConfirm'));
      toast.error(message);
    }
  };

  const handleRecusar = async (escalaId, item) => {
    const motivoRecusa = await promptDialog.prompt({
      title: t('volunteer.mySchedules.declineTitle'),
      label: t('volunteer.mySchedules.declinePrompt', { team: item.equipeNome }),
      description: t('volunteer.mySchedules.declineOptionalHint'),
      defaultValue: item.motivoRecusa || '',
      confirmText: t('confirmDialog.confirm'),
      cancelText: t('actions.cancel'),
    });
    if (motivoRecusa === null) return;

    try {
      await escalasApi.recusarItem(escalaId, item.id, { motivoRecusa });
      toast.success(t('volunteer.mySchedules.declineSuccess'));
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.mySchedules.errorDecline'));
      toast.error(message);
    }
  };

  const openSolicitarTroca = (escalaId, item) => {
    setTrocaItem({ escalaId, item });
    setTrocaMotivo('');
    setTrocaModalOpen(true);
  };

  const handleSolicitarTroca = async () => {
    if (!trocaItem) return;
    try {
      await solicitacoesTrocasEscalasApi.create(trocaItem.escalaId, trocaItem.item.id, { motivo: trocaMotivo });
      toast.success(t('volunteer.mySchedules.swapRequestSuccess'));
      setTrocaModalOpen(false);
      setTrocaItem(null);
      setTrocaMotivo('');
      await load();
    } catch (err) {
      console.error(err);
      const message = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || t('volunteer.mySchedules.errorSwapRequest'));
      toast.error(message);
    }
  };

  if (loading) return <LoadingPage text={t('volunteer.mySchedules.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={() => load()} />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('volunteer.mySchedules.title')}</h1>
          <p className="text-muted-foreground">{t('volunteer.mySchedules.subtitle')}</p>
        </div>

        <div className="w-[220px]">
          <Select value={somenteFuturas} onValueChange={setSomenteFuturas}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="true">{t('volunteer.mySchedules.futureOnly')}</SelectItem>
              <SelectItem value="false">{t('volunteer.mySchedules.allSchedules')}</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {escalas.length === 0 ? (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            {t('volunteer.mySchedules.empty')}
          </CardContent>
        </Card>
      ) : (
        escalas.map((escala) => (
          <Card key={escala.id}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <CalendarDays className="h-5 w-5" />
                {escala.eventoTitulo} - {escala.equipeNome}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="text-sm text-muted-foreground">
                {formatDateTime(escala.eventoDataHoraInicio)}
              </div>

              {escala.itens.map((item) => (
                <div key={item.id} className="flex items-center justify-between rounded-lg border p-4 gap-4">
                  {(() => {
                    const confirmarButton = getActionButtonProps(item, 'confirmar', t);
                    const recusarButton = getActionButtonProps(item, 'recusar', t);

                    return (
                      <>
                  <div className="space-y-1">
                    <div className="font-medium">{item.cargoNome || t('volunteer.mySchedules.undefinedRole')}</div>
                    <div className="text-sm text-muted-foreground">
                      {t('volunteer.mySchedules.labels.status')}: {getEscalaItemStatusLabel(item.status, t)}
                    </div>
                    {item.motivoRecusa && (
                      <div className="text-sm text-red-600">
                        {t('volunteer.mySchedules.labels.declineReason')}: {item.motivoRecusa}
                      </div>
                    )}
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      className={confirmarButton.className}
                      onClick={() => handleConfirmar(escala.id, item.id)}
                    >
                      <CheckCircle2 className="h-4 w-4 mr-2" />
                      {confirmarButton.label}
                    </Button>
                    <Button
                      variant="outline"
                      className={recusarButton.className}
                      onClick={() => handleRecusar(escala.id, item)}
                    >
                      <XCircle className="h-4 w-4 mr-2" />
                      {recusarButton.label}
                    </Button>
                    <Button variant="outline" onClick={() => openSolicitarTroca(escala.id, item)}>
                      {t('volunteer.mySchedules.swapRequestAction')}
                    </Button>
                    {Number(item.status) === 1 && (
                      <span className="inline-flex items-center text-sm text-amber-600">
                        <Clock3 className="h-4 w-4 mr-1" />
                        {t('volunteer.mySchedules.status.pending')}
                      </span>
                    )}
                  </div>
                      </>
                    );
                  })()}
                </div>
              ))}
            </CardContent>
          </Card>
        ))
      )}

      <Card>
        <CardHeader>
          <CardTitle>{t('volunteer.mySchedules.swapRequestsTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          {solicitacoes.length === 0 ? (
            <div className="text-sm text-muted-foreground">{t('volunteer.mySchedules.noSwapRequests')}</div>
          ) : (
            <div className="space-y-3">
              {solicitacoes.map((solicitacao) => (
                <div key={solicitacao.id} className="rounded-lg border p-4">
                  <div className="font-medium">{solicitacao.equipeNome}</div>
                  <div className="text-sm text-muted-foreground">
                    {t('volunteer.mySchedules.labels.status')}: {solicitacao.status === 1
                      ? t('volunteer.mySchedules.requestStatus.pending')
                      : solicitacao.status === 2
                        ? t('volunteer.mySchedules.requestStatus.approved')
                        : t('volunteer.mySchedules.requestStatus.rejected')}
                  </div>
                  <div className="text-sm">{t('volunteer.mySchedules.labels.reason')}: {solicitacao.motivo || '-'}</div>
                  {solicitacao.voluntarioSubstitutoNome && (
                    <div className="text-sm">{t('volunteer.mySchedules.labels.substitute')}: {solicitacao.voluntarioSubstitutoNome}</div>
                  )}
                  {solicitacao.observacaoResposta && (
                    <div className="text-sm">{t('volunteer.mySchedules.labels.response')}: {solicitacao.observacaoResposta}</div>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={trocaModalOpen} onOpenChange={setTrocaModalOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('volunteer.mySchedules.swapDialog.title')}</DialogTitle>
            <DialogDescription>{t('volunteer.mySchedules.swapDialog.description')}</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Textarea
              value={trocaMotivo}
              onChange={(e) => setTrocaMotivo(e.target.value)}
              placeholder={t('volunteer.mySchedules.swapDialog.placeholder')}
              rows={4}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setTrocaModalOpen(false)}>{t('volunteer.mySchedules.cancelAction')}</Button>
            <Button onClick={handleSolicitarTroca}>{t('volunteer.mySchedules.swapDialog.submit')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <PromptDialog
        open={promptDialog.open}
        onOpenChange={promptDialog.onOpenChange}
        value={promptDialog.value}
        onValueChange={promptDialog.setValue}
        onConfirm={promptDialog.handleConfirm}
        config={promptDialog.config}
      />
    </div>
  );
}
