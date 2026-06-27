import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, Plus, Trash2, Save, User, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { receitasApi, contasBancariasApi, categoriasReceitasApi, pessoasApi } from '@/lib/api';
import { toast } from 'sonner';
import { formatCurrency } from '@/lib/formatters';

const EMPTY_ITEM = () => ({
  _key: crypto.randomUUID(),
  pessoaId: null,
  pessoaNome: '',
  valor: '',
  descricao: '',
  pessoaBusca: '',
  resultados: [],
  buscando: false,
});

export default function LancamentoDizimos() {
  const [contas, setContas] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [saving, setSaving] = useState(false);

  const [cabecalho, setCabecalho] = useState({
    data: new Date().toISOString().slice(0, 10),
    descricaoPadrao: 'Dízimo',
    categoriaReceitaId: '',
    contaBancariaId: '',
  });

  const [itens, setItens] = useState([EMPTY_ITEM()]);

  useEffect(() => {
    Promise.all([contasBancariasApi.getAll(), categoriasReceitasApi.getAll()])
      .then(([contasRes, catRes]) => {
        setContas(contasRes.data || []);
        setCategorias((catRes.data || []).filter((c) => c.ativo));
      })
      .catch(console.error);
  }, []);

  // Busca de pessoa por item
  const buscarPessoa = (key, termo) => {
    setItens((prev) =>
      prev.map((item) =>
        item._key === key ? { ...item, pessoaBusca: termo, pessoaId: null, pessoaNome: '' } : item
      )
    );
    if (!termo || termo.length < 2) {
      setItens((prev) =>
        prev.map((item) => (item._key === key ? { ...item, resultados: [] } : item))
      );
      return;
    }
    const timer = setTimeout(async () => {
      setItens((prev) =>
        prev.map((item) => (item._key === key ? { ...item, buscando: true } : item))
      );
      try {
        const res = await pessoasApi.getPaged({ nome: termo, pageSize: 6, ativo: true });
        setItens((prev) =>
          prev.map((item) =>
            item._key === key
              ? { ...item, resultados: res.data?.items || [], buscando: false }
              : item
          )
        );
      } catch {
        setItens((prev) =>
          prev.map((item) => (item._key === key ? { ...item, buscando: false } : item))
        );
      }
    }, 300);
    return () => clearTimeout(timer);
  };

  const selecionarPessoa = (key, pessoa) => {
    setItens((prev) =>
      prev.map((item) =>
        item._key === key
          ? { ...item, pessoaId: pessoa.id, pessoaNome: pessoa.nome, pessoaBusca: '', resultados: [] }
          : item
      )
    );
  };

  const limparPessoa = (key) => {
    setItens((prev) =>
      prev.map((item) =>
        item._key === key
          ? { ...item, pessoaId: null, pessoaNome: '', pessoaBusca: '' }
          : item
      )
    );
  };

  const atualizarItem = (key, campo, valor) => {
    setItens((prev) =>
      prev.map((item) => (item._key === key ? { ...item, [campo]: valor } : item))
    );
  };

  const adicionarLinha = () => setItens((prev) => [...prev, EMPTY_ITEM()]);

  const removerLinha = (key) => {
    if (itens.length === 1) return;
    setItens((prev) => prev.filter((item) => item._key !== key));
  };

  const totalGeral = itens.reduce((sum, item) => sum + (parseFloat(item.valor) || 0), 0);

  const handleSalvar = async () => {
    if (!cabecalho.data) {
      toast.error('Informe a data do culto.');
      return;
    }
    const itensValidos = itens.filter((item) => parseFloat(item.valor) > 0);
    if (itensValidos.length === 0) {
      toast.error('Informe o valor em pelo menos um lançamento.');
      return;
    }

    setSaving(true);
    try {
      const payload = {
        data: new Date(cabecalho.data).toISOString(),
        descricaoPadrao: cabecalho.descricaoPadrao || 'Dízimo',
        categoriaReceitaId: cabecalho.categoriaReceitaId ? Number(cabecalho.categoriaReceitaId) : null,
        contaBancariaId: cabecalho.contaBancariaId ? Number(cabecalho.contaBancariaId) : null,
        itens: itensValidos.map((item) => ({
          pessoaId: item.pessoaId || null,
          valor: parseFloat(item.valor),
          descricao: item.descricao?.trim() || null,
        })),
      };
      const res = await receitasApi.lancarLote(payload);
      const count = res.data?.length ?? itensValidos.length;
      toast.success(`${count} lançamento${count !== 1 ? 's' : ''} registrado${count !== 1 ? 's' : ''} com sucesso.`);
      setItens([EMPTY_ITEM()]);
    } catch (err) {
      const msg = err.response?.data || 'Erro ao salvar lançamentos.';
      toast.error(typeof msg === 'string' ? msg : 'Erro ao salvar lançamentos.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/receitas">
            <ArrowLeft className="h-4 w-4 mr-2" /> Voltar
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">Lançamento de Dízimos e Ofertas</h1>
          <p className="text-muted-foreground">Registre múltiplas contribuições de um culto de uma vez.</p>
        </div>
      </div>

      {/* Cabeçalho do lançamento */}
      <Card>
        <CardHeader>
          <CardTitle>Dados do culto</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <Label htmlFor="data">Data *</Label>
              <Input
                id="data"
                type="date"
                value={cabecalho.data}
                onChange={(e) => setCabecalho((prev) => ({ ...prev, data: e.target.value }))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="descricaoPadrao">Descrição padrão</Label>
              <Input
                id="descricaoPadrao"
                value={cabecalho.descricaoPadrao}
                onChange={(e) => setCabecalho((prev) => ({ ...prev, descricaoPadrao: e.target.value }))}
                placeholder="Dízimo, Oferta, Missões..."
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="categoriaReceitaId">Categoria</Label>
              <select
                id="categoriaReceitaId"
                value={cabecalho.categoriaReceitaId}
                onChange={(e) => setCabecalho((prev) => ({ ...prev, categoriaReceitaId: e.target.value }))}
                className="w-full px-3 py-2 border rounded"
              >
                <option value="">Selecionar</option>
                {categorias.map((c) => (
                  <option key={c.id} value={c.id}>{c.nome}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="contaBancariaId">Conta bancária</Label>
              <select
                id="contaBancariaId"
                value={cabecalho.contaBancariaId}
                onChange={(e) => setCabecalho((prev) => ({ ...prev, contaBancariaId: e.target.value }))}
                className="w-full px-3 py-2 border rounded"
              >
                <option value="">-</option>
                {contas.map((c) => (
                  <option key={c.id} value={c.id}>{c.nome}</option>
                ))}
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tabela de lançamentos */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Contribuições</span>
            <Badge variant="secondary" className="text-base font-bold">
              Total: {formatCurrency(totalGeral)}
            </Badge>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {itens.map((item, index) => (
            <div key={item._key} className="grid gap-3 rounded-lg border p-3 md:grid-cols-[2fr_1fr_2fr_auto]">
              {/* Membro */}
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Membro</Label>
                {item.pessoaNome ? (
                  <div className="flex items-center gap-2 rounded border px-2 py-1.5 bg-muted/40 text-sm">
                    <User className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                    <span className="flex-1 truncate">{item.pessoaNome}</span>
                    <button
                      type="button"
                      onClick={() => limparPessoa(item._key)}
                      className="text-muted-foreground hover:text-foreground"
                    >
                      ×
                    </button>
                  </div>
                ) : (
                  <div className="relative">
                    <div className="relative">
                      <Search className="absolute left-2 top-2 h-3.5 w-3.5 text-muted-foreground" />
                      <Input
                        className="pl-7 h-8 text-sm"
                        placeholder="Buscar membro (opcional)..."
                        value={item.pessoaBusca}
                        onChange={(e) => buscarPessoa(item._key, e.target.value)}
                      />
                    </div>
                    {item.resultados.length > 0 && (
                      <div className="absolute z-10 mt-1 w-full rounded border bg-white shadow-lg">
                        {item.resultados.map((p) => (
                          <button
                            key={p.id}
                            type="button"
                            onClick={() => selecionarPessoa(item._key, p)}
                            className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-muted"
                          >
                            <User className="h-3 w-3 text-muted-foreground" />
                            {p.nome}
                          </button>
                        ))}
                      </div>
                    )}
                    {!item.pessoaNome && !item.pessoaBusca && (
                      <p className="text-xs text-muted-foreground mt-1">Anônimo / sem vínculo</p>
                    )}
                  </div>
                )}
              </div>

              {/* Valor */}
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Valor *</Label>
                <Input
                  type="number"
                  step="0.01"
                  min="0"
                  placeholder="0,00"
                  className="h-8 text-sm"
                  value={item.valor}
                  onChange={(e) => atualizarItem(item._key, 'valor', e.target.value)}
                />
              </div>

              {/* Descrição específica */}
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Obs. específica</Label>
                <Input
                  placeholder="Ex: Oferta missionária..."
                  className="h-8 text-sm"
                  value={item.descricao}
                  onChange={(e) => atualizarItem(item._key, 'descricao', e.target.value)}
                />
              </div>

              {/* Remover */}
              <div className="flex items-end pb-0.5">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => removerLinha(item._key)}
                  disabled={itens.length === 1}
                  className="h-8 w-8 p-0 text-muted-foreground hover:text-destructive"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          ))}

          <Button type="button" variant="outline" size="sm" onClick={adicionarLinha} className="w-full mt-2">
            <Plus className="h-4 w-4 mr-2" /> Adicionar linha
          </Button>
        </CardContent>
      </Card>

      <div className="flex items-center gap-3">
        <Button onClick={handleSalvar} disabled={saving}>
          <Save className="h-4 w-4 mr-2" />
          {saving ? 'Salvando...' : `Salvar ${itens.filter((i) => parseFloat(i.valor) > 0).length} lançamento(s)`}
        </Button>
        <Button variant="outline" asChild>
          <Link to="/financeiro/receitas">Cancelar</Link>
        </Button>
        <span className="text-sm text-muted-foreground ml-auto">
          {itens.filter((i) => parseFloat(i.valor) > 0).length} de {itens.length} linhas com valor
        </span>
      </div>
    </div>
  );
}
