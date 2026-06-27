import { useEffect, useState } from 'react';
import { Gift, Save, RefreshCcw, Send, Clock3, ImageIcon, RotateCcw, Search, CheckCircle2, AlertTriangle, History, Hourglass } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { ImageUpload } from '@/components/ImageUpload';
import { getAbsoluteUrl } from '@/lib/utils';
import { pessoasApi } from '@/lib/api';
import { formatDate, formatDateTime } from '@/lib/formatters';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

function buildDefaultMessage(t) {
  return t('birthdayCampaignManagement.defaultMessage');
}

function renderPreview(template, nome) {
  return (template || '').replace(/\{Nome\}/gi, nome);
}

function getStatusBadgeVariant(status) {
  if (status === 'Enviado') return 'default';
  if (status === 'Erro') return 'destructive';
  return 'secondary';
}

const initialFilters = {
  busca: '',
  status: 'all',
  limit: '50',
};

export default function CampanhaAniversario() {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [sendingTest, setSendingTest] = useState(false);
  const [resendingId, setResendingId] = useState(null);
  const [confirmResend, setConfirmResend] = useState(null);
  const [formData, setFormData] = useState({
    ativo: true,
    imagemUrl: '',
    mensagemTemplate: buildDefaultMessage(t),
    horarioEnvio: '09:00',
  });
  const [metricas, setMetricas] = useState({
    totalHistorico: 0,
    totalEnviadosAnoAtual: 0,
    totalFalhasAnoAtual: 0,
    totalPendentesAnoAtual: 0,
    totalEnviadosHoje: 0,
    totalFalhasHoje: 0,
  });
  const [recentes, setRecentes] = useState([]);
  const [filters, setFilters] = useState(initialFilters);
  const [testData, setTestData] = useState({
    nome: t('birthdayCampaignManagement.test.defaultName'),
    whatsApp: '',
  });

  const loadConfiguracao = async (filtersToUse = filters) => {
    try {
      setLoading(true);
      const response = await pessoasApi.getCampanhaAniversario({
        busca: filtersToUse.busca || undefined,
        status: filtersToUse.status === 'all' ? undefined : filtersToUse.status,
        limit: Number(filtersToUse.limit || 50),
      });
      const data = response.data;
      setFormData({
        ativo: data.ativo ?? true,
        imagemUrl: data.imagemUrl || '',
        mensagemTemplate: data.mensagemTemplate || buildDefaultMessage(t),
        horarioEnvio: data.horarioEnvio || '09:00',
      });
      setMetricas(data.metricas || {});
      setRecentes(data.enviosRecentes || []);
      setFilters({
        busca: data.filtros?.busca || filtersToUse.busca || '',
        status: data.filtros?.status || filtersToUse.status || 'all',
        limit: String(data.filtros?.limit || filtersToUse.limit || '50'),
      });
    } catch (error) {
      console.error(error);
      toast.error(t('birthdayCampaignManagement.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadConfiguracao();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!formData.imagemUrl) {
      toast.error(t('birthdayCampaignManagement.validation.imageRequired'));
      return;
    }

    if (!formData.mensagemTemplate.trim()) {
      toast.error(t('birthdayCampaignManagement.validation.messageRequired'));
      return;
    }

    try {
      setSaving(true);
      const response = await pessoasApi.updateCampanhaAniversario(formData);
      const data = response.data;
      setMetricas(data.metricas || {});
      setRecentes(data.enviosRecentes || []);
      toast.success(t('birthdayCampaignManagement.saveSuccess'));
    } catch (error) {
      console.error(error);
      toast.error(error.response?.data?.message || t('birthdayCampaignManagement.errorSave'));
    } finally {
      setSaving(false);
    }
  };

  const handleSendTest = async (event) => {
    event?.preventDefault?.();
    event?.stopPropagation?.();

    if (!testData.whatsApp.trim()) {
      toast.error(t('birthdayCampaignManagement.test.whatsAppRequired'));
      return;
    }

    try {
      setSendingTest(true);
      const response = await pessoasApi.sendCampanhaAniversarioTeste(testData);
      toast.success(response.data?.mensagem || t('birthdayCampaignManagement.test.success'));
    } catch (error) {
      console.error(error);
      const mensagem = error.response?.data?.mensagem || t('birthdayCampaignManagement.test.error');
      const detalhes = error.response?.data?.detalhes;
      toast.error(detalhes ? `${mensagem} - ${detalhes}` : mensagem);
    } finally {
      setSendingTest(false);
    }
  };

  const handleApplyFilters = async (event) => {
    event.preventDefault();
    await loadConfiguracao(filters);
  };

  const handleResend = async (item) => {
    try {
      setResendingId(item.id);
      const response = await pessoasApi.resendCampanhaAniversarioHistorico(item.id);
      toast.success(response.data?.mensagem || t('birthdayCampaignManagement.history.resendSuccess'));
      await loadConfiguracao(filters);
    } catch (error) {
      console.error(error);
      const mensagem = error.response?.data?.mensagem || t('birthdayCampaignManagement.history.resendError');
      const detalhes = error.response?.data?.detalhes;
      toast.error(detalhes ? `${mensagem} - ${detalhes}` : mensagem);
    } finally {
      setResendingId(null);
      setConfirmResend(null);
    }
  };

  if (loading) {
    return <LoadingPage text={t('birthdayCampaignManagement.loading')} />;
  }

  const previewNome = testData.nome?.trim() || t('birthdayCampaignManagement.preview.fallbackName');
  const previewMessage = renderPreview(formData.mensagemTemplate, previewNome);
  const previewImage = formData.imagemUrl ? getAbsoluteUrl(formData.imagemUrl) : null;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Gift className="h-8 w-8" />
            {t('birthdayCampaignManagement.title')}
          </h1>
          <p className="text-muted-foreground mt-2">
            {t('birthdayCampaignManagement.subtitle')}
          </p>
        </div>
        <Button type="button" variant="outline" onClick={loadConfiguracao}>
          <RefreshCcw className="h-4 w-4 mr-2" />
          {t('actions.refresh')}
        </Button>
      </div>

      <Alert>
        <Gift className="h-4 w-4" />
        <AlertTitle>{t('birthdayCampaignManagement.howItWorks.title')}</AlertTitle>
        <AlertDescription>
          {t('birthdayCampaignManagement.howItWorks.description')}
        </AlertDescription>
      </Alert>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <Card>
          <CardHeader>
            <CardTitle>{t('birthdayCampaignManagement.configuration.title')}</CardTitle>
            <CardDescription>
              {t('birthdayCampaignManagement.configuration.description')} <code>{'{Nome}'}</code> {t('birthdayCampaignManagement.configuration.placeholderHint')}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="flex items-center justify-between rounded-lg border p-4">
                <div className="space-y-1">
                  <Label className="text-base">{t('birthdayCampaignManagement.configuration.activeLabel')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('birthdayCampaignManagement.configuration.activeHint')}
                  </p>
                </div>
                <Switch
                  checked={formData.ativo}
                  onCheckedChange={(checked) =>
                    setFormData((current) => ({ ...current, ativo: checked }))
                  }
                />
              </div>

              <div className="space-y-2">
                <Label className="flex items-center gap-2">
                  <ImageIcon className="h-4 w-4" />
                  {t('birthdayCampaignManagement.configuration.imageLabel')}
                </Label>
                <ImageUpload
                  label={t('birthdayCampaignManagement.configuration.imageLabel')}
                  value={formData.imagemUrl}
                  onChange={(value) => setFormData((current) => ({ ...current, imagemUrl: value }))}
                  accept="image/*"
                  type="image"
                />
                <p className="text-xs text-muted-foreground">
                  {t('birthdayCampaignManagement.configuration.imageHint')}
                </p>
              </div>

              <div className="grid gap-4 md:grid-cols-[220px_1fr]">
                <div className="space-y-2">
                  <Label htmlFor="horarioEnvio" className="flex items-center gap-2">
                    <Clock3 className="h-4 w-4" />
                    {t('birthdayCampaignManagement.configuration.scheduleTime')}
                  </Label>
                  <Input
                    id="horarioEnvio"
                    type="time"
                    value={formData.horarioEnvio}
                    onChange={(event) =>
                      setFormData((current) => ({ ...current, horarioEnvio: event.target.value }))
                    }
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="mensagemTemplate">{t('birthdayCampaignManagement.configuration.messageLabel')}</Label>
                  <Textarea
                    id="mensagemTemplate"
                    value={formData.mensagemTemplate}
                    onChange={(event) =>
                      setFormData((current) => ({ ...current, mensagemTemplate: event.target.value }))
                    }
                    rows={14}
                  />
                </div>
              </div>

              <div className="flex justify-end">
                <Button type="submit" disabled={saving}>
                  <Save className="h-4 w-4 mr-2" />
                  {saving ? t('actions.saving') : t('birthdayCampaignManagement.configuration.save')}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>{t('birthdayCampaignManagement.preview.title')}</CardTitle>
              <CardDescription>
                {t('birthdayCampaignManagement.preview.description')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {previewImage ? (
                <img
                  src={previewImage}
                  alt={t('birthdayCampaignManagement.preview.imageAlt')}
                  className="w-full rounded-xl border object-cover"
                />
              ) : (
                <div className="flex h-56 items-center justify-center rounded-xl border border-dashed text-sm text-muted-foreground">
                  {t('birthdayCampaignManagement.preview.emptyImage')}
                </div>
              )}

              <div className="rounded-xl border bg-muted/20 p-4 whitespace-pre-wrap text-sm leading-6">
                {previewMessage}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t('birthdayCampaignManagement.test.title')}</CardTitle>
              <CardDescription>
                {t('birthdayCampaignManagement.test.description')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <form onSubmit={handleSendTest} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="nomeTeste">{t('birthdayCampaignManagement.test.nameLabel')}</Label>
                  <Input
                    id="nomeTeste"
                    value={testData.nome}
                    onChange={(event) =>
                      setTestData((current) => ({ ...current, nome: event.target.value }))
                    }
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="whatsAppTeste">{t('birthdayCampaignManagement.test.whatsAppLabel')}</Label>
                  <Input
                    id="whatsAppTeste"
                    value={testData.whatsApp}
                    onChange={(event) =>
                      setTestData((current) => ({ ...current, whatsApp: event.target.value }))
                    }
                    placeholder={t('birthdayCampaignManagement.test.whatsAppPlaceholder')}
                  />
                </div>
                <Button type="submit" disabled={sendingTest} className="w-full">
                  <Send className="h-4 w-4 mr-2" />
                  {sendingTest ? t('birthdayCampaignManagement.test.sending') : t('birthdayCampaignManagement.test.send')}
                </Button>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
              <History className="h-4 w-4" />
              {t('birthdayCampaignManagement.metrics.totalHistory')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">{metricas.totalHistorico || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
              <CheckCircle2 className="h-4 w-4" />
              {t('birthdayCampaignManagement.metrics.sentYear')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">{metricas.totalEnviadosAnoAtual || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
              <AlertTriangle className="h-4 w-4" />
              {t('birthdayCampaignManagement.metrics.failuresYear')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">{metricas.totalFalhasAnoAtual || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
              <Hourglass className="h-4 w-4" />
              {t('birthdayCampaignManagement.metrics.pendingYear')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">{metricas.totalPendentesAnoAtual || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
              <Gift className="h-4 w-4" />
              {t('birthdayCampaignManagement.metrics.today')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-1">
            <div className="text-lg font-semibold">{t('birthdayCampaignManagement.metrics.todaySent', { count: metricas.totalEnviadosHoje || 0 })}</div>
            <div className="text-sm text-muted-foreground">{t('birthdayCampaignManagement.metrics.todayFailures', { count: metricas.totalFalhasHoje || 0 })}</div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('birthdayCampaignManagement.history.title')}</CardTitle>
          <CardDescription>
            {t('birthdayCampaignManagement.history.description')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleApplyFilters} className="grid gap-4 mb-6 lg:grid-cols-[1fr_220px_180px_auto]">
            <div className="space-y-2">
              <Label htmlFor="buscaHistorico" className="flex items-center gap-2">
                <Search className="h-4 w-4" />
                {t('birthdayCampaignManagement.history.searchLabel')}
              </Label>
              <Input
                id="buscaHistorico"
                value={filters.busca}
                onChange={(event) => setFilters((current) => ({ ...current, busca: event.target.value }))}
                placeholder={t('birthdayCampaignManagement.history.searchPlaceholder')}
              />
            </div>
            <div className="space-y-2">
              <Label>{t('birthdayCampaignManagement.history.statusLabel')}</Label>
              <Select
                value={filters.status}
                onValueChange={(value) => setFilters((current) => ({ ...current, status: value }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t('birthdayCampaignManagement.history.allStatuses')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('birthdayCampaignManagement.history.allStatuses')}</SelectItem>
                  <SelectItem value="Enviado">{t('birthdayCampaignManagement.history.status.sent')}</SelectItem>
                  <SelectItem value="Erro">{t('birthdayCampaignManagement.history.status.error')}</SelectItem>
                  <SelectItem value="Pendente">{t('birthdayCampaignManagement.history.status.pending')}</SelectItem>
                  <SelectItem value="EmProcessamento">{t('birthdayCampaignManagement.history.status.processing')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t('birthdayCampaignManagement.history.quantityLabel')}</Label>
              <Select
                value={filters.limit}
                onValueChange={(value) => setFilters((current) => ({ ...current, limit: value }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="20">{t('birthdayCampaignManagement.history.recordsCount', { count: 20 })}</SelectItem>
                  <SelectItem value="50">{t('birthdayCampaignManagement.history.recordsCount', { count: 50 })}</SelectItem>
                  <SelectItem value="100">{t('birthdayCampaignManagement.history.recordsCount', { count: 100 })}</SelectItem>
                  <SelectItem value="200">{t('birthdayCampaignManagement.history.recordsCount', { count: 200 })}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-end gap-2">
              <Button type="submit" variant="outline" className="w-full lg:w-auto">
                <Search className="h-4 w-4 mr-2" />
                {t('birthdayCampaignManagement.history.filter')}
              </Button>
              <Button
                type="button"
                variant="ghost"
                className="w-full lg:w-auto"
                onClick={() => {
                  const resetFilters = { ...initialFilters };
                  setFilters(resetFilters);
                  loadConfiguracao(resetFilters);
                }}
              >
                {t('birthdayCampaignManagement.history.clear')}
              </Button>
            </div>
          </form>

          {recentes.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              {t('birthdayCampaignManagement.history.empty')}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('birthdayCampaignManagement.history.table.person')}</TableHead>
                  <TableHead>{t('birthdayCampaignManagement.history.table.whatsApp')}</TableHead>
                  <TableHead>{t('birthdayCampaignManagement.history.table.birthday')}</TableHead>
                  <TableHead>{t('birthdayCampaignManagement.history.table.status')}</TableHead>
                  <TableHead>{t('birthdayCampaignManagement.history.table.attempts')}</TableHead>
                  <TableHead>{t('birthdayCampaignManagement.history.table.lastAttempt')}</TableHead>
                  <TableHead>{t('birthdayCampaignManagement.history.table.sentAt')}</TableHead>
                  <TableHead className="w-[120px] text-right">{t('birthdayCampaignManagement.history.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recentes.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>
                      <div className="font-medium">{item.nomePessoa}</div>
                      {item.logErro ? (
                        <div className="text-xs text-muted-foreground mt-1 line-clamp-2">{item.logErro}</div>
                      ) : null}
                    </TableCell>
                    <TableCell>{item.whatsApp || t('common.notInformed')}</TableCell>
                    <TableCell>{formatDate(item.dataAniversario)}</TableCell>
                    <TableCell>
                      <Badge variant={getStatusBadgeVariant(item.status)}>
                        {t(`birthdayCampaignManagement.history.status.${item.status === 'EmProcessamento' ? 'processing' : item.status.toLowerCase()}`, { defaultValue: item.status })}
                      </Badge>
                    </TableCell>
                    <TableCell>{item.tentativas}</TableCell>
                    <TableCell>{formatDateTime(item.dataUltimaTentativa)}</TableCell>
                    <TableCell>{formatDateTime(item.dataEnvioSucesso)}</TableCell>
                    <TableCell className="text-right">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setConfirmResend(item)}
                        disabled={resendingId === item.id}
                      >
                        <RotateCcw className="h-4 w-4 mr-2" />
                        {resendingId === item.id ? t('birthdayCampaignManagement.history.resending') : t('birthdayCampaignManagement.history.resend')}
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
        open={Boolean(confirmResend)}
        onOpenChange={(open) => {
          if (!open) setConfirmResend(null);
        }}
        title={t('birthdayCampaignManagement.history.confirmResendTitle')}
        description={
          confirmResend
            ? t('birthdayCampaignManagement.history.confirmResendDescription', { name: confirmResend.nomePessoa })
            : ''
        }
        confirmText={resendingId ? t('birthdayCampaignManagement.history.resending') : t('birthdayCampaignManagement.history.confirmResend')}
        cancelText={t('actions.cancel')}
        loading={Boolean(resendingId)}
        onConfirm={() => confirmResend && handleResend(confirmResend)}
      />
    </div>
  );
}
