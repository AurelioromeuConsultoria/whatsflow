import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Search, Phone } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { TableRowActions, RowIconButtonAction, RowIconLinkAction } from '@/components/ui/list-actions';
import { StatusBadge } from '@/components/ui/status-badge';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { useDebouncedCallback } from '@/hooks/useDebouncedCallback';
import { contatosApi, tagsApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { toast } from 'sonner';

const STATUS = {
  1: { label: 'Ativo', tone: 'success' },
  2: { label: 'Inativo', tone: 'neutral' },
  3: { label: 'Bloqueado', tone: 'danger' },
};

const PAGE_SIZE = 20;

export default function ContatosList() {
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [texto, setTexto] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [optInFilter, setOptInFilter] = useState('all');
  const [tagFilter, setTagFilter] = useState('all');
  const [tags, setTags] = useState([]);
  const confirmDialog = useConfirmDialog();

  const load = useCallback(async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const params = { page, pageSize };
      if (texto.trim()) params.texto = texto.trim();
      if (statusFilter !== 'all') params.status = Number(statusFilter);
      if (optInFilter !== 'all') params.optIn = optInFilter === 'true';
      if (tagFilter !== 'all') params.tagId = Number(tagFilter);
      const res = await contatosApi.getPaged(params);
      setItems(res.data?.items || []);
      setTotal(res.data?.total || 0);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Erro ao carregar contatos.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [page, pageSize, texto, statusFilter, optInFilter, tagFilter]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    tagsApi.getAll().then((r) => setTags(r.data || [])).catch(() => setTags([]));
  }, []);

  const debouncedSearch = useDebouncedCallback(() => {
    setPage(1);
  }, 400);

  const handleTextoChange = (value) => {
    setTexto(value);
    debouncedSearch();
  };

  const handleDelete = (contato) => {
    confirmDialog.show({
      title: 'Remover contato',
      description: `Tem certeza que deseja remover "${contato.nome}"?`,
      confirmText: 'Remover',
      cancelText: 'Cancelar',
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await contatosApi.delete(contato.id);
          toast.success('Contato removido.');
          await load({ silent: true });
        } catch (err) {
          toast.error(getApiErrorMessage(err, 'Erro ao remover contato.'));
          throw err;
        }
      },
    });
  };

  if (loading) return <LoadingPage text="Carregando contatos..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Contatos</h1>
          <p className="text-muted-foreground">Gerencie os contatos do seu workspace WhatsApp.</p>
        </div>
        <Button asChild>
          <Link to="/contatos/novo">
            <Plus className="h-4 w-4 mr-2" /> Novo contato
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filtros</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />Buscar</label>
              <Input
                value={texto}
                onChange={(e) => handleTextoChange(e.target.value)}
                placeholder="Nome, telefone ou e-mail"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <Select value={statusFilter} onValueChange={(v) => { setStatusFilter(v); setPage(1); }}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="1">Ativo</SelectItem>
                  <SelectItem value="2">Inativo</SelectItem>
                  <SelectItem value="3">Bloqueado</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Opt-in</label>
              <Select value={optInFilter} onValueChange={(v) => { setOptInFilter(v); setPage(1); }}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="true">Com opt-in</SelectItem>
                  <SelectItem value="false">Sem opt-in</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Tag</label>
              <Select value={tagFilter} onValueChange={(v) => { setTagFilter(v); setPage(1); }}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todas</SelectItem>
                  {tags.map((tag) => (
                    <SelectItem key={tag.id} value={String(tag.id)}>{tag.nome}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between gap-3">
            <CardTitle>{total} contato(s)</CardTitle>
            <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          </div>
        </CardHeader>
        <CardContent>
          {items.length === 0 ? (
            <PageEmptyState
              title="Nenhum contato encontrado"
              description="Crie seu primeiro contato ou ajuste os filtros."
              action={(
                <Button asChild>
                  <Link to="/contatos/novo"><Plus className="h-4 w-4 mr-2" /> Novo contato</Link>
                </Button>
              )}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>WhatsApp</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Opt-in</TableHead>
                  <TableHead>Tags</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((c) => {
                  const status = STATUS[Number(c.status)] || STATUS[1];
                  return (
                    <TableRow key={c.id}>
                      <TableCell className="font-medium">{c.nome || '-'}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <span>{c.telefoneWhatsApp || '-'}</span>
                          {c.telefoneWhatsApp && (
                            <Button variant="ghost" size="sm" onClick={() => window.open(`https://wa.me/${c.telefoneWhatsApp.replace(/\D/g, '')}`)}>
                              <Phone className="h-4 w-4" />
                            </Button>
                          )}
                        </div>
                      </TableCell>
                      <TableCell><StatusBadge tone={status.tone}>{status.label}</StatusBadge></TableCell>
                      <TableCell>
                        <StatusBadge tone={c.optIn ? 'success' : 'neutral'}>{c.optIn ? 'Sim' : 'Não'}</StatusBadge>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          {(c.tags || []).map((tag) => (
                            <Badge
                              key={tag.id}
                              variant="secondary"
                              style={tag.cor ? { backgroundColor: tag.cor, color: '#fff' } : undefined}
                            >
                              {tag.nome}
                            </Badge>
                          ))}
                          {(!c.tags || c.tags.length === 0) && <span className="text-muted-foreground">-</span>}
                        </div>
                      </TableCell>
                      <TableCell className="text-right">
                        <TableRowActions>
                          <RowIconLinkAction>
                            <Link to={`/contatos/${c.id}/editar`}><Edit className="h-4 w-4" /></Link>
                          </RowIconLinkAction>
                          <RowIconButtonAction onClick={() => handleDelete(c)}>
                            <Trash2 className="h-4 w-4" />
                          </RowIconButtonAction>
                        </TableRowActions>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
          {total > 0 && (
            <DataTablePagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
            />
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
