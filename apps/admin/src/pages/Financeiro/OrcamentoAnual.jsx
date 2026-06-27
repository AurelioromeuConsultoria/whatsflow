import { useEffect, useState } from 'react';
import { Save, Trash2, TrendingUp, TrendingDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { orcamentoCategoriasApi, categoriasReceitasApi, categoriasDespesasApi } from '@/lib/api';
import { toast } from 'sonner';
import { formatCurrency } from '@/lib/formatters';

const anoAtual = new Date().getFullYear();
const ANOS = Array.from({ length: 5 }, (_, i) => anoAtual - 1 + i);

function BarraProgresso({ percent }) {
  const cor = percent > 100 ? 'bg-red-500' : percent > 80 ? 'bg-amber-500' : 'bg-green-500';
  return (
    <div className="w-full bg-muted rounded-full h-2 overflow-hidden">
      <div className={`h-2 rounded-full transition-all ${cor}`} style={{ width: `${Math.min(percent, 100)}%` }} />
    </div>
  );
}

function ItemComparacao({ item }) {
  const pct = item.valorOrcado > 0 ? (item.valorRealizado / item.valorOrcado) * 100 : null;
  const semOrcamento = item.valorOrcado === 0;
  return (
    <div className={`rounded border p-3 space-y-2 ${semOrcamento ? 'border-dashed opacity-70' : ''}`}>
      <div className="flex items-center justify-between text-sm">
        <span className="font-medium">{item.categoriaNome}</span>
        {semOrcamento ? (
          <Badge variant="outline" className="text-xs">Sem orçamento</Badge>
        ) : (
          <span className={`text-xs font-semibold ${pct > 100 ? 'text-red-600' : pct > 80 ? 'text-amber-600' : 'text-green-600'}`}>
            {pct?.toFixed(0)}%
          </span>
        )}
      </div>
      <div className="flex justify-between text-xs text-muted-foreground">
        <span>Realizado: <strong className="text-foreground">{formatCurrency(item.valorRealizado)}</strong></span>
        {!semOrcamento && <span>Orçado: {formatCurrency(item.valorOrcado)}</span>}
      </div>
      {!semOrcamento && <BarraProgresso percent={pct} />}
    </div>
  );
}

export default function OrcamentoAnual() {
  const [aba, setAba] = useState('comparacao');
  const [ano, setAno] = useState(anoAtual);
  const [comparacao, setComparacao] = useState(null);
  const [orcamentos, setOrcamentos] = useState([]);
  const [categorias, setCategorias] = useState({ receitas: [], despesas: [] });
  const [loading, setLoading] = useState(false);
  const [form, setForm] = useState({ tipo: '1', categoriaReceitaId: '', categoriaDespesaId: '', valorOrcado: '' });
  const [salvando, setSalvando] = useState(false);

  const carregarComparacao = async () => {
    setLoading(true);
    try {
      const res = await orcamentoCategoriasApi.getComparacao(ano);
      setComparacao(res.data);
    } catch {
      toast.error('Erro ao carregar comparação.');
    } finally {
      setLoading(false);
    }
  };

  const carregarOrcamentos = async () => {
    setLoading(true);
    try {
      const res = await orcamentoCategoriasApi.getByAno(ano);
      setOrcamentos(res.data || []);
    } catch {
      toast.error('Erro ao carregar orçamentos.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    Promise.all([
      categoriasReceitasApi.getAll(),
      categoriasDespesasApi.getAll(),
    ]).then(([rec, desp]) => {
      setCategorias({
        receitas: (rec.data || []).filter((c) => c.ativo),
        despesas: (desp.data || []).filter((c) => c.ativo),
      });
    }).catch(console.error);
  }, []);

  useEffect(() => {
    if (aba === 'comparacao') carregarComparacao();
    else carregarOrcamentos();
  }, [ano, aba]);

  const salvar = async (e) => {
    e.preventDefault();
    if (!form.valorOrcado || Number(form.valorOrcado) <= 0) {
      toast.error('Informe um valor válido.');
      return;
    }
    const tipo = Number(form.tipo);
    if (tipo === 1 && !form.categoriaReceitaId) {
      toast.error('Selecione a categoria de receita.');
      return;
    }
    if (tipo === 2 && !form.categoriaDespesaId) {
      toast.error('Selecione a categoria de despesa.');
      return;
    }
    setSalvando(true);
    try {
      await orcamentoCategoriasApi.save({
        ano,
        tipo,
        categoriaReceitaId: tipo === 1 ? Number(form.categoriaReceitaId) : null,
        categoriaDespesaId: tipo === 2 ? Number(form.categoriaDespesaId) : null,
        valorOrcado: Number(form.valorOrcado),
      });
      toast.success('Orçamento salvo.');
      setForm({ tipo: '1', categoriaReceitaId: '', categoriaDespesaId: '', valorOrcado: '' });
      carregarOrcamentos();
    } catch {
      toast.error('Erro ao salvar orçamento.');
    } finally {
      setSalvando(false);
    }
  };

  const excluir = async (id) => {
    try {
      await orcamentoCategoriasApi.delete(id);
      toast.success('Orçamento removido.');
      carregarOrcamentos();
    } catch {
      toast.error('Erro ao remover orçamento.');
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Orçamento Anual</h1>
        <p className="text-muted-foreground">Planeje receitas e despesas por categoria e acompanhe o realizado.</p>
      </div>

      {/* Controles */}
      <div className="flex items-center gap-4 flex-wrap">
        <div className="flex gap-1 rounded-lg border p-1">
          {['comparacao', 'configurar'].map((a) => (
            <button
              key={a}
              type="button"
              onClick={() => setAba(a)}
              className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors ${aba === a ? 'bg-primary text-primary-foreground' : 'hover:bg-muted'}`}
            >
              {a === 'comparacao' ? 'Orçado vs Realizado' : 'Configurar'}
            </button>
          ))}
        </div>
        <select
          value={ano}
          onChange={(e) => setAno(Number(e.target.value))}
          className="px-3 py-2 border rounded text-sm"
        >
          {ANOS.map((a) => <option key={a} value={a}>{a}</option>)}
        </select>
      </div>

      {loading && <LoadingPage text="Carregando..." />}

      {/* ABA: Comparação */}
      {aba === 'comparacao' && comparacao && !loading && (
        <>
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-green-700">
                  <TrendingUp className="h-5 w-5" /> Receitas {ano}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex justify-between text-sm border-b pb-2">
                  <span className="text-muted-foreground">Orçado total</span>
                  <span className="font-semibold">{formatCurrency(comparacao.totalOrcadoReceitas)}</span>
                </div>
                <div className="flex justify-between text-sm border-b pb-2">
                  <span className="text-muted-foreground">Realizado total</span>
                  <span className="font-semibold text-green-700">{formatCurrency(comparacao.totalRealizadoReceitas)}</span>
                </div>
                {comparacao.receitas.map((item, i) => <ItemComparacao key={i} item={item} />)}
                {comparacao.receitas.length === 0 && (
                  <p className="text-sm text-muted-foreground text-center py-4">Nenhuma receita no período.</p>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-red-700">
                  <TrendingDown className="h-5 w-5" /> Despesas {ano}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex justify-between text-sm border-b pb-2">
                  <span className="text-muted-foreground">Orçado total</span>
                  <span className="font-semibold">{formatCurrency(comparacao.totalOrcadoDespesas)}</span>
                </div>
                <div className="flex justify-between text-sm border-b pb-2">
                  <span className="text-muted-foreground">Realizado total</span>
                  <span className={`font-semibold ${comparacao.totalRealizadoDespesas > comparacao.totalOrcadoDespesas ? 'text-red-700' : ''}`}>
                    {formatCurrency(comparacao.totalRealizadoDespesas)}
                  </span>
                </div>
                {comparacao.despesas.map((item, i) => <ItemComparacao key={i} item={item} />)}
                {comparacao.despesas.length === 0 && (
                  <p className="text-sm text-muted-foreground text-center py-4">Nenhuma despesa no período.</p>
                )}
              </CardContent>
            </Card>
          </div>
        </>
      )}

      {/* ABA: Configurar */}
      {aba === 'configurar' && !loading && (
        <div className="grid gap-6 md:grid-cols-2">
          {/* Formulário */}
          <Card>
            <CardHeader><CardTitle>Novo orçamento</CardTitle></CardHeader>
            <CardContent>
              <form onSubmit={salvar} className="space-y-4">
                <div className="space-y-2">
                  <Label>Tipo</Label>
                  <select
                    value={form.tipo}
                    onChange={(e) => setForm((p) => ({ ...p, tipo: e.target.value, categoriaReceitaId: '', categoriaDespesaId: '' }))}
                    className="w-full px-3 py-2 border rounded"
                  >
                    <option value="1">Receita</option>
                    <option value="2">Despesa</option>
                  </select>
                </div>

                {form.tipo === '1' && (
                  <div className="space-y-2">
                    <Label>Categoria de receita *</Label>
                    <select
                      value={form.categoriaReceitaId}
                      onChange={(e) => setForm((p) => ({ ...p, categoriaReceitaId: e.target.value }))}
                      className="w-full px-3 py-2 border rounded"
                    >
                      <option value="">Selecionar</option>
                      {categorias.receitas.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
                    </select>
                  </div>
                )}

                {form.tipo === '2' && (
                  <div className="space-y-2">
                    <Label>Categoria de despesa *</Label>
                    <select
                      value={form.categoriaDespesaId}
                      onChange={(e) => setForm((p) => ({ ...p, categoriaDespesaId: e.target.value }))}
                      className="w-full px-3 py-2 border rounded"
                    >
                      <option value="">Selecionar</option>
                      {categorias.despesas.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
                    </select>
                  </div>
                )}

                <div className="space-y-2">
                  <Label>Valor orçado para {ano} *</Label>
                  <Input
                    type="number"
                    step="0.01"
                    min="0"
                    placeholder="0,00"
                    value={form.valorOrcado}
                    onChange={(e) => setForm((p) => ({ ...p, valorOrcado: e.target.value }))}
                  />
                </div>

                <Button type="submit" disabled={salvando} className="w-full">
                  <Save className="h-4 w-4 mr-2" /> {salvando ? 'Salvando...' : 'Salvar orçamento'}
                </Button>
              </form>
            </CardContent>
          </Card>

          {/* Lista */}
          <Card>
            <CardHeader>
              <CardTitle>Orçamentos {ano}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {orcamentos.length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-8">Nenhum orçamento configurado para {ano}.</p>
              )}
              {orcamentos.map((o) => (
                <div key={o.id} className="flex items-center justify-between rounded border px-3 py-2 text-sm">
                  <div>
                    <Badge variant={o.tipo === 1 ? 'default' : 'secondary'} className="mr-2 text-xs">
                      {o.tipo === 1 ? 'Receita' : 'Despesa'}
                    </Badge>
                    {o.categoriaNome}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">{formatCurrency(o.valorOrcado)}</span>
                    <Button variant="ghost" size="sm" onClick={() => excluir(o.id)} className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive">
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
