import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ShieldCheck, Plus, ExternalLink, Check, Clock } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { solicitacoesTitularApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';

// Valores correspondem ao enum TipoSolicitacaoTitular (a API binda enums como inteiro).
const TIPO_OPTIONS = [
  { value: '1', label: 'Acesso aos dados' },
  { value: '2', label: 'Exportação / portabilidade' },
  { value: '3', label: 'Correção' },
  { value: '4', label: 'Eliminação' },
  { value: '5', label: 'Revogação de consentimento' },
  { value: '99', label: 'Outro' },
];

const TIPO_LABEL = {
  Acesso: 'Acesso aos dados',
  Exportacao: 'Exportação / portabilidade',
  Correcao: 'Correção',
  Eliminacao: 'Eliminação',
  RevogacaoConsentimento: 'Revogação de consentimento',
  Outro: 'Outro',
};

const STATUS_META = {
  Aberta: { label: 'Aberta', variant: 'secondary' },
  EmAtendimento: { label: 'Em atendimento', variant: 'default' },
  Concluida: { label: 'Concluída', variant: 'outline' },
  Recusada: { label: 'Recusada', variant: 'destructive' },
};

const FORM_INICIAL = {
  tipo: '1',
  pessoaId: '',
  nomeSolicitante: '',
  contatoSolicitante: '',
  canal: 'email',
  descricao: '',
};

export default function SolicitacoesTitular() {
  const [solicitacoes, setSolicitacoes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [statusFiltro, setStatusFiltro] = useState('todos');
  const [criarOpen, setCriarOpen] = useState(false);
  const [form, setForm] = useState(FORM_INICIAL);
  const [saving, setSaving] = useState(false);
  const [recusarAlvo, setRecusarAlvo] = useState(null);
  const [motivoRecusa, setMotivoRecusa] = useState('');
  const confirmDialog = useConfirmDialog();

  const carregar = useCallback(async () => {
    try {
      setLoading(true);
      const response = await solicitacoesTitularApi.listar(statusFiltro === 'todos' ? undefined : statusFiltro);
      setSolicitacoes(response.data ?? []);
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao carregar solicitações.'));
    } finally {
      setLoading(false);
    }
  }, [statusFiltro]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const handleCriar = async () => {
    try {
      setSaving(true);
      await solicitacoesTitularApi.criar({
        tipo: Number(form.tipo),
        pessoaId: form.pessoaId ? Number(form.pessoaId) : null,
        nomeSolicitante: form.nomeSolicitante.trim() || null,
        contatoSolicitante: form.contatoSolicitante.trim() || null,
        canal: form.canal.trim() || null,
        descricao: form.descricao.trim() || null,
      });
      toast.success('Solicitação registrada.');
      setCriarOpen(false);
      setForm(FORM_INICIAL);
      await carregar();
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao registrar a solicitação.'));
    } finally {
      setSaving(false);
    }
  };

  const handleAtender = async (id) => {
    try {
      await solicitacoesTitularApi.atender(id);
      toast.success('Solicitação marcada como em atendimento.');
      await carregar();
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao atualizar a solicitação.'));
    }
  };

  const handleConcluir = (solicitacao) => {
    confirmDialog.show({
      title: 'Concluir solicitação?',
      description: 'Confirme que o pedido do titular foi atendido (ex.: dados exportados, anonimização realizada). A conclusão fica registrada com data e responsável.',
      confirmText: 'Concluir',
      cancelText: 'Cancelar',
      variant: 'default',
      onConfirm: async () => {
        try {
          await solicitacoesTitularApi.concluir(solicitacao.id, null);
          toast.success('Solicitação concluída.');
          await carregar();
        } catch (err) {
          toast.error(getApiErrorMessage(err, 'Erro ao concluir a solicitação.'));
          throw err;
        }
      },
    });
  };

  const handleRecusar = async () => {
    if (!motivoRecusa.trim()) {
      toast.error('Informe o motivo da recusa.');
      return;
    }
    try {
      setSaving(true);
      await solicitacoesTitularApi.recusar(recusarAlvo.id, motivoRecusa.trim());
      toast.success('Solicitação recusada.');
      setRecusarAlvo(null);
      setMotivoRecusa('');
      await carregar();
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao recusar a solicitação.'));
    } finally {
      setSaving(false);
    }
  };

  const renderPrazo = (s) => {
    if (s.status === 'Concluida' || s.status === 'Recusada') {
      return <span className="text-muted-foreground">{formatDate(s.prazoLimite)}</span>;
    }
    if (s.prazoVencido) {
      return <Badge variant="destructive"><Clock className="h-3 w-3" /> Vencido</Badge>;
    }
    if (s.diasRestantes <= 3) {
      return <Badge className="bg-amber-500 text-white"><Clock className="h-3 w-3" /> {s.diasRestantes}d</Badge>;
    }
    return <span>{formatDate(s.prazoLimite)} ({s.diasRestantes}d)</span>;
  };

  if (loading) return <LoadingPage />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ShieldCheck className="h-7 w-7 text-primary" />
          <div>
            <h1 className="text-3xl font-bold">Solicitações de titulares (LGPD)</h1>
            <p className="text-muted-foreground">Requisições de acesso, exportação, correção e eliminação — com prazo legal de 15 dias.</p>
          </div>
        </div>
        <Button onClick={() => setCriarOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Nova solicitação
        </Button>
      </div>

      <div className="flex items-center gap-2">
        <Label htmlFor="statusFiltro">Status</Label>
        <Select value={statusFiltro} onValueChange={setStatusFiltro}>
          <SelectTrigger id="statusFiltro" className="w-56">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="todos">Todas</SelectItem>
            <SelectItem value="Aberta">Abertas</SelectItem>
            <SelectItem value="EmAtendimento">Em atendimento</SelectItem>
            <SelectItem value="Concluida">Concluídas</SelectItem>
            <SelectItem value="Recusada">Recusadas</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Requisições</CardTitle>
        </CardHeader>
        <CardContent>
          {solicitacoes.length === 0 ? (
            <p className="text-muted-foreground py-8 text-center">Nenhuma solicitação encontrada.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Titular</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Canal</TableHead>
                  <TableHead>Solicitado em</TableHead>
                  <TableHead>Prazo</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {solicitacoes.map((s) => {
                  const statusMeta = STATUS_META[s.status] ?? { label: s.status, variant: 'secondary' };
                  const emAberto = s.status === 'Aberta' || s.status === 'EmAtendimento';
                  return (
                    <TableRow key={s.id}>
                      <TableCell>
                        {s.pessoaId ? (
                          <Link to={`/pessoas/${s.pessoaId}`} className="text-primary inline-flex items-center gap-1 hover:underline">
                            {s.nomePessoa || `Pessoa #${s.pessoaId}`} <ExternalLink className="h-3 w-3" />
                          </Link>
                        ) : (
                          <div>
                            <div>{s.nomeSolicitante || '—'}</div>
                            {s.contatoSolicitante && <div className="text-xs text-muted-foreground">{s.contatoSolicitante}</div>}
                          </div>
                        )}
                      </TableCell>
                      <TableCell>{TIPO_LABEL[s.tipo] ?? s.tipo}</TableCell>
                      <TableCell>{s.canal || '—'}</TableCell>
                      <TableCell>{formatDate(s.solicitadoEm)}</TableCell>
                      <TableCell>{renderPrazo(s)}</TableCell>
                      <TableCell><Badge variant={statusMeta.variant}>{statusMeta.label}</Badge></TableCell>
                      <TableCell className="text-right space-x-2 whitespace-nowrap">
                        {s.status === 'Aberta' && (
                          <Button variant="outline" size="sm" onClick={() => handleAtender(s.id)}>Atender</Button>
                        )}
                        {emAberto && (
                          <>
                            <Button variant="outline" size="sm" onClick={() => handleConcluir(s)}>
                              <Check className="h-3 w-3 mr-1" /> Concluir
                            </Button>
                            <Button variant="outline" size="sm" className="text-destructive hover:text-destructive" onClick={() => { setRecusarAlvo(s); setMotivoRecusa(''); }}>
                              Recusar
                            </Button>
                          </>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Nova solicitação */}
      <Dialog open={criarOpen} onOpenChange={setCriarOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Nova solicitação de titular</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="tipo">Tipo *</Label>
              <Select value={form.tipo} onValueChange={(v) => setForm((p) => ({ ...p, tipo: v }))}>
                <SelectTrigger id="tipo"><SelectValue /></SelectTrigger>
                <SelectContent>
                  {TIPO_OPTIONS.map((o) => <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="pessoaId">ID do titular cadastrado (opcional)</Label>
              <Input id="pessoaId" type="number" value={form.pessoaId} onChange={(e) => setForm((p) => ({ ...p, pessoaId: e.target.value }))} placeholder="Ex.: 42" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="nomeSolicitante">Nome do solicitante</Label>
                <Input id="nomeSolicitante" value={form.nomeSolicitante} onChange={(e) => setForm((p) => ({ ...p, nomeSolicitante: e.target.value }))} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="contatoSolicitante">Contato</Label>
                <Input id="contatoSolicitante" value={form.contatoSolicitante} onChange={(e) => setForm((p) => ({ ...p, contatoSolicitante: e.target.value }))} placeholder="e-mail ou telefone" />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="canal">Canal</Label>
              <Input id="canal" value={form.canal} onChange={(e) => setForm((p) => ({ ...p, canal: e.target.value }))} placeholder="email, whatsapp, presencial..." />
            </div>
            <div className="space-y-2">
              <Label htmlFor="descricao">Descrição</Label>
              <Textarea id="descricao" rows={3} value={form.descricao} onChange={(e) => setForm((p) => ({ ...p, descricao: e.target.value }))} maxLength={2000} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCriarOpen(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={handleCriar} disabled={saving}>{saving ? 'Salvando...' : 'Registrar'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Recusar */}
      <Dialog open={!!recusarAlvo} onOpenChange={(open) => { if (!open) setRecusarAlvo(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Recusar solicitação</DialogTitle>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="motivo">Motivo da recusa *</Label>
            <Textarea id="motivo" rows={3} value={motivoRecusa} onChange={(e) => setMotivoRecusa(e.target.value)} maxLength={2000} placeholder="Ex.: retenção exigida por obrigação legal." />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRecusarAlvo(null)} disabled={saving}>Cancelar</Button>
            <Button variant="destructive" onClick={handleRecusar} disabled={saving}>{saving ? 'Salvando...' : 'Recusar'}</Button>
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
