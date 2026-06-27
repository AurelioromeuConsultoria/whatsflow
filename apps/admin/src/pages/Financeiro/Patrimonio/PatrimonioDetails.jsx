import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Edit, MapPin, User, Wallet, ShieldCheck, FileText, CalendarClock, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { patrimonioApi, patrimonioMovimentacoesApi } from '@/lib/api';
import { formatCurrency, formatDate, formatDateTime } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

const statusKeyMap = {
  EmUso: 'inUse',
  EmManutencao: 'inMaintenance',
  Emprestado: 'loaned',
  Ocioso: 'idle',
  Baixado: 'disposed',
};

const acquisitionTypeKeyMap = {
  Comprado: 'purchased',
  Doado: 'donated',
  Fabricado: 'manufactured',
  Cedido: 'assigned',
};

const conservationKeyMap = {
  Novo: 'new',
  Bom: 'good',
  Regular: 'fair',
  Ruim: 'poor',
  Inutilizavel: 'unusable',
};

export default function PatrimonioDetails() {
  const { id } = useParams();
  const { t } = useTranslation();
  const [item, setItem] = useState(null);
  const [movimentacoes, setMovimentacoes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [filtroTipo, setFiltroTipo] = useState('');
  const [filtroDataInicio, setFiltroDataInicio] = useState('');
  const [filtroDataFim, setFiltroDataFim] = useState('');
  const [movimentacaoForm, setMovimentacaoForm] = useState({
    tipoMovimentacao: 'TransferenciaLocal',
    dataMovimentacao: new Date().toISOString().slice(0, 10),
    origem: '',
    destino: '',
    responsavelOrigem: '',
    responsavelDestino: '',
    observacoes: '',
  });

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const [res, movimentacoesRes] = await Promise.all([
        patrimonioApi.getById(id),
        patrimonioMovimentacoesApi.getByPatrimonioId(id),
      ]);
      setItem(res.data);
      setMovimentacoes(movimentacoesRes.data || []);
    } catch (err) {
      setError(t('finance.patrimony.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  const filteredMovimentacoes = movimentacoes.filter((movimentacao) => {
    if (filtroTipo && movimentacao.tipoMovimentacao !== filtroTipo) return false;

    if (filtroDataInicio) {
      const inicio = new Date(`${filtroDataInicio}T00:00:00`);
      if (new Date(movimentacao.dataMovimentacao) < inicio) return false;
    }

    if (filtroDataFim) {
      const fim = new Date(`${filtroDataFim}T23:59:59`);
      if (new Date(movimentacao.dataMovimentacao) > fim) return false;
    }

    return true;
  });

  const handleMovimentacaoChange = (e) => {
    const { name, value } = e.target;
    setMovimentacaoForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCreateMovimentacao = async (e) => {
    e.preventDefault();
    if (!movimentacaoForm.tipoMovimentacao) {
      toast.error(t('finance.patrimonyMovements.validation.typeRequired'));
      return;
    }

    try {
      setSaving(true);
      await patrimonioMovimentacoesApi.create(id, {
        ...movimentacaoForm,
        dataMovimentacao: movimentacaoForm.dataMovimentacao ? new Date(`${movimentacaoForm.dataMovimentacao}T00:00:00`).toISOString() : null,
      });
      toast.success(t('finance.patrimonyMovements.saveSuccessCreate'));
      setDialogOpen(false);
      setMovimentacaoForm({
        tipoMovimentacao: 'TransferenciaLocal',
        dataMovimentacao: new Date().toISOString().slice(0, 10),
        origem: '',
        destino: '',
        responsavelOrigem: '',
        responsavelDestino: '',
        observacoes: '',
      });
      await load();
    } catch (err) {
      toast.error(err.response?.data?.message || t('finance.patrimonyMovements.saveError'));
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingPage text={t('finance.patrimony.loadingDetails')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;
  if (!item) return <ErrorPage message={t('finance.patrimony.notFound')} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" asChild>
            <Link to="/financeiro/patrimonio">
              <ArrowLeft className="h-4 w-4 mr-2" />
              {t('actions.back')}
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold">{item.nome}</h1>
            <p className="text-muted-foreground">{t('finance.patrimony.detailsSubtitle')}</p>
          </div>
        </div>
        <Button asChild>
          <Link to={`/financeiro/patrimonio/${item.id}/editar`}>
            <Edit className="h-4 w-4 mr-2" />
            {t('actions.edit', 'Editar')}
          </Link>
        </Button>
      </div>

      <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
        <SummaryCard title={t('finance.patrimony.fields.code')} value={item.codigo} />
        <SummaryCard title={t('finance.patrimony.fields.category')} value={item.categoriaNome || '-'} />
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimony.fields.status')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Badge>{t(`finance.patrimony.status.${statusKeyMap[item.status] || 'inUse'}`)}</Badge>
          </CardContent>
        </Card>
        <SummaryCard title={t('finance.patrimony.fields.acquisitionValue')} value={formatCurrency(item.valorAquisicao)} />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimony.sections.identification')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <DetailRow icon={FileText} label={t('finance.patrimony.fields.name')} value={item.nome} />
            <DetailRow icon={FileText} label={t('finance.common.description')} value={item.descricao || '-'} />
            <DetailRow icon={FileText} label={t('finance.patrimony.fields.brand')} value={item.marca || '-'} />
            <DetailRow icon={FileText} label={t('finance.patrimony.fields.model')} value={item.modelo || '-'} />
            <DetailRow icon={FileText} label={t('finance.patrimony.fields.serialNumber')} value={item.numeroSerie || '-'} />
            <DetailRow icon={FileText} label={t('finance.patrimony.fields.quantity')} value={String(item.quantidade || 1)} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimony.sections.location')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <DetailRow icon={MapPin} label={t('finance.patrimony.fields.campus')} value={item.campus || '-'} />
            <DetailRow icon={MapPin} label={t('finance.patrimony.fields.location')} value={item.localizacao || '-'} />
            <DetailRow icon={MapPin} label={t('finance.patrimony.fields.ministry')} value={item.ministerioArea || '-'} />
            <DetailRow icon={User} label={t('finance.patrimony.fields.responsible')} value={item.responsavelNome || '-'} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimony.sections.acquisition')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <DetailRow icon={Wallet} label={t('finance.patrimony.fields.acquisitionType')} value={t(`finance.patrimony.acquisitionType.${acquisitionTypeKeyMap[item.tipoAquisicao] || 'purchased'}`)} />
            <DetailRow icon={CalendarClock} label={t('finance.patrimony.fields.acquisitionDate')} value={formatDate(item.dataAquisicao)} />
            <DetailRow icon={Wallet} label={t('finance.patrimony.fields.acquisitionValue')} value={formatCurrency(item.valorAquisicao)} />
            <DetailRow icon={Wallet} label={t('finance.patrimony.fields.supplier')} value={item.fornecedorNome || '-'} />
            <DetailRow icon={FileText} label={t('finance.patrimony.fields.invoiceNumber')} value={item.numeroNotaFiscal || '-'} />
            <DetailRow icon={Wallet} label={t('finance.patrimony.fields.costCenter')} value={item.centroCustoNome || '-'} />
            <DetailRow icon={Wallet} label={t('finance.patrimony.fields.project')} value={item.projetoNome || '-'} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimony.sections.state')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <DetailRow icon={ShieldCheck} label={t('finance.patrimony.fields.conservationState')} value={t(`finance.patrimony.conservationState.${conservationKeyMap[item.estadoConservacao] || 'good'}`)} />
            <DetailRow icon={CalendarClock} label={t('finance.patrimony.fields.lastEvaluation')} value={formatDate(item.dataUltimaAvaliacao)} />
            <DetailRow icon={ShieldCheck} label={t('finance.patrimony.fields.hasWarranty')} value={item.possuiGarantia ? t('finance.common.yes') : t('finance.common.no')} />
            <DetailRow icon={CalendarClock} label={t('finance.patrimony.fields.warrantyUntil')} value={formatDate(item.garantiaAte)} />
            <DetailRow icon={CalendarClock} label={t('finance.patrimony.fields.lastMaintenance')} value={formatDate(item.dataUltimaManutencao)} />
            <DetailRow icon={CalendarClock} label={t('finance.patrimony.fields.nextMaintenance')} value={formatDate(item.dataProximaManutencao)} />
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.patrimony.sections.notes')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <DetailRow icon={FileText} label={t('finance.patrimony.fields.photoUrl')} value={item.fotoUrl || '-'} />
          <DetailRow icon={FileText} label={t('finance.patrimony.fields.documentUrl')} value={item.documentoUrl || '-'} />
          <DetailRow icon={FileText} label={t('finance.patrimony.fields.notes')} value={item.observacoes || '-'} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <CardTitle>{t('finance.patrimonyMovements.title')}</CardTitle>
          <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
            <DialogTrigger asChild>
              <Button variant="outline">
                <Plus className="h-4 w-4 mr-2" />
                {t('finance.patrimonyMovements.new')}
              </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-2xl">
              <DialogHeader>
                <DialogTitle>{t('finance.patrimonyMovements.newTitle')}</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleCreateMovimentacao} className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="tipoMovimentacao">{t('finance.patrimonyMovements.fields.type')}</Label>
                    <select id="tipoMovimentacao" name="tipoMovimentacao" value={movimentacaoForm.tipoMovimentacao} onChange={handleMovimentacaoChange} className="w-full px-3 py-2 border rounded">
                      <option value="CadastroInicial">{t('finance.patrimonyMovements.types.initialRegistration')}</option>
                      <option value="TransferenciaLocal">{t('finance.patrimonyMovements.types.locationTransfer')}</option>
                      <option value="TrocaResponsavel">{t('finance.patrimonyMovements.types.responsibleChange')}</option>
                      <option value="ManutencaoEnvio">{t('finance.patrimonyMovements.types.maintenanceSend')}</option>
                      <option value="ManutencaoRetorno">{t('finance.patrimonyMovements.types.maintenanceReturn')}</option>
                      <option value="Emprestimo">{t('finance.patrimonyMovements.types.loan')}</option>
                      <option value="Devolucao">{t('finance.patrimonyMovements.types.return')}</option>
                      <option value="Baixa">{t('finance.patrimonyMovements.types.disposal')}</option>
                    </select>
                    <p className="text-xs text-muted-foreground">{t('finance.patrimonyMovements.typeHint')}</p>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="dataMovimentacao">{t('finance.patrimonyMovements.fields.date')}</Label>
                    <Input id="dataMovimentacao" name="dataMovimentacao" type="date" value={movimentacaoForm.dataMovimentacao} onChange={handleMovimentacaoChange} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="origem">{t('finance.patrimonyMovements.fields.origin')}</Label>
                    <Input id="origem" name="origem" value={movimentacaoForm.origem} onChange={handleMovimentacaoChange} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="destino">{t('finance.patrimonyMovements.fields.destination')}</Label>
                    <Input id="destino" name="destino" value={movimentacaoForm.destino} onChange={handleMovimentacaoChange} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="responsavelOrigem">{t('finance.patrimonyMovements.fields.originResponsible')}</Label>
                    <Input id="responsavelOrigem" name="responsavelOrigem" value={movimentacaoForm.responsavelOrigem} onChange={handleMovimentacaoChange} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="responsavelDestino">{t('finance.patrimonyMovements.fields.destinationResponsible')}</Label>
                    <Input id="responsavelDestino" name="responsavelDestino" value={movimentacaoForm.responsavelDestino} onChange={handleMovimentacaoChange} />
                  </div>
                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="observacoes">{t('finance.patrimonyMovements.fields.notes')}</Label>
                    <Textarea id="observacoes" name="observacoes" rows={4} value={movimentacaoForm.observacoes} onChange={handleMovimentacaoChange} />
                  </div>
                </div>
                <div className="flex justify-end space-x-2">
                  <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                    {t('actions.cancel')}
                  </Button>
                  <Button type="submit" disabled={saving}>
                    {saving ? t('actions.saving') : t('actions.create')}
                  </Button>
                </div>
              </form>
            </DialogContent>
          </Dialog>
        </CardHeader>
        <CardContent>
          <div className="mb-4 grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="filtroTipo">{t('finance.patrimonyMovements.fields.type')}</Label>
              <select id="filtroTipo" value={filtroTipo} onChange={(e) => setFiltroTipo(e.target.value)} className="w-full px-3 py-2 border rounded">
                <option value="">{t('finance.patrimonyMovements.filters.allTypes')}</option>
                <option value="CadastroInicial">{t('finance.patrimonyMovements.types.initialRegistration')}</option>
                <option value="TransferenciaLocal">{t('finance.patrimonyMovements.types.locationTransfer')}</option>
                <option value="TrocaResponsavel">{t('finance.patrimonyMovements.types.responsibleChange')}</option>
                <option value="ManutencaoEnvio">{t('finance.patrimonyMovements.types.maintenanceSend')}</option>
                <option value="ManutencaoRetorno">{t('finance.patrimonyMovements.types.maintenanceReturn')}</option>
                <option value="Emprestimo">{t('finance.patrimonyMovements.types.loan')}</option>
                <option value="Devolucao">{t('finance.patrimonyMovements.types.return')}</option>
                <option value="Baixa">{t('finance.patrimonyMovements.types.disposal')}</option>
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="filtroDataInicio">{t('finance.patrimonyMovements.filters.startDate')}</Label>
              <Input id="filtroDataInicio" type="date" value={filtroDataInicio} onChange={(e) => setFiltroDataInicio(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="filtroDataFim">{t('finance.patrimonyMovements.filters.endDate')}</Label>
              <Input id="filtroDataFim" type="date" value={filtroDataFim} onChange={(e) => setFiltroDataFim(e.target.value)} />
            </div>
          </div>

          {filteredMovimentacoes.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('finance.patrimonyMovements.emptyMessage')}</p>
          ) : (
            <div className="space-y-4">
              {filteredMovimentacoes.map((movimentacao) => (
                <div key={movimentacao.id} className="rounded-lg border p-4">
                  <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <p className="font-medium">{t(`finance.patrimonyMovements.types.${movementTypeKey(movimentacao.tipoMovimentacao)}`)}</p>
                      <p className="text-sm text-muted-foreground">{formatDateTime(movimentacao.dataMovimentacao)}</p>
                    </div>
                    <div className="text-sm text-muted-foreground">{movimentacao.usuarioNome || '-'}</div>
                  </div>
                  <div className="mt-3 grid gap-3 md:grid-cols-2">
                    <MovementField label={t('finance.patrimonyMovements.fields.origin')} value={movimentacao.origem || '-'} />
                    <MovementField label={t('finance.patrimonyMovements.fields.destination')} value={movimentacao.destino || '-'} />
                    <MovementField label={t('finance.patrimonyMovements.fields.originResponsible')} value={movimentacao.responsavelOrigem || '-'} />
                    <MovementField label={t('finance.patrimonyMovements.fields.destinationResponsible')} value={movimentacao.responsavelDestino || '-'} />
                  </div>
                  {movimentacao.observacoes && (
                    <div className="mt-3">
                      <p className="text-sm font-medium">{t('finance.patrimonyMovements.fields.notes')}</p>
                      <p className="text-sm text-muted-foreground">{movimentacao.observacoes}</p>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function SummaryCard({ title, value }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent className="text-xl font-semibold">{value}</CardContent>
    </Card>
  );
}

function DetailRow({ icon: Icon, label, value }) {
  return (
    <div className="flex items-start space-x-3">
      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
        <Icon className="h-5 w-5 text-primary" />
      </div>
      <div>
        <p className="text-sm font-medium">{label}</p>
        <p className="text-sm text-muted-foreground break-all">{value}</p>
      </div>
    </div>
  );
}

function MovementField({ label, value }) {
  return (
    <div>
      <p className="text-sm font-medium">{label}</p>
      <p className="text-sm text-muted-foreground">{value}</p>
    </div>
  );
}

function movementTypeKey(tipo) {
  return ({
    CadastroInicial: 'initialRegistration',
    TransferenciaLocal: 'locationTransfer',
    TrocaResponsavel: 'responsibleChange',
    ManutencaoEnvio: 'maintenanceSend',
    ManutencaoRetorno: 'maintenanceReturn',
    Emprestimo: 'loan',
    Devolucao: 'return',
    Baixa: 'disposal',
  }[tipo] || 'initialRegistration');
}
