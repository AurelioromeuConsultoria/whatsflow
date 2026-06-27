import { useCallback, useEffect, useState } from 'react';
import { CreditCard, CheckCircle2, AlertTriangle, Clock } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { billingApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';

const CICLO_MENSAL = 1;
const CICLO_ANUAL = 2;

const STATUS_META = {
  Trial: { label: 'Período de teste', variant: 'secondary' },
  Ativa: { label: 'Ativa', variant: 'default' },
  Inadimplente: { label: 'Pagamento pendente', variant: 'destructive' },
  Suspensa: { label: 'Suspensa', variant: 'destructive' },
  Cancelada: { label: 'Cancelada', variant: 'outline' },
};

const FATURA_META = {
  Pendente: 'secondary',
  Paga: 'default',
  Vencida: 'destructive',
  Falhou: 'destructive',
  Cancelada: 'outline',
};

const moeda = (v) => (v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

const FORM_INICIAL = { nomeCliente: '', email: '', cpfCnpj: '', telefone: '', ciclo: String(CICLO_MENSAL) };

export default function MinhaAssinatura() {
  const [assinatura, setAssinatura] = useState(null);
  const [planos, setPlanos] = useState([]);
  const [faturas, setFaturas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [planoSelecionado, setPlanoSelecionado] = useState(null);
  const [form, setForm] = useState(FORM_INICIAL);
  const [saving, setSaving] = useState(false);
  const confirmDialog = useConfirmDialog();

  const carregar = useCallback(async () => {
    setLoading(true);
    try {
      const [planosResp, assinaturaResp] = await Promise.allSettled([
        billingApi.planos(),
        billingApi.minhaAssinatura(),
      ]);

      if (planosResp.status === 'fulfilled') setPlanos(planosResp.value.data ?? []);

      if (assinaturaResp.status === 'fulfilled') {
        setAssinatura(assinaturaResp.value.data);
        const faturasResp = await billingApi.faturas().catch(() => null);
        setFaturas(faturasResp?.data ?? []);
      } else {
        setAssinatura(null); // 404 = ainda sem assinatura
        setFaturas([]);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const abrirAssinar = (plano) => {
    setPlanoSelecionado(plano);
    setForm(FORM_INICIAL);
  };

  const handleAssinar = async () => {
    if (!form.nomeCliente.trim()) {
      toast.error('Informe o nome para a cobrança.');
      return;
    }
    try {
      setSaving(true);
      await billingApi.assinar({
        planoId: planoSelecionado.id,
        ciclo: Number(form.ciclo),
        nomeCliente: form.nomeCliente.trim(),
        email: form.email.trim() || null,
        cpfCnpj: form.cpfCnpj.trim() || null,
        telefone: form.telefone.trim() || null,
      });
      toast.success('Assinatura iniciada! Seu período de teste começou.');
      setPlanoSelecionado(null);
      await carregar();
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao iniciar a assinatura.'));
    } finally {
      setSaving(false);
    }
  };

  const handleCancelar = () => {
    confirmDialog.show({
      title: 'Cancelar assinatura?',
      description: 'Sua assinatura será cancelada. O acesso permanece até o fim do período já pago.',
      confirmText: 'Cancelar assinatura',
      cancelText: 'Voltar',
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await billingApi.cancelar();
          toast.success('Assinatura cancelada.');
          await carregar();
        } catch (err) {
          toast.error(getApiErrorMessage(err, 'Erro ao cancelar.'));
          throw err;
        }
      },
    });
  };

  if (loading) return <LoadingPage />;

  const statusMeta = assinatura ? (STATUS_META[assinatura.status] ?? { label: assinatura.status, variant: 'secondary' }) : null;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <CreditCard className="h-7 w-7 text-primary" />
        <div>
          <h1 className="text-3xl font-bold">Assinatura</h1>
          <p className="text-muted-foreground">Gerencie o plano da sua organização na plataforma.</p>
        </div>
      </div>

      {assinatura ? (
        <>
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>Plano {assinatura.planoNome}</span>
                <Badge variant={statusMeta.variant}>{statusMeta.label}</Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <div className="text-2xl font-semibold">{moeda(assinatura.valor)}<span className="text-sm font-normal text-muted-foreground"> / {assinatura.ciclo === 'Anual' ? 'ano' : 'mês'}</span></div>

              {assinatura.emTrial && (
                <p className="flex items-center gap-2 text-sm"><Clock className="h-4 w-4" /> Período de teste — {assinatura.diasTrialRestantes} dia(s) restante(s).</p>
              )}
              {assinatura.status === 'Ativa' && assinatura.proximaCobranca && (
                <p className="flex items-center gap-2 text-sm text-muted-foreground"><CheckCircle2 className="h-4 w-4" /> Próxima cobrança em {formatDate(assinatura.proximaCobranca)}.</p>
              )}
              {(assinatura.status === 'Inadimplente' || assinatura.status === 'Suspensa') && (
                <p className="flex items-center gap-2 text-sm text-destructive"><AlertTriangle className="h-4 w-4" /> Há uma pendência de pagamento. Regularize para manter o acesso.</p>
              )}

              {assinatura.status !== 'Cancelada' && (
                <div className="pt-2">
                  <Button variant="outline" className="text-destructive hover:text-destructive" onClick={handleCancelar}>
                    Cancelar assinatura
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Faturas</CardTitle></CardHeader>
            <CardContent>
              {faturas.length === 0 ? (
                <p className="text-muted-foreground py-6 text-center">Nenhuma fatura ainda.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Vencimento</TableHead>
                      <TableHead>Valor</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Paga em</TableHead>
                      <TableHead className="text-right">Pagamento</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {faturas.map((f) => (
                      <TableRow key={f.id}>
                        <TableCell>{formatDate(f.vencimento)}</TableCell>
                        <TableCell>{moeda(f.valor)}</TableCell>
                        <TableCell><Badge variant={FATURA_META[f.status] ?? 'secondary'}>{f.status}</Badge></TableCell>
                        <TableCell>{f.pagaEm ? formatDate(f.pagaEm) : '—'}</TableCell>
                        <TableCell className="text-right">
                          {f.linkPagamento ? <a className="text-primary hover:underline" href={f.linkPagamento} target="_blank" rel="noopener">Pagar</a> : '—'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </>
      ) : (
        <div>
          <h2 className="text-lg font-semibold mb-3">Escolha um plano</h2>
          <div className="grid gap-4 md:grid-cols-3">
            {planos.map((p) => (
              <Card key={p.id}>
                <CardHeader><CardTitle>{p.nome}</CardTitle></CardHeader>
                <CardContent className="space-y-3">
                  <div className="text-2xl font-semibold">{moeda(p.precoMensal)}<span className="text-sm font-normal text-muted-foreground"> / mês</span></div>
                  {p.descricao && <p className="text-sm text-muted-foreground">{p.descricao}</p>}
                  <Button className="w-full" onClick={() => abrirAssinar(p)}>Assinar</Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* Dialog de assinatura */}
      <Dialog open={!!planoSelecionado} onOpenChange={(open) => { if (!open) setPlanoSelecionado(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Assinar plano {planoSelecionado?.nome}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">Seu período de teste começa agora — não é necessário cartão para iniciar.</p>
            <div className="space-y-2">
              <Label htmlFor="ciclo">Ciclo</Label>
              <Select value={form.ciclo} onValueChange={(v) => setForm((f) => ({ ...f, ciclo: v }))}>
                <SelectTrigger id="ciclo"><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(CICLO_MENSAL)}>Mensal</SelectItem>
                  <SelectItem value={String(CICLO_ANUAL)}>Anual</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="nomeCliente">Nome para cobrança *</Label>
              <Input id="nomeCliente" value={form.nomeCliente} onChange={(e) => setForm((f) => ({ ...f, nomeCliente: e.target.value }))} placeholder="Razão social ou nome da igreja" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="email">E-mail</Label>
                <Input id="email" type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="cpfCnpj">CPF/CNPJ</Label>
                <Input id="cpfCnpj" value={form.cpfCnpj} onChange={(e) => setForm((f) => ({ ...f, cpfCnpj: e.target.value }))} />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="telefone">Telefone</Label>
              <Input id="telefone" value={form.telefone} onChange={(e) => setForm((f) => ({ ...f, telefone: e.target.value }))} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setPlanoSelecionado(null)} disabled={saving}>Cancelar</Button>
            <Button onClick={handleAssinar} disabled={saving}>{saving ? 'Processando...' : 'Iniciar assinatura'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={(open) => { if (!open) confirmDialog.hide(); }}
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
